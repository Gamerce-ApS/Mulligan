using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MulliganCombatFeel
{
    public enum VFXLabPhase { None, Anticipation, Release, Impact, Recovery, Done }

    /// <summary>
    /// Small experimental effect abstraction for the VFX Lab. An effect prefab carries this plus
    /// whatever it needs (particles, trails, an attacker-motion target, an impact object). Four
    /// timing phases fire in order — <b>Anticipation, Release, Impact, Recovery</b> — each with a
    /// UnityEvent hook (inspector-wireable) AND a sensible default behaviour. Presentation only.
    ///
    /// This is intentionally NOT a production framework: no pooling, no data assets, no editor UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXLabEffectPlayer : MonoBehaviour
    {
        [Header("Timing (seconds from Play, effect-local scaled time)")]
        public float anticipationTime = 0f;
        public float releaseTime = 0.14f;
        public float impactTime = 0.24f;
        public float recoveryTime = 0.5f;
        [Tooltip("Effect is considered finished this long after Recovery.")]
        public float tailTime = 0.4f;

        [Header("Power (scales target reaction, camera punch, hit stop)")]
        [Range(0f, 1f)] public float intensity = 0.6f;
        public float hitStopSeconds = 0.06f;

        [Header("Optional content (all nullable)")]
        [Tooltip("Transform pulled back at Anticipation, lunged toward attackerLungeBy at Release, returned at Recovery.")]
        public Transform attackerMotionTarget;
        [Tooltip("Local-space pull-back offset applied during Anticipation (opposite the lunge for a wind-up).")]
        public Vector3 attackerAnticipationBy = new Vector3(-0.4f, 0f, 0f);
        public Vector3 attackerLungeBy = new Vector3(1.2f, 0f, 0f);
        public ParticleSystem[] primaryParticles;
        public ParticleSystem[] secondaryParticles;
        public TrailRenderer[] trails;
        [Tooltip("Toggled active for a brief flash at Impact (e.g. an additive near-white quad).")]
        public GameObject impactObject;
        public float impactObjectFlashTime = 0.06f;
        [Tooltip("Move impactObject to the lab's impactPoint when it fires, so impact reads at the target.")]
        public bool moveImpactObjectToImpactPoint = true;

        [Header("Phase hooks (inspector-wireable)")]
        public UnityEvent onAnticipation;
        public UnityEvent onRelease;
        public UnityEvent onImpact;
        public UnityEvent onRecovery;
        public UnityEvent onReset;

        public bool IsPlaying { get; private set; }
        public float ElapsedTime { get; private set; }
        public VFXLabPhase CurrentPhase { get; private set; } = VFXLabPhase.None;

        VFXLabController _lab;
        Coroutine _run;
        Vector3 _baseLocalPos;
        Vector3 _baseLocalScale;
        Quaternion _baseLocalRot;
        Vector3 _attackerBaseLocalPos;
        bool _capturedAttackerBase;
        Vector3 _impactObjBaseLocalPos;
        Vector3 _impactObjBaseLocalScale;
        Quaternion _impactObjBaseLocalRot;
        bool _capturedImpactObjBase;

        void Awake()
        {
            _baseLocalPos = transform.localPosition;
            _baseLocalScale = transform.localScale;
            _baseLocalRot = transform.localRotation;
            CaptureAttackerBase();
            if (impactObject != null)
            {
                _impactObjBaseLocalPos = impactObject.transform.localPosition;
                _impactObjBaseLocalScale = impactObject.transform.localScale;
                _impactObjBaseLocalRot = impactObject.transform.localRotation;
                _capturedImpactObjBase = true;
            }
        }

        void CaptureAttackerBase()
        {
            if (_capturedAttackerBase || attackerMotionTarget == null) return;
            _attackerBaseLocalPos = attackerMotionTarget.localPosition;
            _capturedAttackerBase = true;
        }

        public void Play(VFXLabController lab)
        {
            _lab = lab != null ? lab : VFXLabController.Instance;
            ResetEffect();
            _run = StartCoroutine(RunRoutine());
        }

        public void Play() => Play(VFXLabController.Instance);

        IEnumerator RunRoutine()
        {
            IsPlaying = true;
            ElapsedTime = 0f;
            CurrentPhase = VFXLabPhase.None;
            CaptureAttackerBase();

            yield return WaitScaled(anticipationTime);
            CurrentPhase = VFXLabPhase.Anticipation;
            DoAnticipation();
            SafeInvoke(onAnticipation);

            yield return WaitScaled(releaseTime - anticipationTime);
            CurrentPhase = VFXLabPhase.Release;
            DoRelease();
            SafeInvoke(onRelease);

            yield return WaitScaled(impactTime - releaseTime);
            CurrentPhase = VFXLabPhase.Impact;
            DoImpact();
            SafeInvoke(onImpact);

            yield return WaitScaled(recoveryTime - impactTime);
            CurrentPhase = VFXLabPhase.Recovery;
            DoRecovery();
            SafeInvoke(onRecovery);

            yield return WaitScaled(tailTime);
            CurrentPhase = VFXLabPhase.Done;
            if (_lab != null && _lab.target != null)
                _lab.target.ResetTarget();
            IsPlaying = false;
            _run = null;
        }

        [Tooltip("Per-frame time step is clamped to this so a hitchy editor frame cannot skip a whole phase (deterministic phase timing for capture). 0 = no clamp.")]
        public float maxFrameStep = 0.034f;

        IEnumerator WaitScaled(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            float t = 0f;
            while (t < seconds)
            {
                float dt = Time.deltaTime;   // 0 while paused / hit-stopped -> phases hold
                if (maxFrameStep > 0f && Time.timeScale > 0f) dt = Mathf.Min(dt, maxFrameStep * Time.timeScale);
                t += dt;
                ElapsedTime += dt;
                yield return null;
            }
        }

        // ---- default phase behaviour -------------------------------------

        void DoAnticipation()
        {
            // wind-up pull-back — eases in over most of the anticipation window
            if (attackerMotionTarget != null && _capturedAttackerBase)
            {
                float d = Mathf.Max(0.04f, (releaseTime - anticipationTime) * 0.85f);
                StartCoroutine(MoveEase(attackerMotionTarget, _attackerBaseLocalPos + attackerAnticipationBy, d, Ease.InQuad));
            }
        }

        void DoRelease()
        {
            // explosive commit forward with overshoot
            if (attackerMotionTarget != null && _capturedAttackerBase)
                StartCoroutine(MoveEase(attackerMotionTarget, _attackerBaseLocalPos + attackerLungeBy, 0.07f, Ease.OutCubic));
            PlayAll(primaryParticles);
            SetTrailsEmitting(true);
        }

        void DoImpact()
        {
            var lab = _lab != null ? _lab : VFXLabController.Instance;

            PlayAll(secondaryParticles);
            if (impactObject != null)
            {
                // Impact reads at the target: move the impact object to the lab's impact point.
                if (moveImpactObjectToImpactPoint && lab != null && lab.impactPoint != null)
                    impactObject.transform.position = lab.impactPoint.position;
                StartCoroutine(FlashObject(impactObject, impactObjectFlashTime));
            }

            if (lab == null) return;
            Vector3 dir = lab.HitDirection();
            if (lab.target != null) lab.target.PlayHitReaction(dir, intensity);
            if (lab.cameraController != null) lab.cameraController.Punch(dir, intensity);
            if (hitStopSeconds > 0f) lab.HitStop(hitStopSeconds * Mathf.Lerp(0.5f, 1.5f, intensity));
        }

        void DoRecovery()
        {
            SetTrailsEmitting(false);
            if (attackerMotionTarget != null && _capturedAttackerBase)
                StartCoroutine(MoveEase(attackerMotionTarget, _attackerBaseLocalPos, 0.28f, Ease.OutBack));
        }

        // ---- reset -------------------------------------------------------

        public void ResetEffect()
        {
            StopAllCoroutines();
            _run = null;
            IsPlaying = false;
            ElapsedTime = 0f;
            CurrentPhase = VFXLabPhase.None;
            SafeInvoke(onReset);
            transform.localPosition = _baseLocalPos;
            transform.localScale = _baseLocalScale;
            transform.localRotation = _baseLocalRot;
            StopAll(primaryParticles);
            StopAll(secondaryParticles);
            SetTrailsEmitting(false);
            foreach (var tr in Safe(trails)) if (tr != null) tr.Clear();
            if (impactObject != null)
            {
                impactObject.SetActive(false);
                if (_capturedImpactObjBase)
                {
                    impactObject.transform.localPosition = _impactObjBaseLocalPos;
                    impactObject.transform.localScale = _impactObjBaseLocalScale;
                    impactObject.transform.localRotation = _impactObjBaseLocalRot;
                }
            }
            if (attackerMotionTarget != null && _capturedAttackerBase)
                attackerMotionTarget.localPosition = _attackerBaseLocalPos;
        }

        // ---- helpers ----------------------------------------------------

        // easing used by attacker anticipation / lunge / recovery
        enum Ease { Linear, InQuad, OutCubic, OutBack }

        IEnumerator MoveEase(Transform tr, Vector3 to, float dur, Ease ease)
        {
            Vector3 from = tr.localPosition;
            float t = 0f;
            dur = Mathf.Max(0.02f, dur);
            while (t < dur)
            {
                t += Time.deltaTime;
                float x = Mathf.Clamp01(t / dur);
                float k = ease == Ease.InQuad ? x * x
                        : ease == Ease.OutCubic ? 1f - Mathf.Pow(1f - x, 3f)
                        : ease == Ease.OutBack ? EaseOutBack(x)
                        : x;
                tr.localPosition = Vector3.LerpUnclamped(from, to, k);
                yield return null;
            }
            tr.localPosition = to;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float f = x - 1f;
            return 1f + c3 * f * f * f + c1 * f * f;
        }

        IEnumerator FlashObject(GameObject go, float seconds)
        {
            go.SetActive(true);
            float t = 0f;
            while (t < seconds) { t += Time.deltaTime; yield return null; }
            go.SetActive(false);
        }

        static void SafeInvoke(UnityEvent e) { if (e != null) e.Invoke(); }
        static void PlayAll(ParticleSystem[] arr) { foreach (var p in Safe(arr)) if (p != null) { p.Clear(true); p.Play(true); } }
        static void StopAll(ParticleSystem[] arr) { foreach (var p in Safe(arr)) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
        void SetTrailsEmitting(bool on) { foreach (var tr in Safe(trails)) if (tr != null) tr.emitting = on; }
        static T[] Safe<T>(T[] a) => a ?? Array.Empty<T>();
    }
}

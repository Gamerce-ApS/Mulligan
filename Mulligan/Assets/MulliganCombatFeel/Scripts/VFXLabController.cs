using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganCombatFeel
{
    public enum VFXLabBackground { DarkNeutral, LightNeutral, MulliganLike }

    /// <summary>
    /// Standalone experimental VFX Lab orchestrator. Presentation-only — contains NO gameplay logic.
    /// Owns <see cref="Time.timeScale"/> for playback speed and hit stop, drives one effect at a time,
    /// and exposes a small public API so an agent (Unity Open MCP / execute_csharp) can run the
    /// inspect -> play -> capture -> iterate loop without touching UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXLabController : MonoBehaviour
    {
        public static VFXLabController Instance { get; private set; }

        [Header("Scene wiring")]
        public VFXLabCameraController cameraController;
        public VFXLabTarget target;
        public Transform attacker;
        public Transform effectOrigin;
        public Transform impactPoint;
        public Camera labCamera;

        [Header("Backgrounds (child roots toggled by mode)")]
        public GameObject bgDarkNeutral;
        public GameObject bgLightNeutral;
        public GameObject bgMulliganLike;
        public Color darkClear = new Color(0.055f, 0.055f, 0.065f, 1f);
        public Color lightClear = new Color(0.75f, 0.75f, 0.78f, 1f);
        public Color mulliganClear = new Color(0.045f, 0.08f, 0.09f, 1f);
        public VFXLabBackground background = VFXLabBackground.DarkNeutral;

        [Header("Background suppression (optional)")]
        [Tooltip("World mode: a full-screen dark quad Renderer faded up during an attack so effects read, restored after.")]
        public Renderer bgSuppressQuad;
        [Tooltip("UI mode: a full-screen dark Image between the background and the combatants. Alpha is driven the same way.")]
        public Graphic bgSuppressGraphic;
        [Range(0f, 1f)] public float bgSuppressDefaultPeak = 0.32f;

        [Header("Playback")]
        [Tooltip("Base lab time scale. 1 = real time. Hit stop dips below this; ResetLab restores it.")]
        [Range(0.05f, 1f)] public float playbackSpeed = 1f;

        [Header("Current effect")]
        [Tooltip("Prefab spawned at the effect origin on Play(). Swap at runtime with SetEffectPrefab().")]
        public GameObject currentEffectPrefab;
        [Tooltip("Optional pre-placed effect instance in the scene. If set, no prefab is spawned.")]
        public VFXLabEffectPlayer currentEffect;

        // --- state queries (for automation) --------------------------------
        public bool IsPlaying => currentEffect != null && currentEffect.IsPlaying;
        public bool IsPaused { get; private set; }
        public float PlaybackSpeed => playbackSpeed;
        public float CurrentEffectTime => currentEffect != null ? currentEffect.ElapsedTime : 0f;
        public string CurrentPhase => currentEffect != null ? currentEffect.CurrentPhase.ToString() : "None";
        public bool HitStopActive => _hitStopCo != null;

        GameObject _spawnedEffect;
        Coroutine _hitStopCo;
        Coroutine _pauseAtCo;

        [Tooltip("Runtime frame cap while the lab is active — keeps slow-mo readable and captures stable. Not a project setting.")]
        public int labTargetFrameRate = 60;

        int _prevTargetFrameRate;

        void OnEnable()
        {
            Instance = this;
            if (labCamera == null) labCamera = Camera.main;
            _prevTargetFrameRate = Application.targetFrameRate;
            Application.targetFrameRate = Mathf.Max(15, labTargetFrameRate);
            ApplyBackground(background);
            RestoreTimeScale();
        }

        void OnDisable()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;                              // never leave it stuck
            Application.targetFrameRate = _prevTargetFrameRate;
        }

        // ================================================================
        //  Playback API
        // ================================================================

        public void Play()
        {
            ResetLab();
            EnsureEffectInstance();
            if (currentEffect == null) { Debug.LogWarning("[VFXLab] Play() called with no effect assigned."); return; }
            IsPaused = false;
            RestoreTimeScale();
            currentEffect.Play(this);
        }

        public void Replay() => Play();

        public void ResetLab()
        {
            StopLabCoroutines();
            IsPaused = false;
            RestoreTimeScale();
            if (currentEffect != null) currentEffect.ResetEffect();
            if (target != null) target.ResetTarget();
            if (cameraController != null) cameraController.ResetCamera();
            ClearBackgroundSuppression();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            IsPaused = false;
            RestoreTimeScale();
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.05f, 1f);
            if (!IsPaused && _hitStopCo == null) Time.timeScale = playbackSpeed;
        }

        // named presets (0.25x / 0.5x / 1x required by the lab spec)
        public void SpeedQuarter() => SetPlaybackSpeed(0.25f);
        public void SpeedHalf() => SetPlaybackSpeed(0.5f);
        public void SpeedFull() => SetPlaybackSpeed(1f);

        /// <summary>Play the current effect, then Pause() the moment its local time reaches
        /// <paramref name="effectTime"/> seconds — so an agent can capture a chosen frame.</summary>
        public void PlayAndPauseAt(float effectTime)
        {
            Play();
            if (_pauseAtCo != null) StopCoroutine(_pauseAtCo);
            _pauseAtCo = StartCoroutine(PauseAtRoutine(effectTime));
        }

        IEnumerator PauseAtRoutine(float effectTime)
        {
            while (currentEffect != null && currentEffect.IsPlaying && currentEffect.ElapsedTime < effectTime)
                yield return null;
            Pause();
            _pauseAtCo = null;
        }

        /// <summary>Play the current effect, then Pause() one frame after it enters
        /// <paramref name="phase"/> — frame-rate independent, the reliable "capture the impact" call.</summary>
        public void PlayAndPauseAtPhase(VFXLabPhase phase)
        {
            Play();
            if (_pauseAtCo != null) StopCoroutine(_pauseAtCo);
            _pauseAtCo = StartCoroutine(PauseAtPhaseRoutine(phase));
        }

        public void PlayAndPauseAtPhase(string phase)
        {
            if (System.Enum.TryParse(phase, true, out VFXLabPhase p)) PlayAndPauseAtPhase(p);
            else Debug.LogWarning($"[VFXLab] Unknown phase '{phase}'. Use Anticipation|Release|Impact|Recovery.");
        }

        IEnumerator PauseAtPhaseRoutine(VFXLabPhase phase)
        {
            while (currentEffect != null && currentEffect.IsPlaying && currentEffect.CurrentPhase < phase)
                yield return null;
            Pause();               // freeze immediately — an extra frame can skip a short phase
            _pauseAtCo = null;
        }

        // ================================================================
        //  Hit stop (lab-only, presentation-only)
        // ================================================================

        /// <summary>Freeze (or near-freeze) time for <paramref name="realSeconds"/> of real time,
        /// then restore to the current playback speed. Safe with playback speed, pause and ResetLab.</summary>
        public void HitStop(float realSeconds, float freezeScale = 0f)
        {
            if (_hitStopCo != null) StopCoroutine(_hitStopCo);
            _hitStopCo = StartCoroutine(HitStopRoutine(Mathf.Max(0f, realSeconds), Mathf.Clamp01(freezeScale)));
        }

        IEnumerator HitStopRoutine(float realSeconds, float freezeScale)
        {
            if (!IsPaused) Time.timeScale = playbackSpeed * freezeScale;
            float end = Time.unscaledTime + realSeconds;
            while (Time.unscaledTime < end) yield return null;
            _hitStopCo = null;
            if (!IsPaused) Time.timeScale = playbackSpeed;
        }

        // ================================================================
        //  Effect loading
        // ================================================================

        public void SetEffectPrefab(GameObject prefab)
        {
            currentEffectPrefab = prefab;
            DespawnEffect();
        }

        void EnsureEffectInstance()
        {
            if (currentEffect != null) return;
            if (currentEffectPrefab == null) return;
            Vector3 pos = effectOrigin != null ? effectOrigin.position : transform.position;
            Quaternion rot = effectOrigin != null ? effectOrigin.rotation : Quaternion.identity;
            _spawnedEffect = Instantiate(currentEffectPrefab, pos, rot);
            _spawnedEffect.name = currentEffectPrefab.name + " (Lab)";
            currentEffect = _spawnedEffect.GetComponent<VFXLabEffectPlayer>();
            if (currentEffect == null) currentEffect = _spawnedEffect.AddComponent<VFXLabEffectPlayer>();
            // Inject the scene attacker so an effect's optional attacker-lunge works without the
            // prefab needing a scene reference.
            if (currentEffect.attackerMotionTarget == null && attacker != null)
                currentEffect.attackerMotionTarget = attacker;
        }

        void DespawnEffect()
        {
            if (_spawnedEffect == null) return;
            if (Application.isPlaying) Destroy(_spawnedEffect); else DestroyImmediate(_spawnedEffect);
            _spawnedEffect = null;
            currentEffect = null;
        }

        // ================================================================
        //  Backgrounds
        // ================================================================

        public void SetBackgroundMode(VFXLabBackground mode)
        {
            background = mode;
            ApplyBackground(mode);
        }

        // string overload for convenient MCP calls
        public void SetBackgroundMode(string mode)
        {
            if (System.Enum.TryParse(mode, true, out VFXLabBackground m)) SetBackgroundMode(m);
            else Debug.LogWarning($"[VFXLab] Unknown background mode '{mode}'.");
        }

        void ApplyBackground(VFXLabBackground mode)
        {
            if (bgDarkNeutral != null) bgDarkNeutral.SetActive(mode == VFXLabBackground.DarkNeutral);
            if (bgLightNeutral != null) bgLightNeutral.SetActive(mode == VFXLabBackground.LightNeutral);
            if (bgMulliganLike != null) bgMulliganLike.SetActive(mode == VFXLabBackground.MulliganLike);
            if (labCamera != null)
                labCamera.backgroundColor = mode == VFXLabBackground.DarkNeutral ? darkClear
                                          : mode == VFXLabBackground.LightNeutral ? lightClear
                                          : mulliganClear;
        }

        // ================================================================
        //  Isolated triggers (for validation / debugging individual systems)
        // ================================================================

        public void TriggerTargetReaction(float intensity = 1f)
        {
            if (target != null) target.PlayHitReaction(HitDirection(), intensity);
        }

        public void TriggerCameraPunch(float intensity = 1f)
        {
            if (cameraController != null) cameraController.Punch(HitDirection(), intensity);
        }

        public void TriggerHitStop(float realSeconds = 0.09f) => HitStop(realSeconds);

        // ================================================================
        //  Background suppression
        // ================================================================

        Coroutine _bgCo;

        /// <summary>Fade the background-suppression quad to <paramref name="peak"/> alpha over
        /// <paramref name="upT"/>s, hold <paramref name="holdT"/>s, then restore over <paramref name="downT"/>s.</summary>
        public void SuppressBackground(float peak, float upT, float holdT, float downT)
        {
            if (bgSuppressQuad == null && bgSuppressGraphic == null) return;
            if (_bgCo != null) StopCoroutine(_bgCo);
            _bgCo = StartCoroutine(BgRoutine(Mathf.Clamp01(peak), Mathf.Max(0.01f, upT), Mathf.Max(0f, holdT), Mathf.Max(0.01f, downT)));
        }

        public void SuppressBackgroundDefault() => SuppressBackground(bgSuppressDefaultPeak, 0.06f, 0.30f, 0.35f);

        public void ClearBackgroundSuppression()
        {
            if (_bgCo != null) { StopCoroutine(_bgCo); _bgCo = null; }
            SetBgAlpha(0f);
        }

        System.Collections.IEnumerator BgRoutine(float peak, float upT, float holdT, float downT)
        {
            float t = 0f, from = GetBgAlpha();
            while (t < upT) { t += Time.deltaTime; SetBgAlpha(Mathf.Lerp(from, peak, t / upT)); yield return null; }
            SetBgAlpha(peak);
            t = 0f; while (t < holdT) { t += Time.deltaTime; yield return null; }
            t = 0f; while (t < downT) { t += Time.deltaTime; SetBgAlpha(Mathf.Lerp(peak, 0f, t / downT)); yield return null; }
            SetBgAlpha(0f);
            _bgCo = null;
        }

        float GetBgAlpha()
        {
            if (bgSuppressGraphic != null) return bgSuppressGraphic.color.a;
            if (bgSuppressQuad == null) return 0f;
            var m = Application.isPlaying ? bgSuppressQuad.material : bgSuppressQuad.sharedMaterial;
            return m != null && m.HasProperty("_Color") ? m.GetColor("_Color").a : 0f;
        }

        void SetBgAlpha(float a)
        {
            if (bgSuppressGraphic != null)
            {
                bgSuppressGraphic.gameObject.SetActive(a > 0.001f);
                var gc = bgSuppressGraphic.color; gc.a = a; bgSuppressGraphic.color = gc;
                return;
            }
            if (bgSuppressQuad == null) return;
            bgSuppressQuad.gameObject.SetActive(a > 0.001f);
            var m = Application.isPlaying ? bgSuppressQuad.material : bgSuppressQuad.sharedMaterial;
            if (m == null || !m.HasProperty("_Color")) return;
            var c = m.GetColor("_Color"); c.a = a; m.SetColor("_Color", c);
        }

        public Vector3 HitDirection()
        {
            if (attacker != null && target != null)
            {
                Vector3 d = target.transform.position - attacker.transform.position;
                if (d.sqrMagnitude > 0.0001f) return d.normalized;
            }
            return Vector3.right;
        }

        // ================================================================
        //  helpers
        // ================================================================

        void RestoreTimeScale() => Time.timeScale = IsPaused ? 0f : playbackSpeed;

        void StopLabCoroutines()
        {
            if (_hitStopCo != null) { StopCoroutine(_hitStopCo); _hitStopCo = null; }
            if (_pauseAtCo != null) { StopCoroutine(_pauseAtCo); _pauseAtCo = null; }
        }
    }
}

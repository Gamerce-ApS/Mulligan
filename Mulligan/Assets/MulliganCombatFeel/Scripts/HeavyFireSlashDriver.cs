using System.Collections;
using UnityEngine;

namespace MulliganCombatFeel
{
    /// <summary>
    /// Lab-only visual driver for the HeavyFireSlash_01 benchmark. Sequences the authored flipbook
    /// (frame index over scaled time), the impact star, the anticipation glow, and background
    /// suppression — wired to <see cref="VFXLabEffectPlayer"/>'s phase UnityEvents. Presentation only.
    /// All timing uses scaled time so playback speed and hit stop affect the visuals.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeavyFireSlashDriver : MonoBehaviour
    {
        [Header("Renderers (MPB-driven; must use MulliganVFX/HeavyFireSlash)")]
        public Renderer slash;
        public Renderer impactStar;
        public Renderer anticipationGlow;
        public ParticleSystem sparks;
        public ParticleSystem embers;
        public Renderer dust;                       // grey quad, scaled + faded

        [Header("Primary shape (shader-driven sweep)")]
        public float slashLife = 0.60f;
        [Tooltip("normalised life 0..1  ->  _Reveal (s head->tail). Steep = fast eruption.")]
        public AnimationCurve revealCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.20f, 1.05f), new Keyframe(1f, 1.05f));
        [Tooltip("normalised life 0..1  ->  _Retract (origin end pulls back during decay)")]
        public AnimationCurve retractCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.5f, 0f), new Keyframe(1f, 0.62f));
        [Tooltip("normalised life 0..1  ->  _Burn (flame eats itself in decay)")]
        public AnimationCurve burnCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.52f, 0f), new Keyframe(1f, 1f));
        public AnimationCurve slashIntensity = new AnimationCurve(
            new Keyframe(0f, 0.35f), new Keyframe(0.13f, 1.9f), new Keyframe(0.45f, 1.5f), new Keyframe(0.8f, 0.8f), new Keyframe(1f, 0f));
        public AnimationCurve slashAlpha = new AnimationCurve(
            new Keyframe(0f, 0.5f), new Keyframe(0.12f, 1.1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f));

        [Header("Impact star")]
        public float starLife = 0.30f;
        public float starBaseScale = 5.5f;
        public AnimationCurve starScale = new AnimationCurve(
            new Keyframe(0f, 0.25f), new Keyframe(0.28f, 1.28f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.9f));
        public AnimationCurve starIntensity = new AnimationCurve(
            new Keyframe(0f, 0.4f), new Keyframe(0.12f, 2.6f), new Keyframe(0.4f, 1.6f), new Keyframe(1f, 0f));
        public float starSpin = 26f;

        [Header("Anticipation glow")]
        public float glowMaxScale = 2.0f;
        public AnimationCurve glowScale = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(1f, 1f));
        public AnimationCurve glowIntensity = new AnimationCurve(new Keyframe(0f, 0.15f), new Keyframe(0.8f, 1.5f), new Keyframe(1f, 1.9f));
        public float glowConsumeTime = 0.09f;

        [Header("Background suppression")]
        public float bgPeak = 0.34f;
        public float bgUp = 0.05f;
        public float bgHold = 0.34f;
        public float bgDown = 0.36f;

        [Header("Dust")]
        public float dustLife = 0.45f;
        public float dustMaxScale = 3.4f;

        static readonly int ID_Reveal = Shader.PropertyToID("_Reveal");
        static readonly int ID_Retract = Shader.PropertyToID("_Retract");
        static readonly int ID_Burn = Shader.PropertyToID("_Burn");
        static readonly int ID_Intensity = Shader.PropertyToID("_Intensity");
        static readonly int ID_AlphaMul = Shader.PropertyToID("_AlphaMul");
        static readonly int ID_Frame = Shader.PropertyToID("_Frame");
        static readonly int ID_Warp = Shader.PropertyToID("_Warp");
        static readonly int ID_FrontHeat = Shader.PropertyToID("_FrontHeat");

        [Header("Pass 7 — supplied hero-art mode (MulliganVFX/HeavyFireHero)")]
        [Tooltip("Animate the supplied hero flame PNG via directional reveal / burn / warp.")]
        public bool heroMode = false;
        [Tooltip("normalised slash life -> _Reveal (0 tail .. 1.3 fully shown). Fast: near-full before impact.")]
        public AnimationCurve heroReveal = new AnimationCurve(
            new Keyframe(0f, -0.12f), new Keyframe(0.05f, 0.06f), new Keyframe(0.13f, 0.45f),
            new Keyframe(0.24f, 1.02f), new Keyframe(0.34f, 1.30f), new Keyframe(1f, 1.30f));
        [Tooltip("normalised slash life -> _Burn (burn-away from the tail; starts after impact).")]
        public AnimationCurve heroBurn = new AnimationCurve(
            new Keyframe(0f, -0.15f), new Keyframe(0.40f, -0.10f), new Keyframe(0.62f, 0.45f),
            new Keyframe(0.90f, 1.30f), new Keyframe(1f, 1.32f));
        [Tooltip("normalised slash life -> _Warp (heat shimmer baseline + impact deform spike).")]
        public AnimationCurve heroWarp = new AnimationCurve(
            new Keyframe(0f, 0.0f), new Keyframe(0.24f, 0.01f), new Keyframe(0.32f, 0.085f),
            new Keyframe(0.5f, 0.03f), new Keyframe(1f, 0.02f));
        [Tooltip("normalised slash life -> _FrontHeat (burning hot edge along the reveal front).")]
        public AnimationCurve heroFrontHeat = new AnimationCurve(
            new Keyframe(0f, 2.2f), new Keyframe(0.28f, 1.6f), new Keyframe(0.38f, 0.3f), new Keyframe(1f, 0.2f));
        [Tooltip("normalised slash life -> _Intensity.")]
        public AnimationCurve heroIntensity = new AnimationCurve(
            new Keyframe(0f, 1.35f), new Keyframe(0.10f, 1.15f), new Keyframe(0.34f, 1.0f),
            new Keyframe(0.7f, 0.7f), new Keyframe(1f, 0f));
        [Tooltip("normalised slash life -> _AlphaMul.")]
        public AnimationCurve heroAlpha = new AnimationCurve(
            new Keyframe(0f, 0.55f), new Keyframe(0.08f, 1f), new Keyframe(0.78f, 1f), new Keyframe(1f, 0f));

        [Header("Pass 6 — authored flipbook mode")]
        [Tooltip("Drive the slash / impact as an authored flipbook (MulliganVFX/HeavyFireFlipbook) instead of the procedural sweep shader.")]
        public bool flipbookMode = false;
        [Tooltip("normalised slash life 0..1 -> flame sheet frame index (hold the hero frame ~F3).")]
        public AnimationCurve slashFrameCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.16f, 2f), new Keyframe(0.34f, 3f), new Keyframe(0.52f, 3.2f),
            new Keyframe(0.72f, 4f), new Keyframe(1f, 5f));
        [Tooltip("normalised slash life 0..1 -> impact sheet frame index (0..3).")]
        public AnimationCurve impactFrameCurve = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.25f, 1f), new Keyframe(0.6f, 2f), new Keyframe(1f, 3f));
        [Tooltip("Release smear: local-space offset the slash quad starts at (near the attacker), lerps to its rest pose.")]
        public Vector3 slashReleaseFrom = new Vector3(-3.4f, -0.6f, 0f);
        [Tooltip("Release smear: X-scale over normalised life — compressed -> overshoot -> settle.")]
        public AnimationCurve slashStretchX = new AnimationCurve(
            new Keyframe(0f, 0.32f), new Keyframe(0.10f, 1.18f), new Keyframe(0.22f, 0.96f), new Keyframe(1f, 1f));
        [Tooltip("Impact splash travels forward: local +X the impact quad drifts over its life.")]
        public float impactForwardDrift = 1.4f;
        [Tooltip("heroMode: world offset applied to the impact-burst quad so its off-centre contact core lands on the impact point.")]
        public Vector3 impactCoreOffset = new Vector3(-1.4f, 0.1f, 0f);

        Vector3 _slashBasePos, _slashBaseScale;
        Quaternion _slashBaseRot;
        bool _slashBaseCaptured;

        [Tooltip("Per-frame time step is clamped to this in the driver's own routines so a hitchy " +
                 "editor frame under automation cannot blow through a whole sub-animation. 0 = no clamp.")]
        public float maxFrameStep = 0.034f;

        MaterialPropertyBlock _mpb;
        Vector3 _starBasePos, _starBaseScale;
        Quaternion _starBaseRot;
        Vector3 _glowBasePos, _glowBaseScale;
        Quaternion _glowBaseRot;
        Vector3 _dustBasePos, _dustBaseScale;
        Quaternion _dustBaseRot;
        Coroutine _slashCo, _starCo, _glowCo, _dustCo;
        VFXLabController _lab;

        /// <summary>Scaled dt, clamped so one long editor frame can't skip an entire routine.</summary>
        float StepDt()
        {
            float dt = Time.deltaTime;
            if (maxFrameStep > 0f && Time.timeScale > 0f) dt = Mathf.Min(dt, maxFrameStep * Time.timeScale);
            return dt;
        }

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            if (impactStar != null)
            {
                _starBasePos = impactStar.transform.localPosition;
                _starBaseScale = impactStar.transform.localScale;
                _starBaseRot = impactStar.transform.localRotation;
            }
            if (anticipationGlow != null)
            {
                _glowBasePos = anticipationGlow.transform.localPosition;
                _glowBaseScale = anticipationGlow.transform.localScale;
                _glowBaseRot = anticipationGlow.transform.localRotation;
            }
            if (dust != null)
            {
                _dustBasePos = dust.transform.localPosition;
                _dustBaseScale = dust.transform.localScale;
                _dustBaseRot = dust.transform.localRotation;
            }
            if (slash != null && !_slashBaseCaptured)
            {
                _slashBasePos = slash.transform.localPosition;
                _slashBaseScale = slash.transform.localScale;
                _slashBaseRot = slash.transform.localRotation;
                _slashBaseCaptured = true;
            }
            HideAll();
        }

        VFXLabController Lab => _lab != null ? _lab : (_lab = VFXLabController.Instance);

        // ---- phase hooks (wired to VFXLabEffectPlayer UnityEvents) -----------

        public void OnReset()
        {
            StopCo(ref _slashCo); StopCo(ref _starCo); StopCo(ref _glowCo); StopCo(ref _dustCo);
            HideAll();
            if (sparks != null) sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (embers != null) embers.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void OnAnticipation()
        {
            OnReset();
            if (anticipationGlow != null)
            {
                anticipationGlow.gameObject.SetActive(true);
                _glowCo = StartCoroutine(GlowRoutine());
            }
        }

        public void OnRelease()
        {
            StopCo(ref _glowCo);
            if (anticipationGlow != null) StartCoroutine(ConsumeGlow());
            if (slash != null)
            {
                slash.gameObject.SetActive(true);
                _slashCo = StartCoroutine(SlashRoutine());
            }
            if (Lab != null) Lab.SuppressBackground(bgPeak, bgUp, bgHold, bgDown);
        }

        public void OnImpact()
        {
            Vector3 ip = (Lab != null && Lab.impactPoint != null) ? Lab.impactPoint.position : transform.position;
            if (impactStar != null)
            {
                impactStar.gameObject.SetActive(true);
                // heroMode: the impact-burst art's bright contact core is off-centre in the texture,
                // so offset the quad to land that core on the impact point.
                impactStar.transform.position = ip + (heroMode ? impactCoreOffset : Vector3.zero);
                _starCo = StartCoroutine(StarRoutine());
            }
            if (sparks != null) { sparks.transform.position = ip; sparks.Clear(true); sparks.Play(true); }
            if (embers != null) { embers.transform.position = ip; embers.Clear(true); embers.Play(true); }
            if (dust != null)
            {
                if (Lab != null && Lab.impactPoint != null)
                {
                    var p = Lab.impactPoint.position; p.y -= 1.3f; dust.transform.position = p;
                }
                dust.gameObject.SetActive(true);
                _dustCo = StartCoroutine(DustRoutine());
            }
        }

        public void OnRecovery() { /* nothing extra — curves already decay to 0 */ }

        // ---- routines ------------------------------------------------------

        IEnumerator SlashRoutine()
        {
            float t = 0f, life = Mathf.Max(0.05f, slashLife);
            var str = slash != null ? slash.transform : null;
            while (t < life)
            {
                t += StepDt();
                float n = Mathf.Clamp01(t / life);
                if (heroMode)
                {
                    SetFloat(slash, ID_Reveal, heroReveal.Evaluate(n));
                    SetFloat(slash, ID_Burn, heroBurn.Evaluate(n));
                    SetFloat(slash, ID_Warp, Mathf.Max(0f, heroWarp.Evaluate(n)));
                    SetFloat(slash, ID_FrontHeat, Mathf.Max(0f, heroFrontHeat.Evaluate(n)));
                    SetFloat(slash, ID_Intensity, Mathf.Max(0f, heroIntensity.Evaluate(n)));
                    SetFloat(slash, ID_AlphaMul, Mathf.Clamp01(heroAlpha.Evaluate(n)));
                    if (str != null && _slashBaseCaptured)
                    {
                        // release smear: quad starts pulled back toward the attacker + X-compressed,
                        // snaps to its rest pose fast — the directional reveal does the rest.
                        float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(n / 0.14f), 3f);
                        str.localPosition = Vector3.LerpUnclamped(_slashBasePos + slashReleaseFrom, _slashBasePos, k);
                        var sc = _slashBaseScale; sc.x *= slashStretchX.Evaluate(n);
                        str.localScale = sc;
                    }
                }
                else if (flipbookMode)
                {
                    SetFloat(slash, ID_Frame, slashFrameCurve.Evaluate(n));
                    SetFloat(slash, ID_Intensity, Mathf.Max(0f, slashIntensity.Evaluate(n)) * 0.6f + 0.5f);
                    SetFloat(slash, ID_AlphaMul, Mathf.Clamp01(slashAlpha.Evaluate(n)));
                    if (str != null && _slashBaseCaptured)
                    {
                        // release smear: quad starts pulled back toward the attacker + compressed on X,
                        // then snaps to its rest pose — reads as a violent forward throw.
                        float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(n / 0.22f), 3f);   // fast ease to rest by n~0.22
                        str.localPosition = Vector3.LerpUnclamped(_slashBasePos + slashReleaseFrom, _slashBasePos, k);
                        var sc = _slashBaseScale; sc.x *= slashStretchX.Evaluate(n);
                        str.localScale = sc;
                    }
                }
                else
                {
                    SetFloat(slash, ID_Reveal, revealCurve.Evaluate(n));
                    SetFloat(slash, ID_Retract, Mathf.Clamp01(retractCurve.Evaluate(n)));
                    SetFloat(slash, ID_Burn, Mathf.Clamp01(burnCurve.Evaluate(n)));
                    SetFloat(slash, ID_Intensity, Mathf.Max(0f, slashIntensity.Evaluate(n)));
                    SetFloat(slash, ID_AlphaMul, Mathf.Clamp01(slashAlpha.Evaluate(n)));
                }
                yield return null;
            }
            SetFloat(slash, ID_Intensity, 0f);
            SetFloat(slash, ID_AlphaMul, 0f);
            if (str != null && _slashBaseCaptured)
            {
                str.localPosition = _slashBasePos;
                str.localScale = _slashBaseScale;
                str.localRotation = _slashBaseRot;
            }
            slash.gameObject.SetActive(false);
            _slashCo = null;
        }

        IEnumerator StarRoutine()
        {
            var tr = impactStar.transform;
            float t = 0f, life = Mathf.Max(0.05f, starLife);
            float z0 = tr.localEulerAngles.z;
            Vector3 p0 = tr.position;
            while (t < life)
            {
                t += StepDt();
                float n = Mathf.Clamp01(t / life);
                if (heroMode)
                {
                    // impact = the dedicated directional burst art (HeavyFireSlash_Impact_v1), revealed
                    // RADIALLY from its contact core: energy enters -> expands outward -> streaks extend
                    // forward -> fades from the core out. Scale punch + forward X-stretch.
                    float exp01 = Mathf.Clamp01(n / 0.26f);
                    SetFloat(impactStar, ID_Reveal, Mathf.Lerp(-0.05f, 1.55f, 1f - Mathf.Pow(1f - exp01, 2.7f)));
                    SetFloat(impactStar, ID_Burn, Mathf.Lerp(-0.15f, 1.55f, Mathf.Clamp01((n - 0.30f) / 0.58f)));
                    SetFloat(impactStar, ID_FrontHeat, Mathf.Lerp(2.6f, 0.2f, Mathf.Clamp01(n / 0.35f)));
                    SetFloat(impactStar, ID_Warp, Mathf.Lerp(0.02f, 0.006f, n));
                    SetFloat(impactStar, ID_Intensity, Mathf.Lerp(1.6f, 0f, Mathf.Clamp01((n - 0.26f) / 0.74f)));
                    SetFloat(impactStar, ID_AlphaMul, Mathf.Clamp01(1.2f - n * 1.1f));
                    float sc = starBaseScale * Mathf.Lerp(0.55f, 1.12f, 1f - Mathf.Pow(1f - Mathf.Clamp01(n / 0.35f), 3f));
                    // strongest streaks travel FURTHER forward — grow X only (no extra radial spread)
                    tr.localScale = new Vector3(sc * Mathf.Lerp(0.82f, 1.7f, n), sc, sc);
                    tr.rotation = Quaternion.identity;
                    tr.position = p0 + tr.right * (impactForwardDrift * (0.35f * n + 0.65f * n * n));
                }
                else if (flipbookMode)
                {
                    SetFloat(impactStar, ID_Frame, impactFrameCurve.Evaluate(n));
                    SetFloat(impactStar, ID_Intensity, Mathf.Max(0f, starIntensity.Evaluate(n)) * 0.5f + 0.5f);
                    SetFloat(impactStar, ID_AlphaMul, 1f);
                    tr.localScale = Vector3.one * (starBaseScale * Mathf.Lerp(0.75f, 1.35f, n));
                    tr.rotation = Quaternion.identity;
                    tr.position = p0 + tr.right * (impactForwardDrift * n);   // splash travels forward
                }
                else
                {
                    tr.localScale = Vector3.one * (starBaseScale * Mathf.Max(0.01f, starScale.Evaluate(n)));
                    tr.localRotation = Quaternion.Euler(0f, 0f, z0 + starSpin * n);
                    SetFloat(impactStar, ID_Intensity, Mathf.Max(0f, starIntensity.Evaluate(n)));
                    SetFloat(impactStar, ID_AlphaMul, 1f);
                }
                yield return null;
            }
            impactStar.gameObject.SetActive(false);
            _starCo = null;
        }

        IEnumerator GlowRoutine()
        {
            var tr = anticipationGlow.transform;
            float t = 0f;
            // stretch the glow build across the anticipation window (release fires OnRelease)
            float dur = 0.5f;
            var pl = GetComponent<VFXLabEffectPlayer>();
            if (pl != null) dur = Mathf.Max(0.05f, pl.releaseTime - pl.anticipationTime);
            while (true)
            {
                t += StepDt();
                float n = Mathf.Clamp01(t / dur);
                tr.localScale = _glowBaseScale * (glowMaxScale * Mathf.Max(0.02f, glowScale.Evaluate(n)));
                SetFloat(anticipationGlow, ID_Intensity, Mathf.Max(0f, glowIntensity.Evaluate(n)));
                SetFloat(anticipationGlow, ID_AlphaMul, 1f);
                yield return null;
            }
        }

        IEnumerator ConsumeGlow()
        {
            var tr = anticipationGlow.transform;
            Vector3 from = tr.localScale;
            float t = 0f, dur = Mathf.Max(0.02f, glowConsumeTime);
            float i0 = 1.9f;
            while (t < dur)
            {
                t += StepDt();
                float n = t / dur;
                tr.localScale = Vector3.LerpUnclamped(from * 1.15f, from * 0.05f, n * n);
                SetFloat(anticipationGlow, ID_Intensity, Mathf.Lerp(i0, 0f, n));
                yield return null;
            }
            anticipationGlow.gameObject.SetActive(false);
        }

        IEnumerator DustRoutine()
        {
            var tr = dust.transform;
            float t = 0f, life = Mathf.Max(0.05f, dustLife);
            while (t < life)
            {
                t += StepDt();
                float n = Mathf.Clamp01(t / life);
                tr.localScale = _dustBaseScale * Mathf.Lerp(0.3f, dustMaxScale, 1f - Mathf.Pow(1f - n, 2f));
                SetFloat(dust, ID_Intensity, Mathf.Lerp(0.9f, 0f, n));
                SetFloat(dust, ID_AlphaMul, Mathf.Lerp(1f, 0f, n * n));
                yield return null;
            }
            dust.gameObject.SetActive(false);
            _dustCo = null;
        }

        // ---- helpers -----------------------------------------------------

        void HideAll()
        {
            if (slash != null)
            {
                SetFloat(slash, ID_Reveal, heroMode ? -0.35f : 0f);
                SetFloat(slash, ID_Retract, 0f); SetFloat(slash, ID_Burn, heroMode ? -0.2f : 0f);
                SetFloat(slash, ID_Warp, 0f);
                SetFloat(slash, ID_Intensity, 0f); SetFloat(slash, ID_AlphaMul, 0f); SetFloat(slash, ID_Frame, 0f);
                if (_slashBaseCaptured)
                {
                    slash.transform.localPosition = _slashBasePos;
                    slash.transform.localScale = _slashBaseScale;
                    slash.transform.localRotation = _slashBaseRot;
                }
                slash.gameObject.SetActive(false);
            }
            if (impactStar != null)
            {
                SetFloat(impactStar, ID_Intensity, 0f);
                SetFloat(impactStar, ID_AlphaMul, 0f);
                SetFloat(impactStar, ID_Frame, 0f);
                impactStar.transform.localPosition = _starBasePos;
                impactStar.transform.localScale = _starBaseScale;
                impactStar.transform.localRotation = _starBaseRot;
                impactStar.gameObject.SetActive(false);
            }
            if (anticipationGlow != null)
            {
                SetFloat(anticipationGlow, ID_Intensity, 0f);
                SetFloat(anticipationGlow, ID_AlphaMul, 0f);
                anticipationGlow.transform.localPosition = _glowBasePos;
                anticipationGlow.transform.localScale = _glowBaseScale;
                anticipationGlow.transform.localRotation = _glowBaseRot;
                anticipationGlow.gameObject.SetActive(false);
            }
            if (dust != null)
            {
                SetFloat(dust, ID_Intensity, 0f);
                SetFloat(dust, ID_AlphaMul, 0f);
                dust.transform.localPosition = _dustBasePos;
                dust.transform.localScale = _dustBaseScale;
                dust.transform.localRotation = _dustBaseRot;
                dust.gameObject.SetActive(false);
            }
        }

        void SetFloat(Renderer r, int id, float v)
        {
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(id, v);
            r.SetPropertyBlock(_mpb);
        }

        void StopCo(ref Coroutine c) { if (c != null) { StopCoroutine(c); c = null; } }
    }
}

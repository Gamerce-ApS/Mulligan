using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MulliganCombatFeel
{
    /// <summary>
    /// Presentation-only target reactions: hit flash, squash/stretch, small knockback, rotation
    /// punch, reset to initial state. NO gameplay logic — no health, no damage, no rules.
    ///
    /// Works with either a world <see cref="Renderer"/> (SpriteRenderer / MeshRenderer) OR a UGUI
    /// <see cref="Graphic"/> (Image / RawImage). Assign whichever your target uses; if a Graphic is
    /// set it wins. Transform reactions animate localScale/localPosition/localRotation, which behave
    /// the same on a RectTransform — but UI local units are canvas units (often pixels), so
    /// <see cref="knockback"/> / <see cref="rotationPunch"/> usually need larger values in UI mode.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXLabTarget : MonoBehaviour
    {
        [Header("Visual — assign ONE (Graphic wins if both set)")]
        [Tooltip("World mode: the target's SpriteRenderer / MeshRenderer. Colour flash via MaterialPropertyBlock.")]
        public Renderer bodyRenderer;
        [Tooltip("UI mode: the target's Image / RawImage. Colour flash via Graphic.color.")]
        public Graphic bodyGraphic;
        public string colorProperty = "_Color";
        public Color flashColor = Color.white;

        [Header("Optional additive flash overlay — assign ONE")]
        [Tooltip("World: a Renderer on an additive material. UI: an Image on top (additive material or just white).")]
        public Renderer flashOverlay;
        public Graphic flashOverlayGraphic;
        public float flashOverlayPeak = 0.85f;

        [Header("Reaction defaults (at intensity 1)")]
        public float squash = 0.22f;                 // Y compresses by this fraction, X widens by 0.6x that
        [Tooltip("Local units. World: metres. UI: canvas units (often pixels) — expect to raise this a lot.")]
        public float knockback = 0.6f;
        public float rotationPunch = 8f;             // degrees (Z)
        public float flashTime = 0.06f;
        public float reactionTime = 0.26f;           // squash snap + settle
        public float knockbackReturnTime = 0.35f;

        Vector3 _baseScale, _basePos;
        Quaternion _baseRot;
        Color _baseColor = Color.white;
        MaterialPropertyBlock _mpb;
        int _colorId;
        Coroutine _reactCo, _flashCo;
        Coroutine _resetFailsafeCo;

        bool UI => bodyGraphic != null;

        void Awake()
        {
            _baseScale = transform.localScale;
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _colorId = Shader.PropertyToID(colorProperty);
            _mpb = new MaterialPropertyBlock();

            if (bodyGraphic == null && bodyRenderer == null)
            {
                bodyGraphic = GetComponentInChildren<Graphic>();
                if (bodyGraphic == null) bodyRenderer = GetComponentInChildren<Renderer>();
            }

            if (bodyGraphic != null)
                _baseColor = bodyGraphic.color;
            else if (bodyRenderer != null && bodyRenderer.sharedMaterial != null &&
                     bodyRenderer.sharedMaterial.HasProperty(_colorId))
                _baseColor = bodyRenderer.sharedMaterial.GetColor(_colorId);
        }

        /// <summary>Full hit reaction: flash + squash + knockback + rotation punch.
        /// <paramref name="fromDir"/> = attacker -> target direction (world space).</summary>
        public void PlayHitReaction(Vector3 fromDir, float intensity = 1f)
        {
            float i = Mathf.Max(0f, intensity);
            Vector3 dir = fromDir.sqrMagnitude > 0.0001f ? fromDir.normalized : Vector3.right;
            HitFlash(i);
            if (_reactCo != null) StopCoroutine(_reactCo);
            _reactCo = StartCoroutine(ReactRoutine(dir, i));
            if (_resetFailsafeCo != null) StopCoroutine(_resetFailsafeCo);
            _resetFailsafeCo = StartCoroutine(ResetAfterRealtime(reactionTime + knockbackReturnTime + 0.25f));
        }

        public void HitFlash(float intensity = 1f)
        {
            if (bodyGraphic == null && bodyRenderer == null) return;
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(FlashRoutine(Mathf.Clamp01(intensity)));
        }

        IEnumerator FlashRoutine(float i)
        {
            float t = 0f, dur = Mathf.Max(0.02f, flashTime);
            SetOverlayActive(true);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = (1f - t / dur) * i;
                SetColor(Color.Lerp(_baseColor, flashColor, k));
                SetOverlayAlpha(k * flashOverlayPeak);
                yield return null;
            }
            SetColor(_baseColor);
            SetOverlayAlpha(0f);
            SetOverlayActive(false);
            _flashCo = null;
        }

        void SetOverlayActive(bool on)
        {
            if (flashOverlayGraphic != null) flashOverlayGraphic.gameObject.SetActive(on);
            else if (flashOverlay != null) flashOverlay.gameObject.SetActive(on);
        }

        void SetOverlayAlpha(float a)
        {
            a = Mathf.Clamp01(a);
            if (flashOverlayGraphic != null)
            {
                var c = flashOverlayGraphic.color; c.a = a; flashOverlayGraphic.color = c;
                return;
            }
            if (flashOverlay == null) return;
            var sr = flashOverlay as SpriteRenderer;
            if (sr != null) { var c = sr.color; c.a = a; sr.color = c; return; }
            flashOverlay.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, new Color(1f, 1f, 1f, a));
            flashOverlay.SetPropertyBlock(_mpb);
        }

        IEnumerator ReactRoutine(Vector3 dir, float i)
        {
            float sq = squash * i;
            Vector3 hitScale = new Vector3(_baseScale.x * (1f + sq * 0.6f), _baseScale.y * (1f - sq), _baseScale.z);
            Vector3 kbTarget = _basePos + transform.InverseTransformDirection(dir) * (knockback * i);
            float rp = rotationPunch * i * (dir.x >= 0f ? -1f : 1f);

            float t = 0f, snap = Mathf.Max(0.02f, reactionTime * 0.35f);
            while (t < snap)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / snap);
                transform.localScale = Vector3.Lerp(_baseScale, hitScale, k);
                transform.localPosition = Vector3.Lerp(_basePos, kbTarget, k);
                transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, rp, k));
                yield return null;
            }

            t = 0f;
            float settle = Mathf.Max(0.02f, reactionTime * 0.65f + knockbackReturnTime);
            while (t < settle)
            {
                t += Time.deltaTime;
                float k = EaseOutCubic(t / settle);
                transform.localScale = Vector3.LerpUnclamped(hitScale, _baseScale, k);
                transform.localPosition = Vector3.LerpUnclamped(kbTarget, _basePos, k);
                transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(rp, 0f, k));
                yield return null;
            }

            transform.localScale = _baseScale;
            transform.localPosition = _basePos;
            transform.localRotation = _baseRot;
            _reactCo = null;
        }

        IEnumerator ResetAfterRealtime(float seconds)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, seconds));
            _resetFailsafeCo = null;
            ResetTarget();
        }

        public void ResetTarget()
        {
            if (_reactCo != null) { StopCoroutine(_reactCo); _reactCo = null; }
            if (_flashCo != null) { StopCoroutine(_flashCo); _flashCo = null; }
            if (_resetFailsafeCo != null) { StopCoroutine(_resetFailsafeCo); _resetFailsafeCo = null; }
            transform.localScale = _baseScale;
            transform.localPosition = _basePos;
            transform.localRotation = _baseRot;
            SetColor(_baseColor);
            SetOverlayAlpha(0f);
            SetOverlayActive(false);
        }

        void SetColor(Color c)
        {
            if (bodyGraphic != null) { bodyGraphic.color = c; return; }
            if (bodyRenderer == null) return;
            bodyRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(_colorId, c);
            bodyRenderer.SetPropertyBlock(_mpb);
        }

        static float EaseOutCubic(float x) { x = Mathf.Clamp01(x); float f = 1f - x; return 1f - f * f * f; }
        static float EaseOutBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float f = x - 1f;
            return 1f + c3 * f * f * f + c1 * f * f;
        }
    }
}

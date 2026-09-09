using System.Collections;
using UnityEngine;

namespace MulliganCombatFeel
{
    /// <summary>
    /// Presentation-only camera punch / shake for the VFX Lab. No gameplay logic, no third-party deps.
    /// Keep the base camera pose on a parent rig; this component offsets <see cref="shakeTarget"/>
    /// (the camera itself) and always returns it to its captured local pose.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXLabCameraController : MonoBehaviour
    {
        [Tooltip("Transform that receives the punch offset. Defaults to this GameObject.")]
        public Transform shakeTarget;

        [Header("Punch defaults (at intensity 1)")]
        public float positionalPunch = 0.35f;   // local units
        public float rotationalPunch = 2.5f;    // degrees (Z roll)
        public float duration = 0.28f;          // seconds
        [Range(0f, 1f)] public float globalIntensity = 1f;

        AnimationCurve _falloff;
        Vector3 _basePos;
        Quaternion _baseRot;
        Coroutine _co;

        void Awake()
        {
            if (shakeTarget == null) shakeTarget = transform;
            _basePos = shakeTarget.localPosition;
            _baseRot = shakeTarget.localRotation;
            _falloff = new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.14f, 1f), new Keyframe(1f, 0f));
        }

        /// <summary>Directional camera punch. <paramref name="hitDir"/> = attacker -> target direction.</summary>
        public void Punch(Vector3 hitDir, float intensity = 1f)
        {
            float i = Mathf.Max(0f, intensity) * globalIntensity;
            if (i <= 0f) return;
            Vector3 dir = hitDir.sqrMagnitude > 0.0001f ? hitDir.normalized : Vector3.right;
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(PunchRoutine(dir, i));
        }

        IEnumerator PunchRoutine(Vector3 dir, float i)
        {
            float t = 0f;
            float dur = Mathf.Max(0.02f, duration * Mathf.Lerp(0.75f, 1f, i));
            Vector3 kick = -dir * (positionalPunch * i);              // recoil opposite the hit
            float rollSign = dir.x >= 0f ? -1f : 1f;
            while (t < dur)
            {
                t += Time.deltaTime;                                   // scaled: follows playback speed + hit stop
                float k = _falloff.Evaluate(Mathf.Clamp01(t / dur));
                Vector3 jitter = new Vector3(
                    Mathf.PerlinNoise(t * 43f, 0.3f) - 0.5f,
                    Mathf.PerlinNoise(0.7f, t * 47f) - 0.5f, 0f) * (positionalPunch * 0.3f * i);
                shakeTarget.localPosition = _basePos + (kick + jitter) * k;
                shakeTarget.localRotation = _baseRot * Quaternion.Euler(0f, 0f, rollSign * rotationalPunch * i * k);
                yield return null;
            }
            shakeTarget.localPosition = _basePos;
            shakeTarget.localRotation = _baseRot;
            _co = null;
        }

        public void SetIntensity(float intensity) => globalIntensity = Mathf.Clamp01(intensity);

        public void ResetCamera()
        {
            if (_co != null) { StopCoroutine(_co); _co = null; }
            if (shakeTarget != null)
            {
                shakeTarget.localPosition = _basePos;
                shakeTarget.localRotation = _baseRot;
            }
        }
    }
}

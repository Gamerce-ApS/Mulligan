using UnityEngine;

namespace MulliganCombatFeel
{
    /// <summary>
    /// Sandbox-only manual trigger for the VFX Lab / CombatLab scene. Presentation only — NO
    /// gameplay logic, not referenced by anything in production. Lets you fire the full attack
    /// repeatedly by hand while the editor is in Play Mode:
    ///
    ///   • press <see cref="triggerKey"/> (default Space) — play the full attack from the start
    ///   • press <see cref="resetKey"/> (default R)       — reset the lab to idle
    ///   • tick <see cref="autoRepeat"/>                  — fire it on a loop, hands-free
    ///   • on-screen buttons (bottom-left) while playing
    ///   • Inspector "Trigger Attack" button / component context-menu (see the custom editor)
    ///
    /// Uses the legacy Input Manager (project activeInputHandler = 0), so no Input System dependency.
    /// </summary>
    [DisallowMultipleComponent]
    public class VFXLabManualTrigger : MonoBehaviour
    {
        [Tooltip("Controller to drive. Auto-found on this GameObject / in the scene if left empty.")]
        public VFXLabController controller;

        [Header("Keyboard (Play Mode)")]
        public KeyCode triggerKey = KeyCode.Space;
        public KeyCode resetKey = KeyCode.R;

        [Header("Auto-repeat (hands-free testing)")]
        public bool autoRepeat = false;
        [Tooltip("Seconds between automatic re-triggers when Auto Repeat is on.")]
        [Min(0.1f)] public float repeatInterval = 1.6f;

        [Header("On-screen buttons")]
        public bool showOnScreenButtons = true;

        float _nextAutoFire;

        void Reset()   => controller = ResolveController();
        void OnEnable() { if (controller == null) controller = ResolveController(); }

        VFXLabController ResolveController()
        {
            var c = GetComponent<VFXLabController>();
            if (c == null) c = GetComponentInParent<VFXLabController>();
#if UNITY_2023_1_OR_NEWER
            if (c == null) c = FindFirstObjectByType<VFXLabController>();
#else
            if (c == null) c = FindObjectOfType<VFXLabController>();
#endif
            if (c == null) c = VFXLabController.Instance;
            return c;
        }

        void Update()
        {
            if (controller == null) { controller = ResolveController(); if (controller == null) return; }

            if (Input.GetKeyDown(triggerKey)) TriggerAttack();
            if (Input.GetKeyDown(resetKey))   ResetLab();

            if (autoRepeat && Time.unscaledTime >= _nextAutoFire)
            {
                TriggerAttack();
                _nextAutoFire = Time.unscaledTime + Mathf.Max(0.1f, repeatInterval);
            }
        }

        /// <summary>Play the full attack (anticipation → release → impact → recovery) from the start.</summary>
        [ContextMenu("Trigger Attack")]
        public void TriggerAttack()
        {
            var c = controller != null ? controller : ResolveController();
            if (c == null) { Debug.LogWarning("[VFXLabManualTrigger] No VFXLabController found."); return; }
            c.Play();
        }

        [ContextMenu("Reset Lab")]
        public void ResetLab()
        {
            var c = controller != null ? controller : ResolveController();
            if (c != null) c.ResetLab();
        }

        void OnGUI()
        {
            if (!showOnScreenButtons || !Application.isPlaying) return;

            const float w = 190f, h = 40f, pad = 12f;
            float x = pad, y = Screen.height - pad - h * 2f - 6f;

            GUI.color = Color.white;
            var big = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold };

            string label = "▶  Trigger Attack  (" + triggerKey + ")";
            if (GUI.Button(new Rect(x, y, w, h), label, big)) TriggerAttack();
            if (GUI.Button(new Rect(x, y + h + 6f, w, h), "↺  Reset  (" + resetKey + ")", big)) ResetLab();

            bool now = GUI.Toggle(new Rect(x + w + 12f, y, 150f, h), autoRepeat,
                                  "  Auto-repeat");
            if (now != autoRepeat) { autoRepeat = now; _nextAutoFire = Time.unscaledTime; }
        }
    }
}

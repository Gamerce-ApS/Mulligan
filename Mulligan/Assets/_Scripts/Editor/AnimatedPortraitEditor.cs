using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimatedPortrait))]
public class AnimatedPortraitEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AnimatedPortrait portrait = target as AnimatedPortrait;
        if (portrait == null)
            return;

        EditorGUILayout.Space();

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Head Rotation Noise", portrait.LastHeadRotationNoise.ToString("0.00"));
            EditorGUILayout.LabelField("Head Rotation", portrait.LastHeadRotation.ToString("0.00"));
            EditorGUILayout.LabelField("Head Target", portrait.LastHeadTargetRotation.ToString("0.00"));
        }

        if (portrait.SpringParts != null && portrait.SpringParts.Count > 0)
        {
            EditorGUILayout.LabelField("Spring Debug", EditorStyles.boldLabel);
            for (int i = 0; i < portrait.SpringParts.Count; i++)
            {
                AnimatedPortraitSpringPart spring = portrait.SpringParts[i];
                if (spring == null)
                    continue;

                EditorGUILayout.LabelField(spring.name, "Driver: " + (spring.HasValidDriver ? "OK" : "Missing"));
                if (Application.isPlaying)
                    EditorGUILayout.LabelField("Angle", spring.CurrentAngle.ToString("0.00") + " / target " + spring.LastTargetAngle.ToString("0.00"));
            }

            EditorGUILayout.Space();
        }

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Idle"))
                portrait.PlayIdle();
            if (GUILayout.Button("Stop Idle"))
                portrait.StopIdle();
            if (GUILayout.Button("Reset Pose"))
                portrait.ResetPose();
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button("Reset Pose"))
            {
                portrait.ResetPose();
                EditorUtility.SetDirty(portrait);
            }
        }
    }
}

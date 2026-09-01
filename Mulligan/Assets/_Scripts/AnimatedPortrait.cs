using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatedPortrait : MonoBehaviour
{
    [System.Serializable]
    public class PortraitTransformPose
    {
        public RectTransform Target;
        public Vector3 AnchoredPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    public bool playOnEnable = true;
    public bool useManifestAnimationSettings = true;
    public bool debugExaggerateMotion = false;
    public float GlobalIntensity = 0.5f;

    [Header("Source")]
    public int SourceWidth = 1;
    public int SourceHeight = 1;

    [Header("Breathing")]
    public bool BreathingEnabled = true;
    public float BreathingAmount = 0.005f;
    public float BreathingSpeed = 1f;
    public float BreathingAmountMultiplier = 1f;
    public float BreathingSpeedMultiplier = 1f;

    [Header("Head")]
    public bool HeadEnabled = true;
    public float HeadPositionAmount = 15f;
    public float HeadRotationAmount = 5f;
    public float MaxHeadRotation = 10f;
    public float HeadSpeed = 0.15f;
    public float HeadPositionMultiplier = 1f;
    public float HeadRotationMultiplier = 12f;
    public float HeadSpeedMultiplier = 5f;

    [Header("Blink")]
    public bool BlinkEnabled = true;
    public float BlinkMinInterval = 3f;
    public float BlinkMaxInterval = 7f;
    public float BlinkClosedDuration = 0.1f;
    public float BlinkIntervalMultiplier = 1f;
    public float DoubleBlinkChance = 0.12f;

    [Header("Spring")]
    public float GlobalSpringMultiplier = 10f;

    [Header("References")]
    public RectTransform Body;
    public RectTransform HeadPivot;
    public List<GameObject> BlinkOverlays = new List<GameObject>();
    public List<AnimatedPortraitSpringPart> SpringParts = new List<AnimatedPortraitSpringPart>();
    public List<PortraitTransformPose> OriginalPoses = new List<PortraitTransformPose>();
    public float LastHeadRotation;
    public float LastHeadTargetRotation;
    public float LastHeadRotationNoise;

    private bool idlePlaying;
    private bool paused;
    private float seedX;
    private float seedY;
    private float seedRot;
    private float seedEnvelope;
    private float breathingPhase;
    private float headFrequencyVariation = 1f;
    private Vector3 smoothedHeadOffset;
    private float smoothedHeadRotation;
    private int blinkCounter;
    private Coroutine blinkRoutine;

    void Awake()
    {
        if (OriginalPoses == null || OriginalPoses.Count == 0)
            CaptureOriginalPoses();

        int stableSeed = Mathf.Abs(GetStableSeed());
        seedX = Mathf.Repeat(stableSeed * 0.0137f, 997f);
        seedY = Mathf.Repeat(stableSeed * 0.0271f + 42f, 997f);
        seedRot = Mathf.Repeat(stableSeed * 0.0393f + 91f, 997f);
        seedEnvelope = Mathf.Repeat(stableSeed * 0.0189f + 123f, 997f);
        breathingPhase = Mathf.Repeat(stableSeed * 0.071f, Mathf.PI * 2f);
        headFrequencyVariation = Mathf.Lerp(0.88f, 1.12f, Mathf.PerlinNoise(seedEnvelope, 0.37f));
    }

    void OnEnable()
    {
        if (playOnEnable)
            PlayIdle();
    }

    void OnDisable()
    {
        StopIdle();
    }

    void LateUpdate()
    {
        if (idlePlaying == false || paused)
            return;

        float intensity = Mathf.Max(0f, GlobalIntensity);
        UpdateBreathing(intensity);
        UpdateHead(intensity);

        float debugMultiplier = debugExaggerateMotion ? 4f : 1f;
        for (int i = 0; i < SpringParts.Count; i++)
        {
            if (SpringParts[i] != null)
                SpringParts[i].Tick(intensity, GlobalSpringMultiplier * debugMultiplier);
        }
    }

    public void ApplyManifest(PortraitManifest manifest)
    {
        if (manifest == null || useManifestAnimationSettings == false)
            return;

        SourceWidth = Mathf.Max(1, manifest.sourceWidth);
        SourceHeight = Mathf.Max(1, manifest.sourceHeight);

        if (manifest.animation == null)
            return;

        if (manifest.animation.breathing != null)
        {
            BreathingEnabled = manifest.animation.breathing.enabled;
            BreathingAmount = manifest.animation.breathing.amount;
            BreathingSpeed = manifest.animation.breathing.speed;
        }

        if (manifest.animation.head != null)
        {
            HeadEnabled = manifest.animation.head.enabled;
            HeadPositionAmount = manifest.animation.head.positionAmount;
            HeadRotationAmount = manifest.animation.head.rotationAmount;
            HeadSpeed = manifest.animation.head.speed;
        }

        if (manifest.animation.blink != null)
        {
            BlinkEnabled = manifest.animation.blink.enabled;
            BlinkMinInterval = manifest.animation.blink.minInterval;
            BlinkMaxInterval = manifest.animation.blink.maxInterval;
            BlinkClosedDuration = manifest.animation.blink.closedDuration;
            DoubleBlinkChance = manifest.animation.blink.doubleBlinkChance;
        }
    }

    public void PlayIdle()
    {
        idlePlaying = true;
        paused = false;
        blinkCounter = 0;

        if (BlinkEnabled && blinkRoutine == null && gameObject.activeInHierarchy)
            blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    public void StopIdle()
    {
        idlePlaying = false;

        if (blinkRoutine != null)
        {
            StopCoroutine(blinkRoutine);
            blinkRoutine = null;
        }

        ResetPose();
    }

    public void ResetPose()
    {
        for (int i = 0; i < OriginalPoses.Count; i++)
        {
            PortraitTransformPose pose = OriginalPoses[i];
            if (pose == null || pose.Target == null)
                continue;

            pose.Target.anchoredPosition3D = pose.AnchoredPosition;
            pose.Target.localRotation = pose.LocalRotation;
            pose.Target.localScale = pose.LocalScale;
        }

        for (int i = 0; i < SpringParts.Count; i++)
        {
            if (SpringParts[i] != null)
                SpringParts[i].ResetSpring();
        }

        smoothedHeadOffset = Vector3.zero;
        smoothedHeadRotation = 0f;
        SetBlinkOverlays(false);
    }

    public void SetIntensity(float intensity)
    {
        GlobalIntensity = Mathf.Max(0f, intensity);
    }

    public void SetPaused(bool isPaused)
    {
        paused = isPaused;
    }

    public void CaptureOriginalPoses()
    {
        OriginalPoses.Clear();
        RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            OriginalPoses.Add(new PortraitTransformPose
            {
                Target = rects[i],
                AnchoredPosition = rects[i].anchoredPosition3D,
                LocalRotation = rects[i].localRotation,
                LocalScale = rects[i].localScale
            });
        }
    }

    private void UpdateBreathing(float intensity)
    {
        if (BreathingEnabled == false || Body == null)
            return;

        PortraitTransformPose pose = GetPose(Body);
        if (pose == null)
            return;

        float time = Time.time * BreathingSpeed * BreathingSpeedMultiplier + breathingPhase;
        float baseWave = Mathf.Sin(time);
        float secondaryWave = Mathf.Sin(time * 2f + 1.7f) * 0.16f;
        float shapedWave = Mathf.Sign(baseWave) * Mathf.Pow(Mathf.Abs(baseWave), 0.72f);
        float wave = shapedWave + secondaryWave;
        float sourcePixels = Mathf.Abs(BreathingAmount) < 1f ? BreathingAmount * SourceHeight * 0.6f : BreathingAmount;
        float verticalOffset = wave * sourcePixels * BreathingAmountMultiplier * GetSourceToLocalScale() * intensity;
        Body.anchoredPosition3D = pose.AnchoredPosition + new Vector3(0f, verticalOffset, 0f);
    }

    private void UpdateHead(float intensity)
    {
        if (HeadEnabled == false || HeadPivot == null)
            return;

        PortraitTransformPose pose = GetPose(HeadPivot);
        if (pose == null)
            return;

        float time = Time.time * HeadSpeed * HeadSpeedMultiplier * headFrequencyVariation;
        float movementEnvelope = GetMovementEnvelope(time);
        float debugMultiplier = debugExaggerateMotion ? 4f : 1f;

        float x = GetLayeredNoise(seedX, time, 0.82f, 1.73f, 0.75f, 0.25f);
        float y = GetLayeredNoise(seedY, time, 0.57f, 1.31f, 0.82f, 0.18f);
        float independentRotation = GetRotationNoise(time);
        float r = GetHeadTiltNoise(x, independentRotation);
        float pixelScale = GetSourceToLocalScale();

        Vector3 targetOffset = new Vector3(
            x * HeadPositionAmount * HeadPositionMultiplier * pixelScale * intensity * movementEnvelope * debugMultiplier,
            y * HeadPositionAmount * 0.6f * HeadPositionMultiplier * pixelScale * intensity * movementEnvelope * debugMultiplier,
            0f);

        float safeRotationAmount = HeadRotationAmount * HeadRotationMultiplier;
        float rotationEnvelope = Mathf.Lerp(0.65f, 1f, movementEnvelope);
        float targetRotation = r * safeRotationAmount * intensity * rotationEnvelope;
        targetRotation = Mathf.Clamp(targetRotation, -MaxHeadRotation, MaxHeadRotation) * debugMultiplier;
        LastHeadRotationNoise = r;
        LastHeadTargetRotation = targetRotation;

        float smoothing = 1f - Mathf.Exp(-Time.deltaTime * 2.2f);
        smoothedHeadOffset = Vector3.Lerp(smoothedHeadOffset, targetOffset, smoothing);
        smoothedHeadRotation = Mathf.Lerp(smoothedHeadRotation, targetRotation, smoothing);
        LastHeadRotation = smoothedHeadRotation;

        HeadPivot.anchoredPosition3D = pose.AnchoredPosition + smoothedHeadOffset;
        HeadPivot.localRotation = pose.LocalRotation * Quaternion.Euler(0f, 0f, smoothedHeadRotation);
    }

    private IEnumerator BlinkRoutine()
    {
        while (idlePlaying)
        {
            float interval = GetBlinkInterval();
            yield return new WaitForSeconds(interval);

            if (idlePlaying == false || paused || BlinkEnabled == false)
                continue;

            yield return DoBlink(GetBlinkDuration());

            float doubleBlinkChance = Mathf.Clamp(DoubleBlinkChance, 0.08f, 0.15f);
            if (GetSeededBlinkValue(37) <= doubleBlinkChance)
            {
                yield return new WaitForSeconds(Mathf.Lerp(0.08f, 0.16f, GetSeededBlinkValue(53)));
                yield return DoBlink(GetBlinkDuration());
            }

            blinkCounter++;
        }

        blinkRoutine = null;
    }

    private IEnumerator DoBlink(float duration)
    {
        SetBlinkOverlays(true);
        yield return new WaitForSeconds(duration);
        SetBlinkOverlays(false);
    }

    private float GetLayeredNoise(float seed, float time, float slowFrequency, float secondaryFrequency, float slowAmount, float secondaryAmount)
    {
        float slow = (Mathf.PerlinNoise(seed, time * slowFrequency) - 0.5f) * 2f;
        float secondary = (Mathf.PerlinNoise(seed + 19.37f, time * secondaryFrequency + 4.11f) - 0.5f) * 2f;
        return Mathf.Clamp((slow * slowAmount) + (secondary * secondaryAmount), -1f, 1f);
    }

    private float GetRotationNoise(float time)
    {
        float slowDrift = Mathf.Sin((time * 1.37f) + seedRot) * 0.58f;
        float settleDrift = Mathf.Sin((time * 0.53f) + seedRot * 0.37f) * 0.27f;
        float tinyCorrection = (Mathf.PerlinNoise(seedRot + 19.37f, time * 0.91f + 4.11f) - 0.5f) * 0.3f;
        return Mathf.Clamp(slowDrift + settleDrift + tinyCorrection, -1f, 1f);
    }

    private float GetHeadTiltNoise(float horizontalMotion, float independentRotation)
    {
        float leanFromMovement = -horizontalMotion * 0.75f;
        float idleTilt = independentRotation * 0.45f;
        return Mathf.Clamp(leanFromMovement + idleTilt, -1f, 1f);
    }

    private float GetMovementEnvelope(float time)
    {
        float slow = Mathf.PerlinNoise(seedEnvelope, time * 0.08f);
        float secondary = Mathf.PerlinNoise(seedEnvelope + 31.7f, time * 0.19f);
        float envelope = (slow * 0.82f) + (secondary * 0.18f);
        envelope = Mathf.SmoothStep(0f, 1f, envelope);
        return Mathf.Lerp(0.25f, 1f, envelope);
    }

    private float GetBlinkInterval()
    {
        float min = Mathf.Max(0.5f, BlinkMinInterval);
        float max = Mathf.Max(min + 0.1f, BlinkMaxInterval);
        float variation = Mathf.Lerp(min, max, GetSeededBlinkValue(11));
        variation += Mathf.Lerp(-0.35f, 0.45f, GetSeededBlinkValue(23));
        return Mathf.Max(0.5f, variation * Mathf.Max(0.05f, BlinkIntervalMultiplier));
    }

    private float GetBlinkDuration()
    {
        if (GetSeededBlinkValue(71) < 0.16f)
            return Mathf.Lerp(0.13f, 0.18f, GetSeededBlinkValue(83));

        if (BlinkClosedDuration > 0f)
            return Mathf.Clamp(BlinkClosedDuration + Mathf.Lerp(-0.025f, 0.025f, GetSeededBlinkValue(97)), 0.07f, 0.11f);

        return Mathf.Lerp(0.07f, 0.11f, GetSeededBlinkValue(109));
    }

    private float GetSeededBlinkValue(int offset)
    {
        return Mathf.PerlinNoise(seedEnvelope + offset, blinkCounter * 1.731f + offset * 0.137f);
    }

    private void SetBlinkOverlays(bool active)
    {
        for (int i = 0; i < BlinkOverlays.Count; i++)
        {
            if (BlinkOverlays[i] != null)
                BlinkOverlays[i].SetActive(active);
        }
    }

    private PortraitTransformPose GetPose(RectTransform target)
    {
        for (int i = 0; i < OriginalPoses.Count; i++)
        {
            if (OriginalPoses[i] != null && OriginalPoses[i].Target == target)
                return OriginalPoses[i];
        }

        PortraitTransformPose pose = new PortraitTransformPose
        {
            Target = target,
            AnchoredPosition = target.anchoredPosition3D,
            LocalRotation = target.localRotation,
            LocalScale = target.localScale
        };
        OriginalPoses.Add(pose);
        return pose;
    }

    private float GetSourceToLocalScale()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null || SourceWidth <= 0)
            return 1f;

        return rect.rect.width / SourceWidth;
    }

    private int GetStableSeed()
    {
        unchecked
        {
            int hash = 17;
            string seedName = gameObject.name;
            for (int i = 0; i < seedName.Length; i++)
                hash = hash * 31 + seedName[i];

            hash = hash * 31 + SourceWidth;
            hash = hash * 31 + SourceHeight;
            return hash;
        }
    }
}

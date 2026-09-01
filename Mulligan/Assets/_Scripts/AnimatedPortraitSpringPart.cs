using UnityEngine;

public class AnimatedPortraitSpringPart : MonoBehaviour
{
    public RectTransform SpringPivot;
    public RectTransform DriverTransform;
    public RectTransform FollowTarget;
    public float Strength = 12f;
    public float Damping = 6f;
    public float MaxRotation = 2f;
    public float PositionResponse = 1.2f;
    public float VerticalResponse = 0.25f;
    public float RotationResponse = 1.8f;
    public float DeadZone = 0.015f;
    public bool HasValidDriver
    {
        get { return SpringPivot != null && GetDriverTransform() != null; }
    }
    public float LastTargetAngle;
    public float CurrentAngle
    {
        get { return currentAngle; }
    }

    private Vector3 lastTargetPosition;
    private Vector3 restDriverPosition;
    private Quaternion restDriverRotation;
    private Quaternion restLocalRotation;
    private float lastDriverRotation;
    private float currentAngle;
    private float angularVelocity;
    private bool initialized;

    public void Init(RectTransform springPivot, RectTransform followTarget, PortraitSpringDefinition spring)
    {
        SpringPivot = springPivot;
        DriverTransform = followTarget;
        FollowTarget = followTarget;

        if (spring != null)
        {
            Strength = spring.strength;
            Damping = spring.damping;
            MaxRotation = spring.maxRotation;
        }

        ResetSpring();
    }

    public void ResetSpring()
    {
        currentAngle = 0f;
        angularVelocity = 0f;

        RectTransform driver = GetDriverTransform();
        if (driver != null)
        {
            restDriverPosition = driver.anchoredPosition3D;
            restDriverRotation = driver.localRotation;
            lastTargetPosition = driver.anchoredPosition3D;
            lastDriverRotation = driver.localEulerAngles.z;
        }

        if (SpringPivot != null)
        {
            restLocalRotation = SpringPivot.localRotation;
            SpringPivot.localRotation = restLocalRotation;
        }

        initialized = true;
    }

    public void Tick(float intensity, float globalSpringMultiplier)
    {
        RectTransform driver = GetDriverTransform();
        if (SpringPivot == null || driver == null)
            return;

        if (initialized == false)
            ResetSpring();

        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        Vector3 driverPosition = driver.anchoredPosition3D;
        Vector3 driverVelocity = (driverPosition - lastTargetPosition) / deltaTime;
        lastTargetPosition = driverPosition;

        float driverAngularVelocity = Mathf.DeltaAngle(lastDriverRotation, driver.localEulerAngles.z) / deltaTime;
        lastDriverRotation = driver.localEulerAngles.z;

        float positionalReaction = (-driverVelocity.x * PositionResponse) + (-driverVelocity.y * VerticalResponse);
        float rotationReaction = -driverAngularVelocity * RotationResponse * 0.12f;
        float targetRotation = (positionalReaction + rotationReaction) * intensity * globalSpringMultiplier;

        if (Mathf.Abs(targetRotation) < DeadZone)
            targetRotation = 0f;

        targetRotation = Mathf.Clamp(targetRotation, -MaxRotation, MaxRotation);
        LastTargetAngle = targetRotation;

        float force = (targetRotation - currentAngle) * Strength;
        angularVelocity += force * deltaTime;
        angularVelocity *= Mathf.Max(0f, 1f - Damping * deltaTime);
        currentAngle += angularVelocity * deltaTime;

        if (Mathf.Abs(currentAngle) < DeadZone && Mathf.Abs(angularVelocity) < DeadZone)
        {
            currentAngle = 0f;
            angularVelocity = 0f;
        }

        currentAngle = Mathf.Clamp(currentAngle, -MaxRotation, MaxRotation);

        SpringPivot.localRotation = restLocalRotation * Quaternion.Euler(0f, 0f, currentAngle);
    }

    private RectTransform GetDriverTransform()
    {
        if (DriverTransform != null)
            return DriverTransform;

        return FollowTarget;
    }
}

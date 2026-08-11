using UnityEngine;

public class ScaleInOnAwake : MonoBehaviour
{
    public float Delay = 0f;
    public float Duration = 0.35f;

    private Vector3 targetScale;

    void Awake()
    {
        targetScale = transform.localScale;
        transform.localScale = Vector3.zero;

        LeanTween.scale(gameObject, targetScale, Duration)
            .setEaseOutBack()
            .setDelay(Delay);
    }
}

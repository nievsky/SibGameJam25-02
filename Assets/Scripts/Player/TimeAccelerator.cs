using UnityEngine;

public class TimeAccelerator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIController uiController;
    [SerializeField] private Drinkable drinkable;

    [Header("Time Scale")]
    [Tooltip("Time scale when Drunk = 0.")]
    [Min(0f)] [SerializeField] private float baseScale = 1f;

    [Tooltip("Maximum time scale when Drunk = 100.")]
    [Min(0f)] [SerializeField] private float maxScale = 6f;

    [Tooltip("Smoothly approach the target time scale (units: 1/seconds).")]
    [Min(0f)] [SerializeField] private float changeSpeed = 5f;

    [Tooltip("Curve mapping Drunk (0..1) to influence (0..1).")]
    [SerializeField] private AnimationCurve drunkToScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private float _currentScale;

    private void Reset()
    {
        uiController = FindObjectOfType<UIController>();
        drinkable = FindObjectOfType<Drinkable>();
    }

    private void Awake()
    {
        _currentScale = Mathf.Max(0f, baseScale);
        if (uiController != null)
            uiController.SetTimeScale(_currentScale);
    }

    private void Update()
    {
        if (uiController == null || drinkable == null) return;

        // Normalize drunk [0..100] -> [0..1]
        float drunk01 = Mathf.Clamp01(drinkable.Drunk / 100f);

        // Map via curve and then lerp between base and max
        float t = Mathf.Clamp01(drunkToScaleCurve.Evaluate(drunk01));
        float targetScale = Mathf.Lerp(baseScale, Mathf.Max(baseScale, maxScale), t);

        // Exponential smoothing toward target
        float k = 1f - Mathf.Exp(-changeSpeed * Time.deltaTime);
        _currentScale = Mathf.Lerp(_currentScale, targetScale, k);

        uiController.SetTimeScale(_currentScale);
    }

    // Optional setters if you want to tweak at runtime
    public void SetBaseScale(float value) => baseScale = Mathf.Max(0f, value);
    public void SetMaxScale(float value) => maxScale = Mathf.Max(0f, value);
    public void SetChangeSpeed(float value) => changeSpeed = Mathf.Max(0f, value);
}
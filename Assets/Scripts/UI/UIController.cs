// csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("UI time")]
    public TMP_Text timeText;
    
    [Header("UI Bar")]
    public Slider drunkSlider;

    [Header("Config")]
    [SerializeField] private int startHour = 0;      // 00:00
    [SerializeField] private int endHour = 8;        // 08:00 AM
    [SerializeField] private float minutesPerSecond = 10f; // base: +10 minutes each real second
    [SerializeField] private float timeScale = 1f;   // multiplier (1 = default)
    [SerializeField, Tooltip("Display minute step. 1 = every minute, 5/10/etc. = snap.")]
    private int minuteDisplayStep = 1;

    [Header("Behavior")]
    [SerializeField] private bool autoStart = true;

    private float minutesSinceMidnight; // tracked as minutes [0..480]
    private float endMinutes;
    private bool running;

    private float sliderValue;
    [SerializeField] Drinkable drinkable;
    
    
    private void OnEnable()
    {
        endMinutes = endHour * 60f;
        minutesSinceMidnight = Mathf.Clamp(startHour * 60f, 0f, endMinutes);
        UpdateLabel();
        running = autoStart;
    }
    

    private void Update()
    {
        sliderValue = drinkable.GetComponent<Drinkable>().Drunk;
        
        if (!running) return;

        float deltaMinutes = Time.deltaTime * minutesPerSecond * Mathf.Max(0f, timeScale);
        minutesSinceMidnight = Mathf.Min(minutesSinceMidnight + deltaMinutes, endMinutes);

        UpdateLabel();
        SliderUpdate(sliderValue/100f);
        
        if (minutesSinceMidnight >= endMinutes)
        {
            running = false;
        }
        
    }

    private void UpdateLabel()
    {
        if (timeText == null) return;

        int totalMinutes = Mathf.Clamp(Mathf.RoundToInt(minutesSinceMidnight), 0, (int)endMinutes);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        int step = Mathf.Clamp(minuteDisplayStep, 1, 60);
        int snappedMinutes = (minutes / step) * step;

        if (hours >= endHour)
        {
            hours = endHour;
            snappedMinutes = 0;
        }

        timeText.text = $"{hours:00}:{snappedMinutes:00} AM";
    }
    
    private void SliderUpdate(float value)
    {
        if (drunkSlider != null)
        {
            drunkSlider.value = value;
        }
    }

    // Controls
    public void StartClock() => running = true;
    public void StopClock() => running = false;

    public void ResetClock()
    {
        minutesSinceMidnight = Mathf.Clamp(startHour * 60f, 0f, endMinutes);
        running = false;
        UpdateLabel();
    }

    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Max(0f, scale);
    }

    public void SetMinuteDisplayStep(int step)
    {
        minuteDisplayStep = Mathf.Clamp(step, 1, 60);
        UpdateLabel();
    }
}
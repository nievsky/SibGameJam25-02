using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    [Header("End-of-shift scenes")]
    [Tooltip("Scene when drunk < Medium Threshold.")]
    [SerializeField] private string lowDrunkScene = "";
    [Tooltip("Scene when Medium Threshold <= drunk < High Threshold.")]
    [SerializeField] private string mediumDrunkScene = "";
    [Tooltip("Scene when drunk >= High Threshold.")]
    [SerializeField] private string highDrunkScene = "";

    [Header("Drunk thresholds (0..100)")]
    [Range(0f, 100f)] [SerializeField] private float mediumThreshold = 33f;
    [Range(0f, 100f)] [SerializeField] private float highThreshold = 66f;

    private float minutesSinceMidnight; // tracked as minutes [0..480]
    private float endMinutes;
    private bool running;

    private float sliderValue;
    [SerializeField] private Drinkable drinkable;

    private bool sceneQueued;

    private void OnEnable()
    {
        endMinutes = endHour * 60f;
        minutesSinceMidnight = Mathf.Clamp(startHour * 60f, 0f, endMinutes);
        UpdateLabel();
        running = autoStart;
        sceneQueued = false;
    }

    private void Update()
    {
        if (drinkable != null)
            sliderValue = drinkable.Drunk;
        else
            sliderValue = 0f;

        if (!running) return;

        float deltaMinutes = Time.deltaTime * minutesPerSecond * Mathf.Max(0f, timeScale);
        minutesSinceMidnight = Mathf.Min(minutesSinceMidnight + deltaMinutes, endMinutes);

        UpdateLabel();
        SliderUpdate(sliderValue / 100f);

        if (minutesSinceMidnight >= endMinutes && !sceneQueued)
        {
            running = false;
            sceneQueued = true;
            LoadEndSceneByDrunk();
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

    private void LoadEndSceneByDrunk()
    {
        float drunk = Mathf.Clamp(sliderValue, 0f, 100f);
        string sceneToLoad;

        if (drunk < mediumThreshold)
            sceneToLoad = lowDrunkScene;
        else if (drunk < highThreshold)
            sceneToLoad = mediumDrunkScene;
        else
            sceneToLoad = highDrunkScene;

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
        else
            Debug.LogWarning("UIController: End scene name is empty. Assign scene names in the Inspector.");
    }

    // Controls
    public void StartClock() => running = true;
    public void StopClock() => running = false;

    public void ResetClock()
    {
        minutesSinceMidnight = Mathf.Clamp(startHour * 60f, 0f, endMinutes);
        running = false;
        sceneQueued = false;
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
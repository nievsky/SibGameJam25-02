using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;

public class Drinkable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;                          // Defaults to Camera.main
    [SerializeField] private InputActionReference interactAction; // Button action (Press and Hold)
    [SerializeField] private UnityEngine.UI.Slider slider;        // UI Slider to show drunk level

    [Header("Config")]
    [SerializeField] private float interactDistance = 5.0f;
    [SerializeField] private float drunkRatePerSecond = 10f;      // How fast Drunk increases
    [SerializeField] private string drinkTag = "Dragable";

    [Header("Runtime")]
    public float Drunk = 0f; // 0..100
    public float DrunkDecreaser = 1f;
    public bool Drinking { get; private set; }

    private InputAction _interact;
    private bool _isHolding;

    // FMOD
    private EventInstance drinkSoundInstance;
    private bool isDrinkSoundPlaying = false;
    private const string DRINK_EVENT_PATH = "event:/Drink";

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        if (interactAction != null)
        {
            _interact = interactAction.action;
            _interact.started += OnInteractStarted;
            _interact.canceled += OnInteractCanceled;
            _interact.Enable();
        }
    }

    private void OnDisable()
    {
        if (_interact != null)
        {
            _interact.started -= OnInteractStarted;
            _interact.canceled -= OnInteractCanceled;
            _interact.Disable();
        }

        StopDrinkingSound();
        Drinking = false;
        _isHolding = false;
    }

    private void OnInteractStarted(InputAction.CallbackContext _)
    {
        _isHolding = true;
    }

    private void OnInteractCanceled(InputAction.CallbackContext _)
    {
        _isHolding = false;
        Drinking = false;
        StopDrinkingSound();
    }

    private void Update()
    {
        Drunk = Mathf.Max(0f, Drunk - DrunkDecreaser * Time.deltaTime);

        if (!_isHolding || Drunk >= 100f || cam == null)
        {
            Drinking = false;
            return;
        }

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactDistance))
        {
            if (hit.collider != null && hit.collider.CompareTag(drinkTag))
            {
                var drinkable = hit.collider.GetComponentInParent<DrinkComponent>() ?? hit.collider.GetComponent<DrinkComponent>();
                if (drinkable != null && drinkable.DrinkSeconds > 0f)
                {
                    float dt = Time.deltaTime;

                    // Increase player drunk (clamped 0..100)
                    Drunk = Mathf.Min(100f, Drunk + drunkRatePerSecond * dt);

                    // Decrease the source drink time (down to 0)
                    drinkable.DrinkSeconds = Mathf.Max(0f, drinkable.DrinkSeconds - dt);

                    Drinking = true;
                    StartDrinkingSound(hit.collider.gameObject);

                    // Stop if either depleted or capped
                    if (drinkable.DrinkSeconds <= 0f || Drunk >= 100f)
                    {
                        _isHolding = false;
                        StopDrinkingSound();
                    }

                    return;
                }
            }
        }

        Drinking = false;
    }

    // --- FMOD Logic ---

    private void StartDrinkingSound(GameObject source)
    {
        if (isDrinkSoundPlaying) return;

        drinkSoundInstance = RuntimeManager.CreateInstance(DRINK_EVENT_PATH);
        drinkSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
        drinkSoundInstance.start();
        isDrinkSoundPlaying = true;
    }

    private void StopDrinkingSound()
    {
        if (!isDrinkSoundPlaying) return;

        drinkSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        drinkSoundInstance.release();
        isDrinkSoundPlaying = false;
    }
}

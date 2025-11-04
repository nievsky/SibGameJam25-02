using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using FMODUnity;

public class SceneManagerUI : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private GameObject pauseMenuImage;
    [SerializeField] private GameObject settingMenuImage;

    private UIPopWindow uiPopWindow;
    private UIPopWindow uiPopWindow2;
    public bool isPaused = false;

    // --- FMOD snapshot ---
    private EventInstance pausedSnapshot;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (pauseMenuImage != null)
        {
            pauseMenuImage.SetActive(true);
            uiPopWindow = pauseMenuImage.GetComponent<UIPopWindow>();
        }

        if (settingMenuImage != null)
        {
            uiPopWindow2 = settingMenuImage.GetComponent<UIPopWindow>();
        }

        // Initialize FMOD snapshot
        pausedSnapshot = RuntimeManager.CreateInstance("snapshot:/Paused");
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            uiPopWindow?.Show();

            // Start FMOD snapshot
            pausedSnapshot.start();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            uiPopWindow?.Hide();
            uiPopWindow2?.Hide();

            // Stop FMOD snapshot (allow fade-out)
            pausedSnapshot.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPausePerformed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPausePerformed;
            pauseAction.action.Disable();
        }

        // Make sure snapshot is stopped when object is disabled
        pausedSnapshot.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
        pausedSnapshot.release();
    }
}

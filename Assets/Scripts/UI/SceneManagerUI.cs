using UnityEngine;
using UnityEngine.InputSystem;

public class SceneManagerUI : MonoBehaviour
{
    [SerializeField] private InputActionReference pauseAction;
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private GameObject pauseMenuImage;
    [SerializeField] private GameObject settingMenuImage;

    private UIPopWindow uiPopWindow;
    private UIPopWindow uiPopWindow2;
    public bool isPaused = false;

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
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f; // Pause the game
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            uiPopWindow?.Show();
        }
        else
        {
            Time.timeScale = 1f; // Resume the game
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            uiPopWindow?.Hide();
            uiPopWindow2?.Hide();
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
    }
}
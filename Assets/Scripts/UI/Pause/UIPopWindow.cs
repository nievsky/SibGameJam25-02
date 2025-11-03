using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

/* Short Documentation
 * Key Features:
 * Script to handle pop-up window animations (show/hide) with scaling effects
 * Working standalone or as a base class for specific pop-up windows (e.g., pause menu, settings)
 * Can be upgradet to include more complex animations or effects
 */


public class UIPopWindow : MonoBehaviour
{
    [Header("Animation")]
    public float animationDuration = 0.3f;
    public Ease ease = Ease.OutBack; // nice bounce effect

    private RectTransform rectTransform;
    private Vector3 originalScale;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        // start hidden (scale zero & inactive)
        rectTransform.localScale = Vector3.zero;
        // gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        rectTransform.DOKill(); // stop ongoing tweens

        // Animate scale from 0 to full size
        rectTransform.localScale = Vector3.zero;
        rectTransform.DOScale(originalScale, animationDuration)
            .SetEase(ease);
    }

    public void Hide()
    {
        rectTransform.DOKill();

        // Animate back to 0 then disable
        rectTransform.DOScale(Vector3.zero, animationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }

    public void HideWithCursorLock()
    {
        Hide();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void ContinueGame()
    {
        Time.timeScale = 1f; // Resume the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Hide();
    }
    
    public void RestartLevel()
    {
        Time.timeScale = 1f; // Resume the game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
    
    
    
}

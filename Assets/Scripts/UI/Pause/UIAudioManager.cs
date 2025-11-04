using UnityEngine;

/* Short Documentation
 * Key Features:
 * Singleton pattern for easy access to UI audio functions
 * Used only for UI sound effects (clicks, hovers)
 * Can be upgraded to include more UI sounds as needed
 */
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance;
    public AudioSource audioSource;

    public AudioClip clickSFX;
    public AudioClip hoverSFX;

    void Awake() => Instance = this;

    public void PlayHoverSFX()
    {
        if (hoverSFX == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(hoverSFX);
    }

    public void PlayClickSFX()
    {
        if (clickSFX == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(clickSFX);
    }

    public void OnVolumeChanged(float volume)
    {
        audioSource.volume = volume;
    }
}

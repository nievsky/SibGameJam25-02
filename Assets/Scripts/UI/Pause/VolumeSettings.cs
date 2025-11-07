using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

/* Short Documentation
 * Key Features:
 * Simple volume settings manager for adjusting master volume via a UI slider.
 * Can be upgraded, but better to use general Audio Manager, that do all things at once
 */

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioSource audioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    
    private void Start()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
    }
    
    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.volume = volume;
        }
    }
    
}

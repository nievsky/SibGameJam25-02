using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(Rigidbody))]
public class ImpactSound : MonoBehaviour
{
    [Header("FMOD")]
    [Tooltip("FMOD Event Reference for the hit sound (assign from FMOD Event Browser).")]
    [SerializeField] private EventReference impactEvent;

    [Header("Settings")]
    [Tooltip("Minimum collision velocity required to trigger the sound.")]
    [SerializeField] private float minVelocity = 0.2f;

    [Tooltip("Scale velocity (0–1) into the FMOD parameter 'Velocity' (if it exists).")]
    [SerializeField] private bool useVelocityParameter = true;

    [Tooltip("Name of the FMOD parameter that will receive the velocity value.")]
    [SerializeField] private string velocityParameterName = "Velocity";

    [Tooltip("Delay between impact sounds to prevent rapid re-triggers.")]
    [SerializeField] private float cooldown = 0.05f;

    private float _lastPlayTime;

    private void OnCollisionEnter(Collision collision)
    {
        // Skip if no event assigned or cooldown active
        if (impactEvent.IsNull || Time.time - _lastPlayTime < cooldown)
            return;

        // Measure impact velocity
        float impactVelocity = collision.relativeVelocity.magnitude;
        if (impactVelocity < minVelocity)
            return;

        // Play FMOD event at hit point
        PlayImpactSound(collision.contacts[0].point, impactVelocity);
        _lastPlayTime = Time.time;
    }

    private void PlayImpactSound(Vector3 position, float velocity)
    {
        EventInstance instance = RuntimeManager.CreateInstance(impactEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

        if (useVelocityParameter && !string.IsNullOrEmpty(velocityParameterName))
        {
            // Normalize velocity into a reasonable 0..1 range
            float normalized = Mathf.Clamp01(velocity / 10f);
            instance.setParameterByName(velocityParameterName, normalized);
        }

        instance.start();
        instance.release();
    }
}

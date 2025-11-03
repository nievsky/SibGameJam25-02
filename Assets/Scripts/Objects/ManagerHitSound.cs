using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(Collider))]
public class ManagerHitSound : MonoBehaviour
{
    [Header("FMOD")]
    [Tooltip("FMOD Event Reference for the hit sound (assign from FMOD Event Browser).")]
    [SerializeField] private EventReference hitEvent;

    [Header("Settings")]
    [Tooltip("Minimum incoming velocity required to trigger sound.")]
    [SerializeField] private float minVelocity = 0.2f;

    [Tooltip("If true, send collision speed to FMOD parameter (named below).")]
    [SerializeField] private bool useVelocityParameter = true;

    [Tooltip("Name of the FMOD parameter to receive normalized velocity value.")]
    [SerializeField] private string velocityParameterName = "Velocity";

    [Tooltip("Delay between impact sounds to prevent spam.")]
    [SerializeField] private float cooldown = 0.05f;

    private float _lastPlayTime;

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore if no FMOD event assigned
        if (hitEvent.IsNull)
            return;

        // Enforce cooldown
        if (Time.time - _lastPlayTime < cooldown)
            return;

        // Check if hitting object has a Rigidbody and some velocity
        Rigidbody otherRb = collision.rigidbody;
        if (otherRb == null)
            return;

        float impactVelocity = otherRb.linearVelocity.magnitude;
        if (impactVelocity < minVelocity)
            return;

        Vector3 hitPoint = collision.contacts.Length > 0 ?
            collision.contacts[0].point : transform.position;

        PlayHitSound(hitPoint, impactVelocity);
        _lastPlayTime = Time.time;
    }

    private void PlayHitSound(Vector3 position, float velocity)
    {
        EventInstance instance = RuntimeManager.CreateInstance(hitEvent);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));

        if (useVelocityParameter && !string.IsNullOrEmpty(velocityParameterName))
        {
            float normalized = Mathf.Clamp01(velocity / 10f); // normalize to 0–1 range
            instance.setParameterByName(velocityParameterName, normalized);
        }

        instance.start();
        instance.release();
    }
}

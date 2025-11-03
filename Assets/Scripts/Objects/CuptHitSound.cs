using UnityEngine;
using FMODUnity;
using FMOD.Studio;

[RequireComponent(typeof(Rigidbody))]
public class CupHitSound : MonoBehaviour
{
    private const string CUP_HIT_EVENT_PATH = "event:/CupHit";  // hardcoded FMOD event
    private const string VELOCITY_PARAM = "Velocity";           // name of your FMOD parameter

    [Header("Hit Settings")]
    [SerializeField] private float minVelocity = 0.3f;   // Minimum impact to trigger sound
    [SerializeField] private float maxVelocity = 10f;    // Max velocity for parameter scaling
    [SerializeField] private float cooldown = 0.1f;      // Prevents rapid-fire sound spam

    private float lastHitTime = 0f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Ignore very gentle touches or spammy collisions
        if (Time.time - lastHitTime < cooldown)
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minVelocity)
            return; // too soft to make sound

        lastHitTime = Time.time;

        // Clamp velocity to FMOD parameter range
        float normalizedVelocity = Mathf.InverseLerp(minVelocity, maxVelocity, impactSpeed);
        normalizedVelocity = Mathf.Clamp01(normalizedVelocity);

        // Create instance
        EventInstance hitInstance = RuntimeManager.CreateInstance(CUP_HIT_EVENT_PATH);

        // Set position (3D sound)
        hitInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        // Send velocity parameter
        hitInstance.setParameterByName(VELOCITY_PARAM, normalizedVelocity);

        // Play and release (one-shot)
        hitInstance.start();
        hitInstance.release();
    }
}

using UnityEngine;

[DisallowMultipleComponent]
public class VankaVstanka : MonoBehaviour
{
    [Header("Center of Mass")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Upright (rotation)")]
    public float uprightStrength = 40f;     // proportional (Kp) for rotation
    public float uprightDamping = 5f;       // derivative (Kd) for rotation
    public float maxTorque = 200f;          // safety clamp
    public bool keepYawOnly = false;        // true: correct tilt only, let yaw be free

    [Header("Return (position)")]
    public float positionStrength = 10f;    // proportional (Kp) for position
    public float positionDamping = 2f;      // derivative (Kd) for position
    public float maxForce = 500f;           // safety clamp

    [Header("Tuning")]
    public bool useAccelerationMode = true; // mass-independent tuning

    private Rigidbody rb;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        rb.centerOfMass = centerOfMassOffset;
    }

    void FixedUpdate()
    {
        // 1) Angular PD: return to upright/target rotation
        Quaternion desired = keepYawOnly ? UprightWithCurrentYaw() : targetRotation;

        Quaternion delta = desired * Quaternion.Inverse(rb.rotation);
        delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
        // Map to [-180, 180] and guard degenerate cases
        angleDeg = Mathf.DeltaAngle(0f, angleDeg);
        if (axis.sqrMagnitude < 1e-8f || Mathf.Abs(angleDeg) < 0.001f)
        {
            axis = Vector3.zero;
            angleDeg = 0f;
        }

        float angleRad = angleDeg * Mathf.Deg2Rad; // PD in radians
        Vector3 pdTorque = axis * (uprightStrength * angleRad) - rb.angularVelocity * uprightDamping;
        pdTorque = Vector3.ClampMagnitude(pdTorque, maxTorque);
        rb.AddTorque(pdTorque, useAccelerationMode ? ForceMode.Acceleration : ForceMode.Force);

        // 2) Positional PD: return to start position
        Vector3 posError = targetPosition - rb.worldCenterOfMass;
        Vector3 pdForce = posError * positionStrength - rb.linearVelocity * positionDamping;
        pdForce = Vector3.ClampMagnitude(pdForce, maxForce);
        rb.AddForce(pdForce, useAccelerationMode ? ForceMode.Acceleration : ForceMode.Force);
    }

    // Builds an upright rotation that preserves the current yaw (heading) but removes tilt.
    private Quaternion UprightWithCurrentYaw()
    {
        Vector3 fwd = transform.forward;
        Vector3 flatFwd = Vector3.ProjectOnPlane(fwd, Vector3.up);
        if (flatFwd.sqrMagnitude < 1e-6f) flatFwd = Vector3.forward; // fallback
        return Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
    }

    // Call to re-anchor the target to the current pose (e.g., after teleport/grab release).
    public void ReAnchorToCurrent()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }
}
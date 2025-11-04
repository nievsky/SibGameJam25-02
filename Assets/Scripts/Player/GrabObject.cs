using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;
using FMOD.Studio;

public class GrabObject : MonoBehaviour
{
    [Header("Input (new Input System)")]
    public InputActionReference grabAction;
    public InputActionReference pushAction;

    [Header("Grabbing")]
    [SerializeField] private LayerMask grabLayer = ~0;
    [SerializeField] private float maxGrabDistance = 12f;
    [SerializeField] private float holdDistance = 4f;
    [SerializeField, Tooltip("0..1: lower = looser, higher = tighter follow")]
    private float followStrength = 0.3f;

    [Header("Push")]
    [SerializeField] private float pushForce = 10f;

    [Header("Camera")]
    [SerializeField] private Camera cameraOverride;

    private Camera _cam;
    private Rigidbody _grabbedRb;
    private Transform _grabbedTf;
    private Vector3 _hitOffsetLocal;
    private RigidbodyInterpolation _initialInterpolation;

    // --- FMOD Grab Sound ---
    private const string GRAB_EVENT = "event:/Grab";

    private void Awake()
    {
        EnsureCamera();
    }

    private void OnEnable()
    {
        EnsureCamera();

        if (grabAction != null)
        {
            grabAction.action.started += OnGrabStarted;
            grabAction.action.canceled += OnGrabCanceled;
            grabAction.action.Enable();
        }

        if (pushAction != null)
        {
            pushAction.action.performed += OnPushPerformed;
            pushAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (grabAction != null)
        {
            grabAction.action.started -= OnGrabStarted;
            grabAction.action.canceled -= OnGrabCanceled;
            grabAction.action.Disable();
        }

        if (pushAction != null)
        {
            pushAction.action.performed -= OnPushPerformed;
            pushAction.action.Disable();
        }

        Release();
    }

    private void FixedUpdate()
    {
        if (_grabbedRb == null || _cam == null) return;

        Vector3 holdPoint = _cam.transform.position + _cam.transform.forward * holdDistance;
        Vector3 centerDestination = holdPoint - _grabbedTf.TransformVector(_hitOffsetLocal);
        Vector3 toDest = centerDestination - _grabbedTf.position;
        Vector3 velChange = toDest / Time.fixedDeltaTime * Mathf.Clamp01(followStrength);

        _grabbedRb.linearVelocity = Vector3.zero;
        _grabbedRb.AddForce(velChange, ForceMode.VelocityChange);
    }

    private void OnGrabStarted(InputAction.CallbackContext _)
    {
        EnsureCamera();
        if (_cam == null || _grabbedRb != null) return;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxGrabDistance, grabLayer, QueryTriggerInteraction.Ignore))
        {
            var rb = hit.rigidbody ?? hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic) return;

            _grabbedRb = rb;
            _grabbedTf = rb.transform;

            _initialInterpolation = _grabbedRb.interpolation;
            _grabbedRb.interpolation = RigidbodyInterpolation.Interpolate;
            _grabbedRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            _hitOffsetLocal = hit.transform.InverseTransformVector(hit.point - hit.transform.position);
            holdDistance = Mathf.Clamp(hit.distance, 1f, maxGrabDistance);

            // --- Play FMOD Grab Sound (3D) ---
            RuntimeManager.PlayOneShot(GRAB_EVENT, hit.point);
        }
    }

    private void OnGrabCanceled(InputAction.CallbackContext _)
    {
        Release();
    }

    private void OnPushPerformed(InputAction.CallbackContext _)
    {
        EnsureCamera();
        if (_cam == null) return;

        if (_grabbedRb != null)
        {
            _grabbedRb.AddForce(_cam.transform.forward * pushForce, ForceMode.VelocityChange);
            Release();
            return;
        }

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out var hit, maxGrabDistance, grabLayer, QueryTriggerInteraction.Ignore))
        {
            var rb = hit.rigidbody ?? hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic)
            {
                rb.AddForce(_cam.transform.forward * pushForce, ForceMode.VelocityChange);
            }
        }
    }

    private void Release()
    {
        if (_grabbedRb == null) return;

        _grabbedRb.interpolation = _initialInterpolation;
        _grabbedRb = null;
        _grabbedTf = null;
    }

    private void EnsureCamera()
    {
        if (_cam != null) return;
        _cam = cameraOverride != null ? cameraOverride : Camera.main;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_cam == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(_cam.transform.position, _cam.transform.position + _cam.transform.forward * maxGrabDistance);
    }
#endif
}

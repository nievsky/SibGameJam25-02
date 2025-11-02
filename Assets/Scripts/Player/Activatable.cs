using UnityEngine;
using UnityEngine.InputSystem;

public class Activatable : MonoBehaviour
{
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private string activationTag = "ActivationTag";
    [SerializeField] private InputActionReference activateAction; // Assign in Inspector

    private void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = Camera.main;
    }

    private void OnEnable()
    {
        if (activateAction != null)
        {
            activateAction.action.performed += OnActivatePerformed;
            if (!activateAction.action.enabled)
                activateAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (activateAction != null)
        {
            activateAction.action.performed -= OnActivatePerformed;
            if (activateAction.action.enabled)
                activateAction.action.Disable();
        }
    }

    private void OnActivatePerformed(InputAction.CallbackContext _)
    {
        if (sourceCamera == null)
            return;

        var ray = new Ray(sourceCamera.transform.position, sourceCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            var hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag(activationTag))
            {
                if (!hitObject.TryGetComponent<ActivationPipe>(out var activatable))
                    activatable = hitObject.GetComponentInParent<ActivationPipe>();

                if (activatable != null)
                    activatable.Toggle();
            }
        }
    }
}
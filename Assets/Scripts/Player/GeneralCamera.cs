using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/* Short Documentation
 * Key Features:
 * Handles camera rotation based on player input
 * In future be updated to controll different camera settings (FOV, shake, etc.)
 */

public class GeneralCamera : MonoBehaviour
{
    public InputActionReference lookAction;

    [Header("Camera Settings")]
    [SerializeField] private float Sensitivity = 10f;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Slider mouseSensitivity;
    private float xRotation = 0f;

    private void OnEnable()
    {
        lookAction?.action?.Enable();
    }

    private void OnDisable()
    {
        lookAction?.action?.Disable();
    }

    private void Update()
    {
        HandleLook();
    }

    private void HandleLook()
    {
        float Sensitivity = mouseSensitivity != null ? mouseSensitivity.value : this.Sensitivity;
        
        if (lookAction?.action == null || playerBody == null) return;
        
        Vector2 look = lookAction.action.ReadValue<Vector2>();

        float yaw = look.x * Sensitivity * Time.deltaTime;
        float pitch = look.y * Sensitivity * Time.deltaTime;

        xRotation -= pitch;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        playerBody.Rotate(0f, yaw, 0f, Space.Self);
    }
}
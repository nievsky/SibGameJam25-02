using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnObjects : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnPoint;

    [Header("Input (new Input System)")]
    [Tooltip("Reference to a Button action (e.g., Space, A button, etc.).")]
    [SerializeField] private InputActionReference spawnAction;

    private void OnEnable()
    {
        if (spawnAction != null)
        {
            spawnAction.action.performed += OnSpawnPerformed;
            spawnAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (spawnAction != null)
        {
            spawnAction.action.performed -= OnSpawnPerformed;
            spawnAction.action.Disable();
        }
    }

    private void OnSpawnPerformed(InputAction.CallbackContext ctx)
    {
        if (prefabToSpawn == null || spawnPoint == null) return;
        Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
    }
}
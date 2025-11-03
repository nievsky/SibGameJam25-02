using System.Collections;
using UnityEngine;

public class SpawnObjectsAt : MonoBehaviour
{
    [Header("Assignments")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnPoint;

    [Header("Spawn Settings")]
    [Tooltip("Seconds between spawn attempts")]
    public float spawnInterval = 1f;
    [Tooltip("Radius around the spawn point to check for blocking colliders")]
    public float spawnRadius = 0.5f;
    [Tooltip("Layers considered blocking for spawning")]
    public LayerMask blockingLayers = ~0;
    [Tooltip("If true the coroutine starts automatically on Enable")]
    public bool autoStart = true;

    private Coroutine spawnCoroutine;

    private void OnEnable()
    {
        if (autoStart)
            StartSpawning();
    }

    private void OnDisable()
    {
        StopSpawning();
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
            spawnCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        // Wait 0 so first attempt happens immediately; change to WaitForSeconds(spawnInterval) if desired otherwise
        while (true)
        {
            if (prefabToSpawn != null && spawnPoint != null)
            {
                // Check for any colliders inside the radius on the blocking layers (ignores triggers)
                Collider[] hits = Physics.OverlapSphere(spawnPoint.position, spawnRadius, blockingLayers, QueryTriggerInteraction.Ignore);
                if (hits.Length == 0)
                {
                    Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
                }
            }

            yield return new WaitForSeconds(Mathf.Max(0f, spawnInterval));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        Gizmos.DrawSphere(spawnPoint.position, spawnRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);
    }
}
// csharp
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ExaggeratedCollisionPush : MonoBehaviour
{
    [Header("Filter")]
    [SerializeField] private string draggableTag = "Dragable";

    [Header("Impulse")]
    [SerializeField] private float minImpulse = 5f;
    [SerializeField] private float maxImpulse = 10f;
    [Tooltip("Extra upward influence added to the push direction.")]
    [SerializeField] private float upwardBias = 0.5f;
    [Tooltip("How much random left/right to add.")]
    [SerializeField] private float lateralJitter = 1.0f;

    [Header("Torque")]
    [SerializeField] private float minTorque = 2f;
    [SerializeField] private float maxTorque = 6f;

    [Header("Spam Control")]
    [SerializeField] private float pairCooldown = 0.1f;

    private readonly Dictionary<int, float> lastHitTimeByOther = new Dictionary<int, float>();
    private Transform selfRoot;
    private Rigidbody selfRb;

    private void Awake()
    {
        selfRoot = FindTaggedAncestor(transform, draggableTag) ?? transform;
        selfRb = selfRoot.GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only react if both are tagged Dragable
        var otherRoot = FindTaggedAncestor(collision.transform, draggableTag);
        if (otherRoot == null) return;
        if (!selfRoot.CompareTag(draggableTag)) return;

        // Avoid double-processing from both sides
        if (selfRoot.GetInstanceID() > otherRoot.GetInstanceID()) return;

        // Cooldown per other
        int key = otherRoot.GetInstanceID();
        float now = Time.time;
        if (lastHitTimeByOther.TryGetValue(key, out float lastTime) && now - lastTime < pairCooldown)
            return;
        lastHitTimeByOther[key] = now;

        var otherRb = otherRoot.GetComponent<Rigidbody>();
        if (selfRb == null || otherRb == null) return;
        if (selfRb.isKinematic && otherRb.isKinematic) return;

        // Compute away directions with upward and lateral randomness
        Vector3 sepA = SafeHorizontalAway(selfRoot.position - otherRoot.position);
        Vector3 sepB = -sepA;

        Vector3 dirA = RandomUpOrSide(sepA, upwardBias, lateralJitter);
        Vector3 dirB = RandomUpOrSide(sepB, upwardBias, lateralJitter);

        float impulseA = Random.Range(minImpulse, maxImpulse);
        float impulseB = Random.Range(minImpulse, maxImpulse);

        // Apply impulses
        if (selfRb != null && !selfRb.isKinematic)
        {
            selfRb.AddForce(dirA * impulseA, ForceMode.Impulse);
            selfRb.AddTorque(Random.onUnitSphere * Random.Range(minTorque, maxTorque), ForceMode.Impulse);
        }

        if (otherRb != null && !otherRb.isKinematic)
        {
            otherRb.AddForce(dirB * impulseB, ForceMode.Impulse);
            otherRb.AddTorque(Random.onUnitSphere * Random.Range(minTorque, maxTorque), ForceMode.Impulse);
        }
    }

    private static Transform FindTaggedAncestor(Transform start, string tag)
    {
        for (Transform t = start; t != null; t = t.parent)
        {
            if (t.CompareTag(tag)) return t;
        }
        return null;
    }

    private static Vector3 SafeHorizontalAway(Vector3 v)
    {
        // If nearly zero, pick a random horizontal direction
        if (v.sqrMagnitude < 1e-6f)
        {
            Vector3 h = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            return h.sqrMagnitude > 1e-4f ? h.normalized : Vector3.right;
        }
        return v.normalized;
    }

    private static Vector3 RandomUpOrSide(Vector3 baseDir, float upBias, float lateral)
    {
        // Base away, ensure non-downward tendency
        Vector3 hRand = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        Vector3 dir = baseDir + (hRand.normalized * lateral) + (Vector3.up * Mathf.Max(0f, upBias));
        dir.y = Mathf.Abs(dir.y); // bias upward
        if (dir.sqrMagnitude < 1e-6f) dir = Vector3.up;
        return dir.normalized;
    }
}
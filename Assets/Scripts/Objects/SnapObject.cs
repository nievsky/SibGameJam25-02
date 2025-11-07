using UnityEngine;

public class SnapObject : MonoBehaviour
{
    [Header("Snap Settings")]
    [SerializeField] private Transform snapPoint;        // Default pose; uses this transform if null
    [SerializeField] private bool parentSnapped = true;  // Parent snapped object under the snap point
    [SerializeField] private bool occupyOnce = false;    // Prevent replacing the first snapped object
    [SerializeField] private string draggableTag = "Dragable";

    private Transform currentOccupant;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (occupyOnce && currentOccupant != null) return;

        Transform target = FindTaggedAncestor(other.transform, draggableTag);
        if (target == null) return;

        Snap(target);
        currentOccupant = target;
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentOccupant == null) return;

        Transform target = FindTaggedAncestor(other.transform, draggableTag);
        if (target == null || target != currentOccupant) return;

        Release(target);
        currentOccupant = null;
    }

    public void ReleaseCurrent()
    {
        if (currentOccupant == null) return;
        Release(currentOccupant);
        currentOccupant = null;
    }

    private static Transform FindTaggedAncestor(Transform start, string tag)
    {
        for (Transform t = start; t != null; t = t.parent)
        {
            if (t.CompareTag(tag)) return t;
        }
        return null;
    }

    private void Snap(Transform targetTransform)
    {
        Transform pose = snapPoint != null ? snapPoint : transform;

        var rb = targetTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;          // fixed: use velocity, not linearVelocity
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        targetTransform.SetPositionAndRotation(pose.position, pose.rotation);

        if (parentSnapped)
        {
            targetTransform.SetParent(pose, true);
        }
    }

    private void Release(Transform targetTransform)
    {
        if (parentSnapped && targetTransform.parent == (snapPoint != null ? snapPoint : transform))
        {
            targetTransform.SetParent(null, true);
        }

        var rb = targetTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
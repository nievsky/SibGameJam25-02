using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuckCup : MonoBehaviour
{
    [SerializeField] private string fillTag = "Dragable";
    [SerializeField] private float fillRatePerSecond = 1f;
    [SerializeField] private bool searchInParentsIfMissing = true;

    // Push-on-empty settings
    [Header("Push on empty")]
    [SerializeField] private Transform player; // assign in Inspector
    [SerializeField] private float pushImpulse = 5f; // fallback linear push
    [SerializeField] private float pushDelayOnAlreadyEmpty = 1f;

    // Parabolic push settings
    [Header("Parabolic push")]
    [SerializeField] private float arcHeight = 2f;      // apex above max(startY, targetY)
    [SerializeField] private float pushCooldown = 1.0f; // seconds between pushes per rigidbody

    // Spin settings
    [Header("Spin on push")]
    [SerializeField] private bool addSpin = true;
    [SerializeField] [Range(0f, 50f)] private float spinStrength = 10f; // rad/s delta (VelocityChange)
    [SerializeField] [Range(0f, 1f)] private float spinRandomness = 0.3f; // 0=no random, 1=full random axis
    [SerializeField] private float maxAngularVelocity = 50f; // cap to allow fast spins

    // Public flag requested
    public bool isEmpty = true;

    // per-Rigidbody cooldown and flight lock
    private readonly Dictionary<Rigidbody, float> _lastPushTime = new Dictionary<Rigidbody, float>();
    private readonly Dictionary<Rigidbody, float> _lockUntil = new Dictionary<Rigidbody, float>();

    // Manage delayed push scheduling per drink
    private readonly HashSet<DrinkComponent> _delayScheduled = new HashSet<DrinkComponent>();
    // Track currently touching colliders with the fillTag
    private readonly HashSet<Collider> _touchingTagged = new HashSet<Collider>();

    private void OnEnable()
    {
        // start assuming empty until collisions/trigger report otherwise
        isEmpty = true;
    }

    private void OnDisable()
    {
        _touchingTagged.Clear();
        isEmpty = true;
        _delayScheduled.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != null && other.CompareTag(fillTag))
        {
            _touchingTagged.Add(other);
            isEmpty = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null && other.CompareTag(fillTag))
        {
            _touchingTagged.Remove(other);
            isEmpty = _touchingTagged.Count == 0;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
            return;

        var col = collision.collider;
        if (col != null && (col.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag)))
        {
            _touchingTagged.Add(col);
            isEmpty = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision == null)
            return;

        var col = collision.collider;
        if (col != null && (col.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag)))
        {
            _touchingTagged.Remove(col);
            isEmpty = _touchingTagged.Count == 0;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null || !other.CompareTag(fillTag))
            return;

        // ensure the set/state remains correct in edge cases
        _touchingTagged.Add(other);
        isEmpty = false;

        var rb = other.attachedRigidbody;
        if (rb == null && searchInParentsIfMissing)
            rb = other.GetComponentInParent<Rigidbody>();

        if (!TryGetDrinkComponent(other, out var drink))
        {
            TryPushNoDrink(rb);
            return;
        }

        SuckOverTime(drink, rb, Time.deltaTime);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null)
            return;

        var col = collision.collider;
        if (!(col != null && (col.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag))))
            return;

        // ensure the set/state remains correct in edge cases
        _touchingTagged.Add(col);
        isEmpty = false;

        var rb = collision.rigidbody ?? col.attachedRigidbody;
        if (rb == null && searchInParentsIfMissing)
            rb = col.GetComponentInParent<Rigidbody>();

        if (!TryGetDrinkComponent(col, out var drink))
        {
            TryPushNoDrink(rb);
            return;
        }

        SuckOverTime(drink, rb, Time.deltaTime);
    }

    private bool TryGetDrinkComponent(Collider other, out DrinkComponent drink)
    {
        if (other.TryGetComponent(out drink))
            return true;

        if (searchInParentsIfMissing)
        {
            drink = other.GetComponentInParent<DrinkComponent>();
            if (drink != null) return true;

            drink = other.GetComponentInChildren<DrinkComponent>();
            if (drink != null) return true;
        }

        drink = null;
        return false;
    }

    private void SuckOverTime(DrinkComponent drink, Rigidbody targetRb, float deltaTime)
    {
        if (drink == null || targetRb == null || deltaTime <= 0f)
            return;

        float current = drink.DrinkSeconds;

        // Already empty on contact: schedule a delayed push or push immediately
        if (current <= 0f)
        {
            if (pushDelayOnAlreadyEmpty > 0f)
            {
                // schedule one delayed push per drink at a time; actual push will respect per-rigidbody cooldown
                if (_delayScheduled.Add(drink))
                    StartCoroutine(DelayPushOnAlreadyEmpty(drink, targetRb));
            }
            else
            {
                TryPush(targetRb);
            }
            return;
        }

        // Drain over time
        float newValue = Mathf.Max(0f, current - fillRatePerSecond * deltaTime);
        drink.DrinkSeconds = newValue;

        // Became empty due to sucking: push immediately
        if (newValue <= 0f)
        {
            // clear any scheduled marker so future scheduling is allowed
            _delayScheduled.Remove(drink);
            TryPush(targetRb);
        }
        else
        {
            // While refilled, allow future delayed scheduling again
            _delayScheduled.Remove(drink);
        }
    }

    private IEnumerator DelayPushOnAlreadyEmpty(DrinkComponent drink, Rigidbody targetRb)
    {
        yield return new WaitForSeconds(pushDelayOnAlreadyEmpty);

        // If it refilled during delay, clear scheduled flag and stop
        if (drink == null || drink.DrinkSeconds > 0f)
        {
            _delayScheduled.Remove(drink);
            yield break;
        }

        // remove scheduled marker before attempting push so future scheduling is possible
        _delayScheduled.Remove(drink);

        // If target RB is gone or push is still on cooldown/locked, do nothing
        if (targetRb == null)
            yield break;

        if (!CanPushNow(targetRb))
            yield break;

        TryPush(targetRb);
    }

    private void TryPushNoDrink(Rigidbody rb)
    {
        if (rb == null)
            return;

        TryPush(rb);
    }

    private void TryPush(Rigidbody targetRb)
    {
        if (targetRb == null || player == null)
            return;

        if (!CanPushNow(targetRb))
            return;

        bool pushed = false;

        // Try parabolic push first
        if (TryComputeBallisticVelocity(targetRb.position, player.position, out var v0, out var flightTime))
        {
            Vector3 deltaV = v0 - targetRb.velocity;
            if (IsFinite(deltaV))
            {
                targetRb.AddForce(deltaV, ForceMode.VelocityChange);
                ApplySpin(targetRb, v0.normalized);
                pushed = true;

                // Lock pushes until the end of the flight (plus a small margin)
                LockForFlight(targetRb, flightTime + 0.05f);
            }
        }

        // Fallback: simple impulse toward player
        if (!pushed)
        {
            Vector3 to = (player.position - targetRb.position);
            float sq = to.sqrMagnitude;
            if (sq > 1e-6f)
            {
                Vector3 dir = to / Mathf.Sqrt(sq);
                targetRb.AddForce(dir * pushImpulse, ForceMode.Impulse);
                ApplySpin(targetRb, dir);
                pushed = true;

                // Estimate a rough "flight" time to avoid quick re-pushes
                float horizDist = new Vector3(to.x, 0f, to.z).magnitude;
                float estSpeed = Mathf.Max(0.1f, pushImpulse / Mathf.Max(0.01f, targetRb.mass));
                float estTime = Mathf.Clamp(horizDist / estSpeed, 0.25f, 3f);
                LockForFlight(targetRb, estTime);
            }
        }

        if (pushed)
            MarkPushed(targetRb);
    }

    private bool CanPushNow(Rigidbody rb)
    {
        // In-flight lock
        if (_lockUntil.TryGetValue(rb, out var until) && Time.time < until)
            return false;

        // Cooldown
        if (_lastPushTime.TryGetValue(rb, out var last))
            return (Time.time - last) >= pushCooldown;

        return true;
    }

    private void MarkPushed(Rigidbody rb)
    {
        _lastPushTime[rb] = Time.time;
    }

    private void LockForFlight(Rigidbody rb, float seconds)
    {
        float until = Time.time + Mathf.Max(0f, seconds);
        if (_lockUntil.TryGetValue(rb, out var existing))
            _lockUntil[rb] = Mathf.Max(existing, until);
        else
            _lockUntil[rb] = until;
    }

    // Compute launch velocity and total time for a ballistic arc reaching a chosen apex height
    private bool TryComputeBallisticVelocity(Vector3 start, Vector3 end, out Vector3 v0, out float tTotal)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        if (g < 1e-5f)
        {
            v0 = Vector3.zero;
            tTotal = 0f;
            return false;
        }

        float apexY = Mathf.Max(start.y, end.y) + Mathf.Max(0.01f, arcHeight);

        // Vertical to reach apex
        float vy0 = Mathf.Sqrt(2f * g * Mathf.Max(0f, apexY - start.y));
        float tUp = vy0 / g;

        // Time from apex down to target
        float tDown = Mathf.Sqrt(2f * Mathf.Max(0f, apexY - end.y) / g);

        tTotal = tUp + tDown;
        if (tTotal < 1e-3f || float.IsNaN(tTotal) || float.IsInfinity(tTotal))
        {
            v0 = Vector3.zero;
            return false;
        }

        Vector3 toTarget = end - start;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
        Vector3 vHoriz = toTargetXZ / tTotal;

        v0 = vHoriz + Vector3.up * vy0;
        return IsFinite(v0);
    }

    private void ApplySpin(Rigidbody rb, Vector3 travelDir)
    {
        if (!addSpin || rb == null)
            return;

        if (travelDir.sqrMagnitude < 1e-6f)
            travelDir = Vector3.forward;

        // Base axis: mostly perpendicular to travel direction (slight preference for tumbling)
        Vector3 baseAxis = Vector3.Cross(Vector3.up, travelDir);
        if (baseAxis.sqrMagnitude < 1e-6f)
            baseAxis = Vector3.Cross(Vector3.right, travelDir);
        baseAxis.Normalize();

        // Blend with random axis for variation
        Vector3 randomAxis = Random.onUnitSphere;
        Vector3 axis = Vector3.Slerp(baseAxis, randomAxis, Mathf.Clamp01(spinRandomness)).normalized;

        // Ensure high enough cap to allow visible rotation
        if (maxAngularVelocity > 0f)
            rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, maxAngularVelocity);

        // Instant angular velocity change (mass/inertia independent)
        rb.AddTorque(axis * spinStrength, ForceMode.VelocityChange);
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class SuckCup : MonoBehaviour
{
    [SerializeField] private string fillTag = "Dragable";
    [SerializeField] private float fillRatePerSecond = 1f;
    [SerializeField] private bool searchInParentsIfMissing = true;

    [Header("Push on empty")]
    [SerializeField] private Transform player;
    [SerializeField] private float pushImpulse = 5f;
    [SerializeField] private float pushDelayOnAlreadyEmpty = 1f;

    [Header("Parabolic push")]
    [SerializeField] private float arcHeight = 2f;
    [SerializeField] private float pushCooldown = 1.0f;

    [Header("Spin on push")]
    [SerializeField] private bool addSpin = true;
    [SerializeField] [Range(0f, 50f)] private float spinStrength = 10f;
    [SerializeField] [Range(0f, 1f)] private float spinRandomness = 0.3f;
    [SerializeField] private float maxAngularVelocity = 50f;

    private EventInstance drinkSoundInstance;
    private bool isDrinkSoundPlaying = false;

    private const string DRINK_EVENT_PATH = "event:/DrinkNPC";
    private const string THROW_EVENT_PATH = "event:/CupThrow";

    public bool isEmpty = true;

    private readonly Dictionary<Rigidbody, float> _lastPushTime = new Dictionary<Rigidbody, float>();
    private readonly Dictionary<Rigidbody, float> _lockUntil = new Dictionary<Rigidbody, float>();
    private readonly HashSet<DrinkComponent> _delayScheduled = new HashSet<DrinkComponent>();
    private readonly HashSet<Collider> _touchingTagged = new HashSet<Collider>();

    private void OnEnable()
    {
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
        if (collision == null) return;

        var col = collision.collider;
        if (col != null && (col.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag)))
        {
            _touchingTagged.Add(col);
            isEmpty = false;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision == null) return;

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

        // already empty
        if (current <= 0f)
        {
            StopDrinkingSound();

            if (pushDelayOnAlreadyEmpty > 0f)
            {
                if (_delayScheduled.Add(drink))
                    StartCoroutine(DelayPushOnAlreadyEmpty(drink, targetRb));
            }
            else
            {
                TryPush(targetRb);
            }
            return;
        }

        // play drinking loop
        StartDrinkingSound(drink.gameObject);

        // drain
        float newValue = Mathf.Max(0f, current - fillRatePerSecond * deltaTime);
        drink.DrinkSeconds = newValue;

        // if just became empty
        if (newValue <= 0f)
        {
            StopDrinkingSound();
            _delayScheduled.Remove(drink);
            TryPush(targetRb);
        }
        else
        {
            _delayScheduled.Remove(drink);
        }
    }

    private IEnumerator DelayPushOnAlreadyEmpty(DrinkComponent drink, Rigidbody targetRb)
    {
        yield return new WaitForSeconds(pushDelayOnAlreadyEmpty);

        if (drink == null || drink.DrinkSeconds > 0f)
        {
            _delayScheduled.Remove(drink);
            yield break;
        }

        _delayScheduled.Remove(drink);

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

        StopDrinkingSound();
        TryPush(rb);
    }

    private void TryPush(Rigidbody targetRb)
    {
        if (targetRb == null || player == null)
            return;

        if (!CanPushNow(targetRb))
            return;

        bool pushed = false;

        // try parabolic push
        if (TryComputeBallisticVelocity(targetRb.position, player.position, out var v0, out var flightTime))
        {
            Vector3 deltaV = v0 - targetRb.linearVelocity;
            if (IsFinite(deltaV))
            {
                targetRb.AddForce(deltaV, ForceMode.VelocityChange);
                ApplySpin(targetRb, v0.normalized);
                pushed = true;
                LockForFlight(targetRb, flightTime + 0.05f);
            }
        }

        // fallback impulse
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

                float horizDist = new Vector3(to.x, 0f, to.z).magnitude;
                float estSpeed = Mathf.Max(0.1f, pushImpulse / Mathf.Max(0.01f, targetRb.mass));
                float estTime = Mathf.Clamp(horizDist / estSpeed, 0.25f, 3f);
                LockForFlight(targetRb, estTime);
            }
        }

        if (pushed)
        {
            MarkPushed(targetRb);
            ThrowSound(targetRb.gameObject); // ?? play throw sound here
        }
    }

    private bool CanPushNow(Rigidbody rb)
    {
        if (_lockUntil.TryGetValue(rb, out var until) && Time.time < until)
            return false;

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
        float vy0 = Mathf.Sqrt(2f * g * Mathf.Max(0f, apexY - start.y));
        float tUp = vy0 / g;
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

        Vector3 baseAxis = Vector3.Cross(Vector3.up, travelDir);
        if (baseAxis.sqrMagnitude < 1e-6f)
            baseAxis = Vector3.Cross(Vector3.right, travelDir);
        baseAxis.Normalize();

        Vector3 randomAxis = Random.onUnitSphere;
        Vector3 axis = Vector3.Slerp(baseAxis, randomAxis, Mathf.Clamp01(spinRandomness)).normalized;

        if (maxAngularVelocity > 0f)
            rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, maxAngularVelocity);

        rb.AddTorque(axis * spinStrength, ForceMode.VelocityChange);
    }

    private void StartDrinkingSound(GameObject source)
    {
        if (isDrinkSoundPlaying) return;

        drinkSoundInstance = RuntimeManager.CreateInstance(DRINK_EVENT_PATH);
        drinkSoundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
        drinkSoundInstance.start();
        isDrinkSoundPlaying = true;
    }

    private void StopDrinkingSound()
    {
        if (!isDrinkSoundPlaying) return;

        drinkSoundInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        drinkSoundInstance.release();
        isDrinkSoundPlaying = false;
    }

    private void ThrowSound(GameObject source)
    {
        EventInstance throwInstance = RuntimeManager.CreateInstance(THROW_EVENT_PATH);
        throwInstance.set3DAttributes(RuntimeUtils.To3DAttributes(source));
        throwInstance.start();
        throwInstance.release();
    }

    private static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}

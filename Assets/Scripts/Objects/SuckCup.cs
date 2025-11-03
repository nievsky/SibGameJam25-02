using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class SuckCup : MonoBehaviour
{
    [SerializeField] private string fillTag = "Dragable";
    [SerializeField] private float fillRatePerSecond = 1f;
    [SerializeField] private bool searchInParentsIfMissing = true;

    // Push-on-empty settings
    [Header("Push on empty")]
    [SerializeField] private Transform player; // assign in Inspector
    [SerializeField] private float pushImpulse = 5f;

    private EventInstance drinkSoundInstance;
    private EventInstance throwSoundInstance;
    private bool isDrinkSoundPlaying = false;
    private bool isThrowSoundPlaying = false;
    private const string DRINK_EVENT_PATH = "event:/DrinkNPC";
    private const string THROW_EVENT_PATH = "event:/CupThrow";

    private readonly HashSet<DrinkComponent> pushedOnEmpty = new HashSet<DrinkComponent>();

    private void OnTriggerStay(Collider other)
    {
        if (other == null || !other.CompareTag(fillTag))
            return;

        if (!TryGetDrinkComponent(other, out var drink))
            return;

        var rb = other.attachedRigidbody;
        if (rb == null && searchInParentsIfMissing)
            rb = other.GetComponentInParent<Rigidbody>();

        SuckOverTime(drink, rb, Time.deltaTime);
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision == null)
            return;

        var col = collision.collider;
        if (!(col != null && (col.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag))))
            return;

        if (!TryGetDrinkComponent(col, out var drink))
            return;

        var rb = collision.rigidbody ?? col.attachedRigidbody;
        if (rb == null && searchInParentsIfMissing)
            rb = col.GetComponentInParent<Rigidbody>();

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

        return false;
    }

    private void SuckOverTime(DrinkComponent drink, Rigidbody targetRb, float deltaTime)
    {
        if (drink == null || deltaTime <= 0f)
            return;

        float current = drink.DrinkSeconds;
        if (current <= 0f)
        {
            StopDrinkingSound();
            return;
        }

        StartDrinkingSound(drink.gameObject);

        float newValue = Mathf.Max(0f, current - fillRatePerSecond * deltaTime);
        drink.DrinkSeconds = newValue;

        if (newValue <= 0f && !pushedOnEmpty.Contains(drink))
        {
            pushedOnEmpty.Add(drink);
            PushCupTowardPlayer(targetRb);
            StopDrinkingSound();
        }
        else if (newValue > 0f && pushedOnEmpty.Contains(drink))
        {
            // Allow push again if refilled later
            pushedOnEmpty.Remove(drink);
        }

    }


    private void PushCupTowardPlayer(Rigidbody targetRb)
    {
        StopDrinkingSound();

        if (targetRb == null || player == null)
            return;

        Vector3 dir = (player.position - targetRb.position).normalized;
        if (dir.sqrMagnitude < 1e-6f)
            return;

        // Apply throw force
        targetRb.AddForce(dir * pushImpulse, ForceMode.Impulse);


        ThrowSound(targetRb.gameObject);
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

}
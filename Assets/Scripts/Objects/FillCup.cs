using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class FillCup : MonoBehaviour
{
    [SerializeField] private string fillTag = "Dragable";
    [SerializeField] private float fillRatePerSecond = 1f;
    [SerializeField] private bool searchInParentsIfMissing = true;


    private string fillEvent = "event:/Fill";
    private EventInstance fillInstance;

    private bool isFillingSoundPlaying = false;

    private void OnTriggerStay(Collider other)
    {
        if (other == null || !other.CompareTag(fillTag))
            return;

        if (!TryGetDrinkComponent(other, out var drink))
            return;

        bool wasFull = drink.DrinkSeconds >= drink.MaxDrinkSeconds;

        FillOverTime(drink, Time.deltaTime);

        bool isFull = drink.DrinkSeconds >= drink.MaxDrinkSeconds;

        if (!wasFull && !isFull) 
        {
            StartFillingSound();
        }
        else if (isFull)
        {
            StopFillingSound();
        }
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

    private void FillOverTime(DrinkComponent drink, float deltaTime)
    {
        if (drink == null || deltaTime <= 0f)
            return;

        float target = drink.MaxDrinkSeconds;
        if (drink.DrinkSeconds >= target)
            return;

        drink.DrinkSeconds = Mathf.Min(target, drink.DrinkSeconds + fillRatePerSecond * deltaTime);
    }

    private void StartFillingSound()
    {
        if (isFillingSoundPlaying) return;

        fillInstance = RuntimeManager.CreateInstance(fillEvent);
        fillInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
        fillInstance.start();
        isFillingSoundPlaying = true;
    }

    private void StopFillingSound()
    {
        if (!isFillingSoundPlaying) return;

        fillInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        fillInstance.release();
        isFillingSoundPlaying = false;
    }

    private void OnDisable()
    {
        StopFillingSound();
    }
}

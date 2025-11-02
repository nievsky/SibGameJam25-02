using UnityEngine;

public class FillCup : MonoBehaviour
{
    [SerializeField] private string fillTag = "Dragable";
    [SerializeField] private float fillRatePerSecond = 1f;
    [SerializeField] private bool searchInParentsIfMissing = true;

    private void OnTriggerStay(Collider other)
    {
        if (other == null || !other.CompareTag(fillTag))
            return;

        if (!TryGetDrinkComponent(other, out var drink))
            return;

        FillOverTime(drink, Time.deltaTime);
    }

    private bool TryGetDrinkComponent(Collider other, out DrinkComponent drink)
    {
        // Directly on the collider's GameObject
        if (other.TryGetComponent(out drink))
            return true;

        // Optionally search hierarchy if not directly present
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
}
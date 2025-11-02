using UnityEngine;

public class SuckCup : MonoBehaviour
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

        SuckOverTime(drink, Time.deltaTime);
    }
    
    private void OnCollisionStay(Collision collision)
    {
        if (collision != null && (collision.collider.CompareTag(fillTag) || collision.gameObject.CompareTag(fillTag)))
        {
            if (TryGetDrinkComponent(collision.collider, out var drink))
            {
                SuckOverTime(drink, Time.deltaTime);
                return;
            }
        }
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

    private void SuckOverTime(DrinkComponent drink, float deltaTime)
    {
        if (drink == null || deltaTime <= 0f)
            return;

        float target = 0f;
        if (drink.DrinkSeconds <= target)
            return;

        drink.DrinkSeconds = Mathf.Max(target, drink.DrinkSeconds - fillRatePerSecond * deltaTime);
    }
}

using UnityEngine;

public class DrinkComponent : MonoBehaviour
{ 
    [Tooltip("How many seconds this drink can be consumed for.")]
    public float DrinkSeconds = 5f;

    public float MaxDrinkSeconds = 5f;
}

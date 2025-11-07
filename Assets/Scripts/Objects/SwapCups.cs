// csharp
using UnityEngine;

[DisallowMultipleComponent]
public class SwapCups : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DrinkComponent drink;   // On the same Cup (parent) object
    [SerializeField] private GameObject emptyModel;  // Visual for empty cup (A)
    [SerializeField] private GameObject fullModel;   // Visual for full cup (B)

    [Header("Settings")]
    [Tooltip("Tolerance for float comparisons.")]
    [Min(0f)] [SerializeField] private float epsilon = 0.01f;

    private enum State { Unknown, Empty, Full }
    private State _last = State.Unknown;

    private void Reset()
    {
        drink = GetComponent<DrinkComponent>();
        // Optionally auto-guess by child names
        if (emptyModel == null) emptyModel = transform.Find("Empty")?.gameObject;
        if (fullModel == null) fullModel = transform.Find("Full")?.gameObject;
    }

    private void Awake()
    {
        if (drink == null)
            drink = GetComponent<DrinkComponent>();
    }

    private void OnEnable()
    {
        Apply(true);
    }

    private void Update()
    {
        Apply(false);
    }

    private void OnValidate()
    {
        // Update visuals in editor when values change
        if (isActiveAndEnabled)
            Apply(true);
    }

    private void Apply(bool force)
    {
        if (drink == null) return;

        var state = GetState(drink.DrinkSeconds, drink.MaxDrinkSeconds);
        if (!force && state == _last) return;

        _last = state;

        // Only swap at extremes; do nothing for in-between values.
        if (state == State.Empty)
        {
            SetActiveSafe(emptyModel, true);
            SetActiveSafe(fullModel, false);
        }
        else if (state == State.Full)
        {
            SetActiveSafe(emptyModel, false);
            SetActiveSafe(fullModel, true);
        }
        // State.Unknown: keep current visuals as-is
    }

    private State GetState(float value, float max)
    {
        // Clamp to valid range for robust comparison
        float v = Mathf.Clamp(value, 0f, Mathf.Max(0f, max));

        bool isEmpty = v <= epsilon;
        bool isFull = max <= epsilon ? true : Mathf.Abs(v - max) <= epsilon || v >= (max - epsilon);

        if (isEmpty) return State.Empty;
        if (isFull) return State.Full;
        return State.Unknown;
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ManagerLogic : MonoBehaviour
{
    [SerializeField] private List<SuckCup> cups = new List<SuckCup>();
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float rotateSpeed = 90f; // degrees per second

    [Header("Visibility")]
    [SerializeField, Range(0f, 360f)]
    private float visibleFovDegrees = 180f; // total FOV angle
    [SerializeField, Tooltip("Yaw offset (deg) of the view cone relative to forward. + turns right, - left.")]
    private float visibleYawOffsetDegrees = 0f;
    [SerializeField, Tooltip("Ignore vertical when checking visibility.")]
    private bool ignoreVertical = true;

    [Header("Detection")]
    [SerializeField, Tooltip("How long the player must keep drinking while visible before reloading.")]
    [Min(0f)] private float drinkingConfirmTime = 0.5f;

    private bool _playerDrinking;
    private Coroutine _rotationCo;
    private Coroutine _drinkingCheckCo;
    private CupState _lastCupState = CupState.Mixed;
    private bool _isReloading;

    public bool PlayerDrinking => _playerDrinking;

    private void Update()
    {
        // Update Player.Drinking each frame; default to false if not found
        _playerDrinking = TryGetPlayerDrinking(playerObject, out var drinking) && drinking;

        var state = GetCupState();

        // Trigger only on state changes and when not already rotating
        if (_rotationCo == null && state != _lastCupState)
        {
            if (state == CupState.AllEmpty)
            {
                _rotationCo = StartCoroutine(RotateYBy(180f));
            }
            else if (state == CupState.AllFull)
            {
                _rotationCo = StartCoroutine(RotateYBy(180f));
            }
        }

        _lastCupState = state;

        // Delayed scene reload: start/stop confirmation coroutine
        if (!_isReloading)
        {
            bool inView = IsPlayerInVisibleZone();

            if (inView && _playerDrinking)
            {
                if (_drinkingCheckCo == null)
                    _drinkingCheckCo = StartCoroutine(ConfirmDrinkingThenReload());
            }
            else
            {
                if (_drinkingCheckCo != null)
                {
                    StopCoroutine(_drinkingCheckCo);
                    _drinkingCheckCo = null;
                }
            }
        }

        Debug.Log("Is player drinking: " + _playerDrinking);
        Debug.Log("Is player in visible zone: " + IsPlayerInVisibleZone());
    }

    private enum CupState { AllEmpty, AllFull, Mixed }

    private CupState GetCupState()
    {
        if (cups == null || cups.Count == 0) return CupState.Mixed;

        bool anyNull = false;
        bool anyEmpty = false;
        bool anyFull = false;

        foreach (var cup in cups)
        {
            if (cup == null) { anyNull = true; break; }
            if (cup.isEmpty) anyEmpty = true; else anyFull = true;
            if (anyEmpty && anyFull) break;
        }

        if (anyNull) return CupState.Mixed;
        if (anyEmpty && !anyFull) return CupState.AllEmpty;   // all isEmpty == true
        if (!anyEmpty && anyFull) return CupState.AllFull;    // all isEmpty == false
        return CupState.Mixed;
    }

    private IEnumerator RotateYBy(float degrees)
    {
        var target = transform.rotation * Quaternion.Euler(0f, degrees, 0f);
        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotateSpeed * Time.deltaTime
            );
            yield return null;
        }
        transform.rotation = target;
        _rotationCo = null;
    }

    // Confirmation window: require continuous visibility and drinking for 'drinkingConfirmTime'
    private IEnumerator ConfirmDrinkingThenReload()
    {
        float elapsed = 0f;
        while (elapsed < drinkingConfirmTime)
        {
            // Cancel if condition breaks during the window
            if (!_playerDrinking || !IsPlayerInVisibleZone())
            {
                _drinkingCheckCo = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!_isReloading)
            ReloadCurrentScene();

        _drinkingCheckCo = null;
    }

    // Check if player is inside the forward FOV cone with yaw offset
    private bool IsPlayerInVisibleZone()
    {
        if (playerObject == null) return false;

        Vector3 toPlayer = playerObject.transform.position - transform.position;

        // Prepare reference forward with yaw offset
        Vector3 forward = transform.forward;
        Quaternion yawOffset = Quaternion.Euler(0f, visibleYawOffsetDegrees, 0f);

        if (ignoreVertical)
        {
            toPlayer.y = 0f;
            forward = new Vector3(forward.x, 0f, forward.z);
        }

        if (toPlayer.sqrMagnitude <= Mathf.Epsilon) return true;

        Vector3 centerDir = (yawOffset * forward).normalized;
        float halfFov = Mathf.Clamp(visibleFovDegrees, 0f, 360f) * 0.5f;

        return Vector3.Angle(centerDir, toPlayer) <= halfFov;
    }

    private void ReloadCurrentScene()
    {
        _isReloading = true;
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    // More robust: search in player and children, allow non-public members, and common name variants.
    private static bool TryGetPlayerDrinking(GameObject player, out bool drinking)
    {
        drinking = false;
        if (player == null) return false;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var monos = player.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (m == null) continue;
            var t = m.GetType();

            // Prefer field lookup
            var field = t.GetField("Drinking", flags)
                        ?? t.GetField("isDrinking", flags);
            if (field != null && TryConvertToBool(field.GetValue(m), out drinking))
                return true;

            // Fallback to property lookup
            var prop = t.GetProperty("Drinking", flags)
                       ?? t.GetProperty("IsDrinking", flags);
            if (prop != null && prop.CanRead && TryConvertToBool(prop.GetValue(m), out drinking))
                return true;
        }

        return false;
    }

    private static bool TryConvertToBool(object value, out bool result)
    {
        switch (value)
        {
            case bool b:
                result = b; return true;
            case int i:
                result = i != 0; return true;
            case float f:
                result = Mathf.Abs(f) > Mathf.Epsilon; return true;
            case double d:
                result = Mathf.Abs((float)d) > Mathf.Epsilon; return true;
            case byte b8:
                result = b8 != 0; return true;
            case sbyte sb:
                result = sb != 0; return true;
            case short s:
                result = s != 0; return true;
            case ushort us:
                result = us != 0; return true;
            case uint ui:
                result = ui != 0; return true;
            case long l:
                result = l != 0; return true;
            case ulong ul:
                result = ul != 0; return true;
            default:
                result = false; return false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Visualize the FOV with yaw offset in the Scene view
        Gizmos.color = Color.yellow;
        float halfFov = Mathf.Clamp(visibleFovDegrees, 0f, 360f) * 0.5f;

        Vector3 origin = transform.position;
        Vector3 baseForward = transform.forward;
        if (ignoreVertical) baseForward = new Vector3(baseForward.x, 0f, baseForward.z).normalized;

        Vector3 leftDir  = Quaternion.Euler(0f, visibleYawOffsetDegrees - halfFov, 0f) * baseForward;
        Vector3 rightDir = Quaternion.Euler(0f, visibleYawOffsetDegrees + halfFov, 0f) * baseForward;
        Vector3 center   = Quaternion.Euler(0f, visibleYawOffsetDegrees, 0f) * baseForward;

        float rayLen = 2.0f;
        Gizmos.DrawRay(origin, center * rayLen);
        Gizmos.DrawRay(origin, leftDir * rayLen);
        Gizmos.DrawRay(origin, rightDir * rayLen);
    }
#endif
}
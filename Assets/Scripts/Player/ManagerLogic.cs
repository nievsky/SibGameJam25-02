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
    [SerializeField] private float visibleFovDegrees = 180f; // total FOV angle

    private bool _playerDrinking;
    private Coroutine _rotationCo;
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

        // If player is within visible zone and drinking -> reload scene once
        if (!_isReloading && IsPlayerInVisibleZone(visibleFovDegrees) && _playerDrinking)
        {
            ReloadCurrentScene();
        }
        Debug.Log("Is player drinking: " + _playerDrinking);
        Debug.Log("Is player in visible zone: " + IsPlayerInVisibleZone(visibleFovDegrees));
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

    // Check if player is inside the forward FOV cone (total angle = visibleFovDegrees)
    private bool IsPlayerInVisibleZone(float totalFovDeg)
    {
        if (playerObject == null) return false;

        Vector3 toPlayer = playerObject.transform.position - transform.position;
        toPlayer.y = 0f; // ignore vertical angle
        if (toPlayer.sqrMagnitude <= Mathf.Epsilon) return true;

        Vector3 forward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        float halfFov = Mathf.Clamp(totalFovDeg, 0f, 360f) * 0.5f;

        return Vector3.Angle(forward, toPlayer) <= halfFov;
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
}
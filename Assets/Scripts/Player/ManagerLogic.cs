using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ManagerLogic : MonoBehaviour
{
    [SerializeField] private List<SuckCup> cups = new List<SuckCup>();
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Penalty")]
    [SerializeField] private float drunkPenaltyOnCaught = 15f;
    
    [Header("Visibility")]
    [SerializeField, Range(0f, 360f)] private float visibleFovDegrees = 180f;
    [SerializeField] private float visibleYawOffsetDegrees = 0f;
    [SerializeField] private bool ignoreVertical = true;

    [Header("Detection")]
    [SerializeField] private float drinkingConfirmTime = 0.5f;

    [Header("Caught Message")]
    [SerializeField] private TypeWritterRandom typeWritterRandom;
    [SerializeField, TextArea(2, 4)] private List<string> drinkingWarningMessages = new List<string>();

    private bool _playerDrinking;
    private Coroutine _rotationCo;
    private Coroutine _drinkingCheckCo;
    private CupState _lastCupState = CupState.Mixed;
    private bool _messagePlaying;

    public bool PlayerDrinking => _playerDrinking;
    public bool IsPlayerVisibleNow => IsPlayerInVisibleZone();

    private void Awake()
    {
        if (typeWritterRandom == null)
            typeWritterRandom = FindObjectOfType<TypeWritterRandom>(true);
    }

    private void OnEnable()
    {
        if (typeWritterRandom != null)
            typeWritterRandom.Finished += OnTypewriterFinished;
    }

    private void OnDisable()
    {
        if (typeWritterRandom != null)
            typeWritterRandom.Finished -= OnTypewriterFinished;
    }

    private void Update()
    {
        _playerDrinking = TryGetPlayerDrinking(playerObject, out var drinking) && drinking;
        var state = GetCupState();

        if (_rotationCo == null && state != _lastCupState)
        {
            if (state == CupState.AllEmpty || state == CupState.AllFull)
            {
                //  Play manager alert sound right before he starts to spin
                FMODUnity.RuntimeManager.PlayOneShot("event:/Manager/ManagerAlert", transform.position);

                _rotationCo = StartCoroutine(RotateYBy(180f));
            }
        }

        _lastCupState = state;

        if (_messagePlaying)
        {
            if (_drinkingCheckCo != null)
            {
                StopCoroutine(_drinkingCheckCo);
                _drinkingCheckCo = null;
            }
            return;
        }

        bool inView = IsPlayerInVisibleZone();
        if (inView && _playerDrinking)
        {
            if (_drinkingCheckCo == null)
                _drinkingCheckCo = StartCoroutine(ConfirmDrinkingThenShowMessage());
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

    private enum CupState { AllEmpty, AllFull, Mixed }

    private CupState GetCupState()
    {
        if (cups == null || cups.Count == 0) return CupState.Mixed;

        bool anyEmpty = false;
        bool anyFull = false;

        foreach (var cup in cups)
        {
            if (cup == null) return CupState.Mixed;
            if (cup.isEmpty) anyEmpty = true; else anyFull = true;
            if (anyEmpty && anyFull) break;
        }

        if (anyEmpty && !anyFull) return CupState.AllEmpty;
        if (!anyEmpty && anyFull) return CupState.AllFull;
        return CupState.Mixed;
    }

    private IEnumerator RotateYBy(float degrees)
    {
        var target = transform.rotation * Quaternion.Euler(0f, degrees, 0f);
        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = target;
        _rotationCo = null;
    }
    
    

    private IEnumerator ConfirmDrinkingThenShowMessage()
    {
        float elapsed = 0f;
        while (elapsed < drinkingConfirmTime)
        {
            if (!_playerDrinking || !IsPlayerInVisibleZone())
            {
                _drinkingCheckCo = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Manager alarm sound (when he catches you drinking)
        FMODUnity.RuntimeManager.PlayOneShot("event:/Manager/ManagerAlarm", transform.position);

        // Apply drunk penalty once on catch
        ApplyDrunkPenalty();

        TriggerCaughtMessage();
        _drinkingCheckCo = null;
    }

    private void ApplyDrunkPenalty()
    {
        if (playerObject == null) return;

        var drinkable = playerObject.GetComponentInChildren<Drinkable>(true);
        if (drinkable == null) return;

        drinkable.Drunk = Mathf.Max(0f, drinkable.Drunk - drunkPenaltyOnCaught);
    }

    private bool IsPlayerInVisibleZone()
    {
        if (playerObject == null) return false;
        Vector3 toPlayer = playerObject.transform.position - transform.position;
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

    private void TriggerCaughtMessage()
    {
        _messagePlaying = true;

        if (typeWritterRandom == null)
        {
            Debug.LogWarning("ManagerLogic: TypeWritterRandom reference is not set.");
            _messagePlaying = false;
            return;
        }

        if (drinkingWarningMessages != null && drinkingWarningMessages.Count > 0)
            typeWritterRandom.PlayRandomFrom(drinkingWarningMessages);
        else
            typeWritterRandom.Play();
    }

    private void OnTypewriterFinished()
    {
        _messagePlaying = false;
    }

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

            var field = t.GetField("Drinking", flags)
                        ?? t.GetField("isDrinking", flags);
            if (field != null && TryConvertToBool(field.GetValue(m), out drinking))
                return true;

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
            case bool b: result = b; return true;
            case int i: result = i != 0; return true;
            case float f: result = Mathf.Abs(f) > Mathf.Epsilon; return true;
            default: result = false; return false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        float halfFov = Mathf.Clamp(visibleFovDegrees, 0f, 360f) * 0.5f;

        Vector3 origin = transform.position;
        Vector3 baseForward = transform.forward;
        if (ignoreVertical) baseForward = new Vector3(baseForward.x, 0f, baseForward.z).normalized;

        Vector3 leftDir = Quaternion.Euler(0f, visibleYawOffsetDegrees - halfFov, 0f) * baseForward;
        Vector3 rightDir = Quaternion.Euler(0f, visibleYawOffsetDegrees + halfFov, 0f) * baseForward;
        Vector3 center = Quaternion.Euler(0f, visibleYawOffsetDegrees, 0f) * baseForward;

        float rayLen = 2f;
        Gizmos.DrawRay(origin, center * rayLen);
        Gizmos.DrawRay(origin, leftDir * rayLen);
        Gizmos.DrawRay(origin, rightDir * rayLen);
    }
#endif
}

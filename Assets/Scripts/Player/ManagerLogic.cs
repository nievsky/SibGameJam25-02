using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ManagerLogic : MonoBehaviour
{
    [SerializeField] private List<SuckCup> cups = new List<SuckCup>();
    [SerializeField] private GameObject playerObject;
    [SerializeField] private float rotateSpeed = 90f; // degrees per second

    private bool _playerDrinking;
    private bool _rotating;
    private bool _rotationDone;
    private Quaternion _targetRotation;

    public bool PlayerDrinking => _playerDrinking;

    private void Update()
    {
        // Read Player.Drinking (bool/int/float supported) from any attached component
        if (TryGetPlayerDrinking(playerObject, out var drinking))
            _playerDrinking = drinking;

        if (_rotationDone) return;

        // When all cups are "full" (isEmpty == false), rotate back 180 degrees once
        if (!_rotating && AllCupsFull())
        {
            _targetRotation = transform.rotation * Quaternion.Euler(0f, 180f, 0f);
            _rotating = true;
        }

        if (_rotating)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _targetRotation,
                rotateSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, _targetRotation) < 0.1f)
            {
                transform.rotation = _targetRotation;
                _rotating = false;
                _rotationDone = true;
            }
        }
    }

    // True only when every cup has isEmpty == false
    private bool AllCupsFull()
    {
        if (cups == null || cups.Count == 0) return false;
        foreach (var cup in cups)
        {
            if (cup == null) return false;
            if (cup.isEmpty) return false;
        }
        return true;
    }

    private static bool TryGetPlayerDrinking(GameObject player, out bool drinking)
    {
        drinking = false;
        if (player == null) return false;

        var monos = player.GetComponents<MonoBehaviour>();
        foreach (var m in monos)
        {
            var t = m.GetType();

            var prop = t.GetProperty("Drinking", BindingFlags.Instance | BindingFlags.Public);
            if (prop != null && TryConvertToBool(prop.GetValue(m), out drinking))
                return true;

            var field = t.GetField("Drinking", BindingFlags.Instance | BindingFlags.Public);
            if (field != null && TryConvertToBool(field.GetValue(m), out drinking))
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
            default:
                result = false; return false;
        }
    }
}
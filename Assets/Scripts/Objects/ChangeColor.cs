using System;
using System.Reflection;
using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Renderer targetRenderer;

    [Header("Source (Parent)")]
    [Tooltip("If not set, parents will be scanned for the properties.")]
    [SerializeField] private Component parentWithValues;

    [Header("Property Names")]
    [SerializeField] private string drinkSecondsName = "DrinkSeconds";
    [SerializeField] private string maxDrinkSecondsName = "MaxDrinkSeconds";

    [Header("Shader Property")]
    [Tooltip("URP/HDRP: _BaseColor, Built-in: _Color. Auto-fallback to _Color if not found.")]
    [SerializeField] private string colorPropertyName = "_BaseColor";

    private MaterialPropertyBlock _mpb;
    private int _colorId;
    private bool _hasColorProp;

    private Func<float> _getDrinkSeconds;
    private Func<float> _getMaxDrinkSeconds;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        // Resolve shader color property (_BaseColor -> _Color fallback).
        var mat = targetRenderer != null ? targetRenderer.sharedMaterial : null;
        _colorId = Shader.PropertyToID(colorPropertyName);
        _hasColorProp = mat != null && mat.HasProperty(_colorId);
        if (!_hasColorProp && colorPropertyName == "_BaseColor")
        {
            colorPropertyName = "_Color";
            _colorId = Shader.PropertyToID(colorPropertyName);
            _hasColorProp = mat != null && mat.HasProperty(_colorId);
        }

        // Find a parent component that has both properties.
        ResolveParentGetters();
    }

    private void Update()
    {
        if (targetRenderer == null || !_hasColorProp || _getDrinkSeconds == null || _getMaxDrinkSeconds == null)
            return;

        float cur = Mathf.Max(0f, _getDrinkSeconds());
        float max = Mathf.Max(0.0001f, _getMaxDrinkSeconds());
        float t = Mathf.Clamp01(cur / max); // 0 -> black, max -> white
        Color c = Color.Lerp(Color.black, Color.white, t);

        targetRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorId, c);
        targetRenderer.SetPropertyBlock(_mpb); // per-instance tint
    }

    private void ResolveParentGetters()
    {
        if (parentWithValues != null)
        {
            TryBindGetters(parentWithValues, out _getDrinkSeconds, out _getMaxDrinkSeconds);
            if (_getDrinkSeconds != null && _getMaxDrinkSeconds != null) return;
        }

        var parents = GetComponentsInParent<Component>(true);
        foreach (var comp in parents)
        {
            if (comp == this) continue;
            if (TryBindGetters(comp, out _getDrinkSeconds, out _getMaxDrinkSeconds))
                return;
        }

        Debug.LogWarning($"ChangeColor: Could not find both float properties '{drinkSecondsName}' and '{maxDrinkSecondsName}' in parents.");
    }

    private static bool TryBindGetters(object obj, out Func<float> drink, out Func<float> max)
    {
        drink = TryCreateFloatGetter(obj, "DrinkSeconds", out var g1) ? g1 : TryCreateFloatGetter(obj, "drinkSeconds", out g1) ? g1 : null;
        max   = TryCreateFloatGetter(obj, "MaxDrinkSeconds", out var g2) ? g2 : TryCreateFloatGetter(obj, "maxDrinkSeconds", out g2) ? g2 : null;

        // If custom names are needed, you can replace the above with the serialized names:
        // drink = TryCreateFloatGetter(obj, drinkSecondsName, out g1) ? g1 : null;
        // max   = TryCreateFloatGetter(obj, maxDrinkSecondsName, out g2) ? g2 : null;

        return drink != null && max != null;
    }

    private static bool TryCreateFloatGetter(object obj, string name, out Func<float> getter)
    {
        getter = null;
        if (obj == null || string.IsNullOrEmpty(name)) return false;

        var type = obj.GetType();

        // Field
        var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(float))
        {
            getter = () => (float)f.GetValue(obj);
            return true;
        }

        // Property
        var p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanRead && p.PropertyType == typeof(float))
        {
            getter = () => (float)p.GetValue(obj);
            return true;
        }

        return false;
    }
}
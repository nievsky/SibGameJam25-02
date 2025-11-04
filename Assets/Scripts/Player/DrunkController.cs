using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using FMODUnity;

public class DrunkController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Volume volume;
    [SerializeField] private Drinkable player;

    [Header("Response")]
    [SerializeField] private float smoothSpeed = 4f;
    [SerializeField] private float wobbleAmplitude = 0.2f;
    [SerializeField] private float wobbleFreqMin = 1.5f;
    [SerializeField] private float wobbleFreqMax = 6f;

    [Header("Targets at Drunk = 100")]
    [SerializeField] private float targetBloomIntensity = 12f;
    [SerializeField] private float targetChromaticIntensity = 1f;
    [SerializeField] private float targetVignetteIntensity = 0.45f;
    [SerializeField] private Color targetVignetteColor = new Color(0.95f, 0.85f, 1f);
    [SerializeField] private float targetLensDistortion = -0.45f;
    [SerializeField] private float targetFilmGrain = 0.7f;
    [SerializeField] private float targetPaniniDistance = 0.9f;
    [SerializeField] private float targetPaniniCropToFit = 1f;
    [SerializeField] private float targetSaturation = -30f;
    [SerializeField] private float targetPostExposure = 0.6f;

    private Bloom bloom;
    private ChromaticAberration chroma;
    private Vignette vignette;
    private LensDistortion lens;
    private FilmGrain grain;
    private PaniniProjection panini;
    private ColorAdjustments colorAdj;

    private float initBloom, initChroma, initVignette, initLens, initGrain, initPaniniDist, initPaniniCrop, initSaturation, initPostExposure;
    private Color initVignetteColor;
    private float currentT;

    private void Awake()
    {
        if (!volume) volume = GetComponent<Volume>();
        if (!player) player = FindFirstObjectByType<Drinkable>();
        CacheOverrides();
        CaptureInitial();
    }

    private void OnDisable() => RestoreInitial();

    private void Update()
    {
        float targetT = (player != null) ? Mathf.Clamp01(player.Drunk / 100f) : 0f;
        currentT = Mathf.MoveTowards(currentT, targetT, smoothSpeed * Time.unscaledDeltaTime);

        float t = EaseOutQuad(currentT);
        float wobble = Mathf.Sin(Time.unscaledTime * Mathf.Lerp(wobbleFreqMin, wobbleFreqMax, t)) * wobbleAmplitude * t;

        if (bloom != null)
            bloom.intensity.Override(Mathf.Lerp(initBloom, targetBloomIntensity, t));

        if (chroma != null)
            chroma.intensity.Override(Mathf.Clamp01(Mathf.Lerp(initChroma, targetChromaticIntensity, t) + Mathf.Abs(wobble) * 0.25f));

        if (vignette != null)
        {
            vignette.intensity.Override(Mathf.Lerp(initVignette, targetVignetteIntensity, t));
            vignette.color.Override(Color.Lerp(initVignetteColor, targetVignetteColor, t * 0.75f));
        }

        if (lens != null)
            lens.intensity.Override(Mathf.Clamp(Mathf.Lerp(initLens, targetLensDistortion, t) + wobble, -1f, 1f));

        if (grain != null)
            grain.intensity.Override(Mathf.Lerp(initGrain, targetFilmGrain, Mathf.Pow(t, 1.2f)));

        if (panini != null)
        {
            panini.distance.Override(Mathf.Lerp(initPaniniDist, targetPaniniDistance, t));
            panini.cropToFit.Override(Mathf.Lerp(initPaniniCrop, targetPaniniCropToFit, t));
        }

        if (colorAdj != null)
        {
            colorAdj.saturation.Override(Mathf.Lerp(initSaturation, targetSaturation, t));
            colorAdj.postExposure.Override(Mathf.Lerp(initPostExposure, targetPostExposure, t));
        }

        //  Update FMOD parameter
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Drunk", player != null ? player.Drunk : 0f);
    }

    private void CacheOverrides()
    {
        if (volume == null || volume.profile == null) return;
        var p = volume.profile;
        p.TryGet(out bloom);
        p.TryGet(out chroma);
        p.TryGet(out vignette);
        p.TryGet(out lens);
        p.TryGet(out grain);
        p.TryGet(out panini);
        p.TryGet(out colorAdj);
    }

    private void CaptureInitial()
    {
        if (bloom != null) initBloom = bloom.intensity.value;
        if (chroma != null) initChroma = chroma.intensity.value;
        if (vignette != null) { initVignette = vignette.intensity.value; initVignetteColor = vignette.color.value; }
        if (lens != null) initLens = lens.intensity.value;
        if (grain != null) initGrain = grain.intensity.value;
        if (panini != null) { initPaniniDist = panini.distance.value; initPaniniCrop = panini.cropToFit.value; }
        if (colorAdj != null) { initSaturation = colorAdj.saturation.value; initPostExposure = colorAdj.postExposure.value; }
    }

    private void RestoreInitial()
    {
        if (bloom != null) bloom.intensity.Override(initBloom);
        if (chroma != null) chroma.intensity.Override(initChroma);
        if (vignette != null) { vignette.intensity.Override(initVignette); vignette.color.Override(initVignetteColor); }
        if (lens != null) lens.intensity.Override(initLens);
        if (grain != null) grain.intensity.Override(initGrain);
        if (panini != null) { panini.distance.Override(initPaniniDist); panini.cropToFit.Override(initPaniniCrop); }
        if (colorAdj != null) { colorAdj.saturation.Override(initSaturation); colorAdj.postExposure.Override(initPostExposure); }
    }

    private static float EaseOutQuad(float x) => 1f - (1f - x) * (1f - x);
}

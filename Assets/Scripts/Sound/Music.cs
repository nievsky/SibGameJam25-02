using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class BandPerformanceManager : MonoBehaviour
{
    [Header("FMOD Events")]
    [Tooltip("Speaking")]
    public List<EventReference> speakingEvents;

    [Tooltip("Music")]
    public List<EventReference> musicEvents;

    private EventInstance currentEvent;

    void Start()
    {
        StartCoroutine(PerformanceLoop());
    }

    private IEnumerator PerformanceLoop()
    {
        while (true)
        {
            // --- SPEAKING PART ---
            if (speakingEvents.Count > 0)
            {
                var speakingEvent = speakingEvents[Random.Range(0, speakingEvents.Count)];
                yield return PlayAndWaitForEvent(speakingEvent);
            }

            // --- MUSIC PART ---
            if (musicEvents.Count > 0)
            {
                var musicEvent = musicEvents[Random.Range(0, musicEvents.Count)];
                yield return PlayAndWaitForEvent(musicEvent);
            }
        }
    }

    private IEnumerator PlayAndWaitForEvent(EventReference eventRef)
    {
        currentEvent = RuntimeManager.CreateInstance(eventRef);
        currentEvent.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        currentEvent.start();


        PLAYBACK_STATE state;
        do
        {
            currentEvent.getPlaybackState(out state);
            yield return null;
        } while (state == PLAYBACK_STATE.STARTING);


        bool isPlaying = true;
        while (isPlaying)
        {
            currentEvent.getPlaybackState(out state);
            if (state == PLAYBACK_STATE.STOPPED || state == PLAYBACK_STATE.STOPPING)
                isPlaying = false;
            yield return null;
        }

        currentEvent.release();


        yield return new WaitForSeconds(1f);
    }
}
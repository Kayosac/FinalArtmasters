using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AnimationSoundEvents : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource source;
    public AudioClip[] clips;

    [Header("Randomization")]
    public bool randomizePitch = true;
    public float pitchMin = 0.97f;
    public float pitchMax = 1.03f;

    void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    // Animation Event: no param (plays first clip)
    public void PlayOneShot()
    {
        if (clips == null || clips.Length == 0) return;
        InternalPlay(clips[0]);
    }

    // Animation Event: int param (index in clips)
    public void PlayOneShotByIndex(int index)
    {
        if (clips == null || index < 0 || index >= clips.Length) return;
        InternalPlay(clips[index]);
    }

    // Animation Event: AudioClip param (drag clip into event)
    public void PlayOneShotClip(AudioClip clip)
    {
        if (clip == null) return;
        InternalPlay(clip);
    }

    // Animation Event: random from array
    public void PlayRandom()
    {
        if (clips == null || clips.Length == 0) return;
        int i = Random.Range(0, clips.Length);
        InternalPlay(clips[i]);
    }

    private void InternalPlay(AudioClip clip)
    {
        if (!source || clip == null) return;
        float prevPitch = source.pitch;
        if (randomizePitch) source.pitch = Random.Range(pitchMin, pitchMax);
        source.PlayOneShot(clip);
        if (randomizePitch) source.pitch = prevPitch;
    }

    // optional stop (can be called from an event too)
    public void StopAll()
    {
        if (source) source.Stop();
    }
}

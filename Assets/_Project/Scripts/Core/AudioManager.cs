// AudioManager
// Plays background music and sound effects, and controls their volume through an
// Audio Mixer. The Settings menu calls SetMusicVolume / SetSoundVolume; other
// scripts call PlayMusic / PlaySfx.
//
// Put this on: the "Managers" GameObject (the prefab that lives in every scene).
// Assign in Inspector:
//   - Audio Mixer: the AudioMixer asset that has exposed parameters
//     named exactly "MusicVolume" and "SoundVolume".
//   - Music Source: an AudioSource set to Loop (for background music).
//   - Sfx Source: an AudioSource NOT set to loop (for one-shot sounds).

using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    [Tooltip("The AudioMixer asset. It must have exposed parameters called 'MusicVolume' and 'SoundVolume'.")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sources")]
    [Tooltip("AudioSource used for looping background music.")]
    [SerializeField] private AudioSource musicSource;

    [Tooltip("AudioSource used for short one-shot sound effects.")]
    [SerializeField] private AudioSource sfxSource;

    // Names of the exposed volume parameters set up inside the AudioMixer.
    private const string MusicVolumeParameter = "MusicVolume";
    private const string SoundVolumeParameter = "SoundVolume";

    // The quietest volume before we treat the slider as fully muted.
    private const float MinimumVolume = 0.0001f;

    protected override void Awake()
    {
        base.Awake();

        if (audioMixer == null)
        {
            Debug.LogError("AudioManager on 'Managers': Audio Mixer is not assigned. Drag your AudioMixer asset here.", this);
        }
        if (musicSource == null)
        {
            Debug.LogError("AudioManager on 'Managers': Music Source is not assigned. Drag a looping AudioSource here.", this);
        }
        if (sfxSource == null)
        {
            Debug.LogError("AudioManager on 'Managers': Sfx Source is not assigned. Drag a non-looping AudioSource here.", this);
        }
    }

    // Starts playing a background music clip on a loop.
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null)
        {
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // Plays a one-shot sound effect (for example an interact sound).
    public void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    // Sets music volume from a 0-1 slider value (0 = silent, 1 = full).
    public void SetMusicVolume(float normalizedVolume)
    {
        SetMixerVolume(MusicVolumeParameter, normalizedVolume);
    }

    // Sets sound-effect volume from a 0-1 slider value (0 = silent, 1 = full).
    public void SetSoundVolume(float normalizedVolume)
    {
        SetMixerVolume(SoundVolumeParameter, normalizedVolume);
    }

    // Converts a 0-1 slider value into decibels and applies it to the mixer.
    // Audio volume is measured in decibels, so we use a logarithm here.
    private void SetMixerVolume(string parameterName, float normalizedVolume)
    {
        if (audioMixer == null)
        {
            return;
        }

        float clamped = Mathf.Clamp(normalizedVolume, MinimumVolume, 1f);
        float decibels = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(parameterName, decibels);
    }
}

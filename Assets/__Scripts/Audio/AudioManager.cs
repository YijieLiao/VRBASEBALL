using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;

    private AudioSource bgmSource;
    private AudioSource sfxSource2D;

    private const string MasterVolParam = "Master";
    private const string MusicVolParam  = "Music";
    private const string SFXVolParam    = "SFX";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 如果 Inspector 没赋值 mixer，尝试从 Dark UI 加载
        if (mixer == null)
            mixer = Resources.Load<AudioMixer>("_Mixer");

        if (bgmSource == null)
            bgmSource = CreateSource("BGM", "Music");
        if (sfxSource2D == null)
            sfxSource2D = CreateSource("SFX2D", "SFX");
    }

    private AudioSource CreateSource(string goName, string mixerGroup)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 0f;       // 2D
        src.playOnAwake = false;

        if (mixer != null)
        {
            var groups = mixer.FindMatchingGroups(mixerGroup);
            if (groups.Length > 0)
                src.outputAudioMixerGroup = groups[0];
        }
        return src;
    }

    // ==================== BGM ====================

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        bgmSource.Stop();
        bgmSource.clip = null;
    }

    public AudioClip CurrentBGM => bgmSource.clip;

    // ==================== UI SFX (2D) ====================

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource2D.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;
        sfxSource2D.PlayOneShot(clip, volumeScale);
    }

    // ==================== 3D 空间音效 ====================

    public void PlaySFXAt(AudioClip clip, Vector3 position)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position);
    }

    public void PlaySFXAt(AudioClip clip, Vector3 position, float volumeScale)
    {
        if (clip == null) return;
        // PlayClipAtPoint 不支持 volume，用临时 GameObject 实现
        var go = new GameObject("SFX3D_Temp");
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.clip = clip;
        src.volume = volumeScale;

        if (mixer != null)
        {
            var groups = mixer.FindMatchingGroups("SFX");
            if (groups.Length > 0)
                src.outputAudioMixerGroup = groups[0];
        }

        src.Play();
        Destroy(go, clip.length + 0.1f);
    }

    // ==================== 音量 ====================

    public void SetMasterVolume(float normalized)
    {
        SetVolume(MasterVolParam, normalized);
    }

    public void SetMusicVolume(float normalized)
    {
        SetVolume(MusicVolParam, normalized);
    }

    public void SetSFXVolume(float normalized)
    {
        SetVolume(SFXVolParam, normalized);
    }

    public float GetMasterVolume()
    {
        return GetVolume(MasterVolParam);
    }

    public float GetMusicVolume()
    {
        return GetVolume(MusicVolParam);
    }

    public float GetSFXVolume()
    {
        return GetVolume(SFXVolParam);
    }

    private void SetVolume(string param, float normalized)
    {
        if (mixer == null) return;
        // 0..1 → -80..0 dB
        float clamped = Mathf.Clamp01(normalized);
        float dB = clamped <= 0.0001f ? -80f : Mathf.Log10(clamped) * 20f;
        mixer.SetFloat(param, dB);
    }

    private float GetVolume(string param)
    {
        if (mixer == null) return 1f;
        if (mixer.GetFloat(param, out float dB))
        {
            if (dB <= -80f) return 0f;
            return Mathf.Pow(10f, dB / 20f);
        }
        return 1f;
    }
}

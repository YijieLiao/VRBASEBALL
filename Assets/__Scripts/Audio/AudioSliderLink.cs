using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂到 SETTINGSCanvas 的每个 Slider 上，选好 Channel 即可接入音量系统。
/// </summary>
public class AudioSliderLink : MonoBehaviour
{
    public enum Channel { Master, Music, SFX, Ambient }

    [SerializeField] private Channel channel = Channel.Master;

    void Start()
    {
        var slider = GetComponent<UnityEngine.UI.XRSlider>();
        if (slider == null) return;

        // 初始化 slider 位置
        var am = AudioManager.Instance;
        if (am != null)
            slider.value = channel switch
            {
                Channel.Master  => am.GetMasterVolume(),
                Channel.Music   => am.GetMusicVolume(),
                Channel.SFX     => am.GetSFXVolume(),
                Channel.Ambient => am.GetAmbientVolume(),
                _               => 1f
            };

        // slider 值变化 → 写入 AudioManager
        slider.onValueChanged.AddListener(v =>
        {
            if (AudioManager.Instance == null) return;
            switch (channel)
            {
                case Channel.Master:  AudioManager.Instance.SetMasterVolume(v); break;
                case Channel.Music:   AudioManager.Instance.SetMusicVolume(v); break;
                case Channel.SFX:     AudioManager.Instance.SetSFXVolume(v); break;
                case Channel.Ambient: AudioManager.Instance.SetAmbientVolume(v); break;
            }
        });
    }
}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace Menu
{
    public class SettingsMenuScreen : BaseScreen
    {
        [Header("Music Volume")]
        [SerializeField] private Slider _sliderMusic;
        [SerializeField] private AudioMixer _audioMixerMusic;
        [Header("Sound Volume")]
        [SerializeField] private Slider _sliderSound;
        [SerializeField] private AudioMixer _audioMixerSound;
        [Header("Fullscreen")]
        [SerializeField] private Toggle _fullscreenToggle;
        [Header("Resolution")]
        [SerializeField] private TMP_Dropdown _resolutionsDropdown;

        private Resolution[] _resolutions;

        private void Start()
        {
            // music settings
            _sliderMusic.onValueChanged.AddListener(SliderMusic_OnValueChanged);
            _audioMixerMusic.GetFloat("volume", out float powerMusic);
            _sliderMusic.SetValueWithoutNotify((powerMusic + 80f) / 100f);
            
            // sounds settings
            _sliderSound.onValueChanged.AddListener(SliderSound_OnValueChanged);
            _audioMixerSound.GetFloat("volume", out float powerSound);
            _sliderSound.SetValueWithoutNotify((powerSound + 80f) / 100f);

            // fullscreen settings
            _fullscreenToggle.onValueChanged.AddListener(FullscreenToggle_OnValueChanged);
            _fullscreenToggle.isOn = Screen.fullScreen;

            // resolution settings
            _resolutions = Screen.resolutions;
            _resolutionsDropdown.ClearOptions();
            List<string> options = new List<string>();
            int currentResolutionIndex = 0;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                options.Add(_resolutions[i].width + "x" + _resolutions[i].height);
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = i;
                }
            }
            _resolutionsDropdown.AddOptions(options);
            _resolutionsDropdown.value = currentResolutionIndex;
            _resolutionsDropdown.RefreshShownValue();
            _resolutionsDropdown.onValueChanged.AddListener(ResolutionsDropdown_OnValueChanged);
        }

        private void SliderMusic_OnValueChanged(float power) => _audioMixerMusic.SetFloat("volume", power * 100 - 80);
        private void SliderSound_OnValueChanged(float power) => _audioMixerSound.SetFloat("volume", power * 100 - 80);
        private void FullscreenToggle_OnValueChanged(bool isEnabled) => Screen.fullScreen = isEnabled;
        
        private void ResolutionsDropdown_OnValueChanged(int resolutionIndex)
        {
            Resolution resolution = _resolutions[resolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
    }
}
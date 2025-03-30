using System;
using Progression;
using UnityEngine;

namespace Game
{
    public class MusicSetter : MonoBehaviour
    {
        [SerializeField] private AudioClip _themeMusic;

        private void Start()
        {
            AudioController.Instance.SetThemeMusic(_themeMusic);
        }
    }
}
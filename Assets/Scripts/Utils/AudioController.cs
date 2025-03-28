using UnityEngine;

namespace Progression
{
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _soundSource;

        public static AudioController Instance { get; set; }

        private AudioController() { }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void PlaySound(AudioClip clip)
        {
            _soundSource.PlayOneShot(clip);
        }
    }
}
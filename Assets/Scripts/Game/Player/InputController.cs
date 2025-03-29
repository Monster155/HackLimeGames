using UnityEngine;

namespace Game.Player
{
    public class InputController : MonoBehaviour
    {

        #region Singleton

        public static InputController Instance { get; set; }

        private InputController() { }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.Log("There is more than one InputController - removing this one");
                Destroy(gameObject);
            }
        }

        #endregion
        
        public float Horizontal { get; private set; }
        public float Vertical { get; private set; }
        public float MouseX { get; private set; }
        public float MouseY { get; private set; }

        void Update()
        {
            Horizontal = Input.GetAxis("Horizontal");
            Vertical = Input.GetAxis("Vertical");
            MouseX = Input.GetAxis("Mouse X");
            MouseY = Input.GetAxis("Mouse Y");
        }
    }
}

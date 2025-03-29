using System;
using UnityEngine;

namespace Game.Player
{
    public class ColliderEventReceiver : MonoBehaviour
    {
        public event Action<Collision> OnCollisionEnterReceive;
        public event Action<Collision> OnCollisionExitReceive;
        public event Action<Collider> OnTriggerEnterReceive;
        public event Action<Collider> OnTriggerExitReceive;

        private void OnCollisionEnter(Collision other) => OnCollisionEnterReceive?.Invoke(other);
        private void OnCollisionExit(Collision other) => OnCollisionExitReceive?.Invoke(other);
        private void OnTriggerEnter(Collider other) => OnTriggerEnterReceive?.Invoke(other);
        private void OnTriggerExit(Collider other) => OnTriggerExitReceive?.Invoke(other);
    }
}
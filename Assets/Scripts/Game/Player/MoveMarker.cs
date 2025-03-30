using UnityEngine;

namespace Game.Player
{
    public class MoveMarker : MonoBehaviour
    {
        public void UpdateContent(Vector3 pos)
        {
            transform.position = pos;
        }
    }
}
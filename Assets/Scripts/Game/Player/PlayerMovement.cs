using System.Collections;
using System.Collections.Generic;
using Game.Utils;
using UnityEngine;
using Utils;

namespace Game.Player
{

    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _stepTime = 1f;
        [SerializeField] private float _stepDelayTime = 0.2f;
        [SerializeField] private ColliderEventReceiver _startSBS;
        [SerializeField] private ColliderEventReceiver _endSBS;

        private float TotalStepTime => _stepTime + _stepDelayTime;

        private float _stepDelayTimer;
        private List<Collider> _enemiesAround = new();

        private void Start()
        {
            _stepDelayTimer = TotalStepTime;

            _startSBS.OnTriggerEnterReceive += StartSBS_OnTriggerEnterReceive;
            _endSBS.OnTriggerExitReceive += EndSBS_OnTriggerExitReceive;
        }

        private void Update()
        {
            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= TotalStepTime)
            {
                Move();
            }
        }

        private void Move()
        {
            float h = InputController.Instance.Horizontal;
            float v = InputController.Instance.Vertical;

            if (h != 0 || v != 0)
            {
                int x = h switch
                {
                    > 0 => 1,
                    < 0 => -1,
                    _ => 0
                };

                int y = v switch
                {
                    > 0 => 1,
                    < 0 => -1,
                    _ => 0
                };

                Vector3 forward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
                Vector3 right = new Vector3(transform.right.x, 0, transform.right.z).normalized;

                _animator.SetTrigger(AnimHashes.MoveHash);
                _stepDelayTimer = 0;
                StartCoroutine(MoveCoroutine(right * x + forward * y));
            }
        }

        private IEnumerator MoveCoroutine(Vector3 moveDirection)
        {
            EventSystem.OnPlayerStartsMoving?.Invoke();

            float stepTimer = 0;
            Vector3 start = transform.position;
            Vector3 end = transform.position + moveDirection;
            while (stepTimer < _stepTime)
            {
                transform.position = Vector3.Lerp(start, end, stepTimer);
                stepTimer += Time.deltaTime;
                yield return null;
            }
            transform.position = end;

            EventSystem.OnPlayerFinishedMoving?.Invoke();
        }

        private void StartSBS_OnTriggerEnterReceive(Collider other)
        {
            if (!other.tag.Equals("Enemy"))
                return;

            _enemiesAround.Add(other);

            GlobalGameSettings.IsStepByStepMovement = true;
        }

        private void EndSBS_OnTriggerExitReceive(Collider other)
        {
            if (!other.tag.Equals("Enemy"))
                return;

            _enemiesAround.Remove(other);

            if (_enemiesAround.Count == 0)
                GlobalGameSettings.IsStepByStepMovement = false;
        }
    }
}
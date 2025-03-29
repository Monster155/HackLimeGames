using System;
using System.Collections;
using Game.Player;
using Game.Utils;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Game.Enemies
{
    public class EnemyMovement : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _stepTime = 1f;
        [SerializeField] private float _stepDelayTime = 0.2f;

        private float TotalStepTime => _stepTime + _stepDelayTime;

        private float _stepDelayTimer;

        private void Start()
        {
            EventSystem.OnPlayerStartsMoving += EventSystem_OnPlayerStartsMoving;
            _stepDelayTimer = TotalStepTime;
        }

        private void Update()
        {
            if (GlobalGameSettings.IsStepByStepMovement)
                return;

            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= TotalStepTime)
            {
                Move();
            }
        }

        private void EventSystem_OnPlayerStartsMoving()
        {
            if (GlobalGameSettings.IsStepByStepMovement)
                Move();
        }

        private void Move()
        {
            int h = Random.Range(-1, 2);
            int v = Random.Range(-1, 2);

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
        }
    }
}
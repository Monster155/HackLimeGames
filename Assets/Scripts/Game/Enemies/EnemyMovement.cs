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
        [SerializeField] private int _hp = 3;
        [SerializeField] private int _damage = 1;

        private bool CanMove => _hp > 0;
        public float TotalStepTime => _stepTime + _stepDelayTime;

        private float _stepDelayTimer;

        private void Start()
        {
            _stepDelayTimer = TotalStepTime;
        }

        private void Update()
        {
            if (!CanMove)
                return;

            if (GlobalGameSettings.IsStepByStepMovement)
                return;

            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= TotalStepTime)
            {
                if (GetMoveDirection(out Vector3 moveDirection))
                {
                    _stepDelayTimer = 0;
                    _animator.transform.rotation = Quaternion.LookRotation(moveDirection);
                    Move(moveDirection);
                }
            }
        }

        public void DoStep(PlayerMovement player)
        {
            if (!CanMove)
                return;

            if (GetMoveDirection(out Vector3 moveDirection))
            {
                _animator.transform.rotation = Quaternion.LookRotation(moveDirection);
                
                if (Vector3.Distance(player.transform.position, transform.position + moveDirection) < 0.8f)
                    Attack(moveDirection, player);
                else
                    Move(moveDirection);
            }
        }

        private bool GetMoveDirection(out Vector3 moveDirection)
        {
            moveDirection = Vector3.zero;

            int h = Random.Range(-1, 2);
            int v = Random.Range(-1, 2);

            if (h == 0 && v == 0)
                return false;

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

            moveDirection = right * x + forward * y;
            return true;
        }

        private void Attack(Vector3 moveDirection, PlayerMovement player)
        {
            StartCoroutine(AttackCoroutine(moveDirection, player));
        }

        private void Move(Vector3 moveDirection)
        {
            _animator.SetTrigger(AnimHashes.MoveHash);
            StartCoroutine(MoveCoroutine(moveDirection));
        }

        private IEnumerator AttackCoroutine(Vector3 moveDirection, PlayerMovement player)
        {
            if (player.IsAliveAfterDamage(_damage))
            {
                _animator.SetTrigger(AnimHashes.AttackHash);

                float stepTimer = 0;
                Vector3 start = transform.position;
                Vector3 end = transform.position + moveDirection;
                while (stepTimer < _stepTime / 2)
                {
                    transform.position = Vector3.Lerp(start, end, stepTimer / (_stepTime / 2));
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = end;

                player.GetDamage(_damage);

                stepTimer = 0;
                while (stepTimer < _stepTime / 2)
                {
                    transform.position = Vector3.Lerp(end, start, stepTimer / (_stepTime / 2));
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = start;
            }
            else
            {
                _animator.SetTrigger(AnimHashes.FatalAttackHash);

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

                player.GetDamage(_damage);
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

        public bool IsAliveAfterDamage(int damage)
        {
            return _hp - damage > 0;
        }

        public void GetDamage(int damage)
        {
            _hp -= damage;
            if (_hp <= 0)
                _animator.SetTrigger(AnimHashes.DeadHash);
        }
    }
}
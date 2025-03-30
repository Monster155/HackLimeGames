using System;
using System.Collections;
using System.Linq;
using Game.Player;
using Game.Utils;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace Game.Enemies
{
    public class EnemyMovement : MonoBehaviour
    {
        // TODO combine Player and Enemy movements due to same code

        private readonly Vector2Int[] HorAndVerVectors = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private readonly Vector2Int[] DiaglonalVectors = { new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1) };
        private readonly Vector2Int[] HorseVectors =
        {
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1), new Vector2Int(1, 2), new Vector2Int(-1, 2), new Vector2Int(1, -2),
            new Vector2Int(-1, -2)
        };

        [SerializeField] private Animator _animator;
        [SerializeField] private MovementType _movementType = MovementType.AsPawn;
        [SerializeField] private int _hp = 3;
        [SerializeField] private int _damage = 1;
        [SerializeField] private Collider _collider;
        [SerializeField] private int _chanceToAfk = 10;

        private bool CanMove => _hp > 0;

        private float _stepDelayTimer;
        private Vector2Int[] _directionVectors;

        private void Start()
        {
            _stepDelayTimer = GlobalGameSettings.TotalStepTime;
            _directionVectors = GetMoveDirection();
        }

        private void Update()
        {
            if (!CanMove)
                return;

            if (GlobalGameSettings.IsStepByStepMovement)
                return;

            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= GlobalGameSettings.TotalStepTime)
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
            Vector2Int selectedVector = _directionVectors[Random.Range(0, _directionVectors.Length)];

            if (Random.Range(0f, 100f) < _chanceToAfk)
            {
                moveDirection = Vector3.zero;
                return false;
            }

            int x = Random.Range(1, selectedVector.x + 1);
            int y = Random.Range(1, selectedVector.y + 1);

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

        private Vector2Int[] GetMoveDirection()
        {
            return _movementType switch
            {
                MovementType.AsPawn => HorAndVerVectors,
                MovementType.HorAndVer => HorAndVerVectors.Select(v => v * GlobalGameSettings.EnemyMaxMoveDistance).ToArray(),
                MovementType.Diagonal => DiaglonalVectors.Select(v => v * GlobalGameSettings.EnemyMaxMoveDistance).ToArray(),
                MovementType.AllSide => HorAndVerVectors.Concat(DiaglonalVectors).Select(v => v * GlobalGameSettings.EnemyMaxMoveDistance).ToArray(),
                MovementType.Horse => HorseVectors,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private IEnumerator AttackCoroutine(Vector3 moveDirection, PlayerMovement player)
        {
            if (player.IsAliveAfterDamage(_damage))
            {
                _animator.SetTrigger(AnimHashes.AttackHash);

                float stepTimer = 0;
                Vector3 start = transform.position;
                Vector3 end = transform.position + moveDirection;
                while (stepTimer < GlobalGameSettings.StepTime / 2)
                {
                    transform.position = Vector3.Lerp(start, end, stepTimer / (GlobalGameSettings.StepTime / 2));
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = end;

                player.GetDamage(_damage);

                stepTimer = 0;
                while (stepTimer < GlobalGameSettings.StepTime / 2)
                {
                    transform.position = Vector3.Lerp(end, start, stepTimer / (GlobalGameSettings.StepTime / 2));
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
                while (stepTimer < GlobalGameSettings.StepTime)
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
            while (stepTimer < GlobalGameSettings.StepTime)
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
            {
                _animator.SetTrigger(AnimHashes.DeadHash);
                enabled = false;
                _collider.enabled = false;
            }
        }
    }
}
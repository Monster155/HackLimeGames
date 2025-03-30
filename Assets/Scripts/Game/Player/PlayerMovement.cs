using System;
using System.Collections;
using System.Collections.Generic;
using Game.Enemies;
using Game.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Game.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        public event Action<StepType> OnStepStart;
        public event Action<StepType> OnStepEnd;

        private readonly Vector3[] HorAndVerVectors = { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };
        private readonly Vector3[] DiaglonalVectors = { new Vector3(1, 0, 1), new Vector3(-1, 0, 1), new Vector3(1, 0, -1), new Vector3(-1, 0, -1) };
        private readonly Vector3[] HorseVectors =
        {
            new Vector3(2, 0, 1), new Vector3(2, 0, -1), new Vector3(-2, 0, 1), new Vector3(-2, 0, -1), new Vector3(1, 0, 2), new Vector3(-1, 0, 2), new Vector3(1, 0, -2),
            new Vector3(-1, 0, -2)
        };

        [SerializeField] private Animator _animator;
        [SerializeField] private float _stepTime = 1f;
        [SerializeField] private float _stepDelayTime = 0.2f;
        [SerializeField] private ColliderEventReceiver _startSBS;
        [SerializeField] private ColliderEventReceiver _endSBS;
        [SerializeField] private Transform _cameraArmTransform;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private Vector2 _verticalAngleLimits = new Vector2(-5f, 80f);
        [SerializeField] private MovementType _movementType = MovementType.AsPawn;
        [SerializeField] private Transform _moveMarker;
        [SerializeField] private ColliderEventReceiver _enemyDetector;
        [SerializeField] private int _hp = 5;
        [SerializeField] private int _damage = 1;

        public bool CanMove = true; // sets from StepManager
        private float TotalStepTime => _stepTime + _stepDelayTime;

        private Vector3 _verticalRotation = Vector3.zero;

        private float _stepDelayTimer;
        private List<Collider> _enemiesAround = new();

        private int _moveDistanceMultiplier = 1;
        private bool _isMoving = false;
        private List<Collider> _enemiesInMoveZone = new();
        private List<Collider> _obstaclesInMoveZone = new();

        private void Start()
        {
            _stepDelayTimer = TotalStepTime;

            _startSBS.OnTriggerEnterReceive += StartSBS_OnTriggerEnterReceive;
            _endSBS.OnTriggerExitReceive += EndSBS_OnTriggerExitReceive;
            _enemyDetector.OnTriggerEnterReceive += EnemyDetector_OnTriggerEnterReceive;
            _enemyDetector.OnTriggerExitReceive += EnemyDetector_OnTriggerExitReceive;

            _verticalRotation = _cameraArmTransform.localRotation.eulerAngles;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= TotalStepTime)
            {
                Move();
            }

            RotateCamera();
            UpdateMoveMarker();
        }

        private void Move()
        {
            if (!CanMove)
                return;

            float v = InputController.Instance.Vertical;

            if (v > 0)
            {
                _stepDelayTimer = 0;
                StartCoroutine(MoveCoroutine(GetMoveDirection() * _moveDistanceMultiplier));
            }
        }

        private void RotateCamera()
        {
            float mouseX = InputController.Instance.MouseX * _mouseSensitivity;
            float mouseY = InputController.Instance.MouseY * _mouseSensitivity;

            _verticalRotation.y += mouseX;
            _verticalRotation.y %= 360;

            _verticalRotation.x -= mouseY;
            _verticalRotation.x = Mathf.Clamp(_verticalRotation.x, _verticalAngleLimits.x, _verticalAngleLimits.y);

            _cameraArmTransform.localRotation = Quaternion.Euler(_verticalRotation);
        }

        private void UpdateMoveMarker()
        {
            if (_isMoving)
                return;

            _moveMarker.localPosition = transform.position + GetMoveDirection();
            _enemyDetector.transform.position = _moveMarker.position;
        }

        private IEnumerator MoveCoroutine(Vector3 moveDirection)
        {
            _isMoving = true;
            _animator.transform.rotation = Quaternion.LookRotation(moveDirection);

            StepType stepType;

            if (_obstaclesInMoveZone.Count > 0)
                stepType = StepType.MoveBlocked;
            else if (_enemiesInMoveZone.Count > 0)
                stepType = StepType.Attack;
            else
                stepType = StepType.Move;

            OnStepStart?.Invoke(stepType);

            yield return stepType switch
            {
                StepType.Move => MoveAllowedCoroutine(moveDirection),
                StepType.Attack => AttackCoroutine(moveDirection),
                StepType.MoveBlocked => MoveBlockedCoroutine(moveDirection),
                _ => throw new ArgumentOutOfRangeException()
            };

            OnStepEnd?.Invoke(stepType);
            _isMoving = false;
        }

        private Vector3 GetMoveDirection()
        {
            MovementType movementType = _movementType;
            return movementType switch
            {
                MovementType.AsPawn => HaVMove(),
                MovementType.HorAndVer => HaVMove(),
                MovementType.Diagonal => DiagMove(),
                MovementType.AllSide => AllSideMove(),
                MovementType.Horse => HorseMove(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        #region MovingVariantsMethods

        private Vector3 HorseMove()
        {
            Vector3 closestVector = HorseVectors[0];
            foreach (Vector3 v in HorseVectors)
            {
                float sqrDistanceToA = (_cameraArmTransform.forward - v).sqrMagnitude;
                float sqrDistanceToB = (_cameraArmTransform.forward - closestVector).sqrMagnitude;

                if (sqrDistanceToA < sqrDistanceToB)
                    closestVector = v;
            }
            return closestVector;
        }

        private Vector3 HaVMove()
        {
            Vector3 closestVector = HorAndVerVectors[0];
            foreach (Vector3 v in HorAndVerVectors)
            {
                float sqrDistanceToA = (_cameraArmTransform.forward - v).sqrMagnitude;
                float sqrDistanceToB = (_cameraArmTransform.forward - closestVector).sqrMagnitude;

                if (sqrDistanceToA < sqrDistanceToB)
                    closestVector = v;
            }
            return closestVector;
        }

        private Vector3 DiagMove()
        {
            Vector3 closestVector = DiaglonalVectors[0];
            foreach (Vector3 v in DiaglonalVectors)
            {
                float sqrDistanceToA = (_cameraArmTransform.forward - v).sqrMagnitude;
                float sqrDistanceToB = (_cameraArmTransform.forward - closestVector).sqrMagnitude;

                if (sqrDistanceToA < sqrDistanceToB)
                    closestVector = v;
            }
            return closestVector;
        }

        private Vector3 AllSideMove()
        {
            Vector3 closestHaVVector = HaVMove();
            Vector3 closestDiagVector = DiagMove();

            float distToHaV = (_cameraArmTransform.forward - closestHaVVector).sqrMagnitude;
            float distToDiag = (_cameraArmTransform.forward - closestDiagVector.normalized).sqrMagnitude;

            return distToHaV < distToDiag ? closestHaVVector : closestDiagVector;
        }

        #endregion

        public bool IsAliveAfterDamage(int damage)
        {
            return _hp - damage > 0;
        }

        public void GetDamage(int damage)
        {
            _hp -= damage;
            if (_hp <= 0)
                StartCoroutine(DeadCoroutine());
        }

        private IEnumerator DeadCoroutine()
        {
            _animator.SetTrigger(AnimHashes.DeadHash);
            // do fade screen with GAME OVER text
            yield return new WaitForSeconds(2f);

            SceneManager.LoadScene("GameScene");
        }

        private IEnumerator AttackCoroutine(Vector3 moveDirection)
        {
            EnemyMovement enemyHP = _enemiesInMoveZone[0].GetComponentInParent<EnemyMovement>();

            if (enemyHP.IsAliveAfterDamage(_damage))
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

                enemyHP.GetDamage(_damage);

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
                    transform.position = Vector3.Lerp(start, end, stepTimer / _stepTime);
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = end;

                enemyHP.GetDamage(_damage);
            }
        }

        private IEnumerator MoveBlockedCoroutine(Vector3 moveDirection)
        {
            _animator.SetTrigger(AnimHashes.MoveHash);

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

            stepTimer = 0;
            while (stepTimer < _stepTime / 2)
            {
                transform.position = Vector3.Lerp(end, start, stepTimer / (_stepTime / 2));
                stepTimer += Time.deltaTime;
                yield return null;
            }
            transform.position = start;
        }

        private IEnumerator MoveAllowedCoroutine(Vector3 moveDirection)
        {
            _animator.SetTrigger(AnimHashes.MoveHash);

            float stepTimer = 0;
            Vector3 start = transform.position;
            Vector3 end = transform.position + moveDirection;
            while (stepTimer < _stepTime)
            {
                transform.position = Vector3.Lerp(start, end, stepTimer / _stepTime);
                stepTimer += Time.deltaTime;
                yield return null;
            }
            transform.position = end;
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

        private void EnemyDetector_OnTriggerEnterReceive(Collider other)
        {
            switch (other.tag)
            {
                case "Enemy":
                    _enemiesInMoveZone.Add(other);
                    break;
                case "Obstacle":
                    _obstaclesInMoveZone.Add(other);
                    break;
            }
        }

        private void EnemyDetector_OnTriggerExitReceive(Collider other)
        {
            switch (other.tag)
            {
                case "Enemy":
                    _enemiesInMoveZone.Remove(other);
                    break;
                case "Obstacle":
                    _obstaclesInMoveZone.Remove(other);
                    break;
            }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private ColliderEventReceiver _startSBS;
        [SerializeField] private ColliderEventReceiver _endSBS;
        [SerializeField] private Transform _cameraArmTransform;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private Vector2 _verticalAngleLimits = new Vector2(-5f, 80f);
        [SerializeField] private MoveMarker _moveMarker;
        [SerializeField] private Transform _obstaclesDetectorsContainer;
        [SerializeField] private ColliderEventReceiver _obstaclesDetectorPrefab;
        [SerializeField] private ColliderEventReceiver _enemyDetector;
        [SerializeField] private int _hp = 5;
        [SerializeField] private int _damage = 1;

        public int MoveDistanceMultiplier => _movementType is MovementType.AsPawn or MovementType.Horse ? 1 : _currentMoveDistanceMultiplierValue;

        private MovementType _movementType;

        private bool _isMoving = false;
        private int _currentMoveDistanceMultiplierValue = 1;

        private Vector3 _verticalRotation = Vector3.zero;

        private List<Collider> _enemiesAround = new();
        private float _stepDelayTimer;

        private List<Collider> _enemiesInMoveZone = new();
        private List<Collider> _obstaclesInMoveZone = new();

        private List<ColliderEventReceiver> _obstaclesDetectors = new();

        private void Start()
        {
            _stepDelayTimer = GlobalGameSettings.TotalStepTime;

            _startSBS.OnTriggerEnterReceive += StartSBS_OnTriggerEnterReceive;
            _endSBS.OnTriggerExitReceive += EndSBS_OnTriggerExitReceive;

            for (int i = 0; i < GlobalGameSettings.PlayerMaxMoveDistance; i++)
            {
                ColliderEventReceiver detector = Instantiate(_obstaclesDetectorPrefab, _obstaclesDetectorsContainer);
                _obstaclesDetectors.Add(detector);

                detector.OnTriggerEnterReceive += ObstaclesDetector_OnTriggerEnterReceive;
                detector.OnTriggerExitReceive += ObstaclesDetector_OnTriggerExitReceive;
            }

            _enemyDetector.OnTriggerEnterReceive += EnemyDetector_OnTriggerEnterReceive;
            _enemyDetector.OnTriggerExitReceive += EnemyDetector_OnTriggerExitReceive;

            _verticalRotation = _cameraArmTransform.localRotation.eulerAngles;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            _movementType = MovementType.AsPawn;
        }

        private void Update()
        {
            UpdateMoveDistanceMultiplier();
            RotateCamera();
            UpdateMoveMarker();

            _stepDelayTimer += Time.deltaTime;
            if (_stepDelayTimer >= GlobalGameSettings.TotalStepTime)
            {
                Move();
            }
        }

        private void Move()
        {
            if (_isMoving)
                return;

            float v = InputController.Instance.Vertical;

            if (v > 0)
            {
                _stepDelayTimer = 0;
                StartCoroutine(MoveCoroutine(GetMoveDirection() * MoveDistanceMultiplier));
            }
        }

        private void UpdateMoveDistanceMultiplier()
        {
            float scroll = InputController.Instance.MouseScroll;
            _currentMoveDistanceMultiplierValue = scroll switch
            {
                > 0 => Mathf.Min(_currentMoveDistanceMultiplierValue + 1, GlobalGameSettings.PlayerMaxMoveDistance),
                < 0 => Mathf.Max(_currentMoveDistanceMultiplierValue - 1, 1),
                _ => _currentMoveDistanceMultiplierValue
            };
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

            Vector3 moveDirection = GetMoveDirection();

            Vector3 newPos = transform.position + moveDirection * MoveDistanceMultiplier;
            _moveMarker.UpdateContent(newPos);

            _enemyDetector.transform.position = newPos;

            for (int i = 0; i < MoveDistanceMultiplier; i++)
                _obstaclesDetectors[i].transform.position = transform.position + moveDirection * (i + 1);
            for (int i = MoveDistanceMultiplier; i < _obstaclesDetectors.Count; i++)
                _obstaclesDetectors[i].transform.position = transform.position;
        }

        private IEnumerator MoveCoroutine(Vector3 moveDirection)
        {
            _isMoving = true;
            _animator.transform.rotation = Quaternion.LookRotation(moveDirection);

            StepType stepType;

            _enemiesInMoveZone = _enemiesInMoveZone.Where(c => c.GetComponentInParent<EnemyMovement>() != null).ToList();

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

            yield return new WaitForSeconds(GlobalGameSettings.TotalStepTime);

            _isMoving = false;
        }

        private MovementType GetNextMovementType(MovementType movementType)
        {
            //Пешка - Ладья (Замок) - Конь - Слон - Королева
            switch (movementType)
            {
                case MovementType.AsPawn:
                    return MovementType.HorAndVer;
                case MovementType.HorAndVer:
                    return MovementType.Horse;
                case MovementType.Diagonal:
                    return MovementType.AllSide;
                case MovementType.AllSide:
                    return MovementType.AllSide;
                case MovementType.Horse:
                    return MovementType.Diagonal;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 GetMoveDirection()
        {
            Vector3 moveDirection = _movementType switch
            {
                MovementType.AsPawn => HaVMove(),
                MovementType.HorAndVer => HaVMove(),
                MovementType.Diagonal => DiagMove(),
                MovementType.AllSide => AllSideMove(),
                MovementType.Horse => HorseMove(),
                _ => throw new ArgumentOutOfRangeException()
            };
            return moveDirection;
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
            _isMoving = true;
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
                while (stepTimer < GlobalGameSettings.StepTime / 2)
                {
                    transform.position = Vector3.Lerp(start, end, stepTimer / (GlobalGameSettings.StepTime / 2));
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = end;

                enemyHP.GetDamage(_damage);

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
                    transform.position = Vector3.Lerp(start, end, stepTimer / GlobalGameSettings.StepTime);
                    stepTimer += Time.deltaTime;
                    yield return null;
                }
                transform.position = end;

                enemyHP.GetDamage(_damage);
                _movementType = GetNextMovementType(_movementType);
            }
        }

        private IEnumerator MoveBlockedCoroutine(Vector3 moveDirection)
        {
            _animator.SetTrigger(AnimHashes.MoveHash);

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

            stepTimer = 0;
            while (stepTimer < GlobalGameSettings.StepTime / 2)
            {
                transform.position = Vector3.Lerp(end, start, stepTimer / (GlobalGameSettings.StepTime / 2));
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
            while (stepTimer < GlobalGameSettings.StepTime)
            {
                transform.position = Vector3.Lerp(start, end, stepTimer / GlobalGameSettings.StepTime);
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
            if (other.tag.Equals("Enemy"))
                _enemiesInMoveZone.Add(other);
        }

        private void EnemyDetector_OnTriggerExitReceive(Collider other)
        {
            if (other.tag.Equals("Enemy"))
                _enemiesInMoveZone.Remove(other);
        }

        private void ObstaclesDetector_OnTriggerEnterReceive(Collider other)
        {
            if (other.tag.Equals("Obstacle"))
                _obstaclesInMoveZone.Add(other);
        }

        private void ObstaclesDetector_OnTriggerExitReceive(Collider other)
        {
            if (other.tag.Equals("Obstacle"))
                _obstaclesInMoveZone.Remove(other);
        }
    }
}
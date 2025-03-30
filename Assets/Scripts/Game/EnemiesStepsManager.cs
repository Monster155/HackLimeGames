using System.Collections;
using Game.Enemies;
using Game.Player;
using Game.Utils;
using UnityEngine;

namespace Game
{
    public class EnemiesStepsManager : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _player;
        private EnemyMovement[] _enemies;

        private void Start()
        {
            _enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
            
            _player.OnStepStart += Payer_OnStepStart;
            _player.OnStepEnd += Payer_OnStepEnd;
        }

        private void Payer_OnStepStart(StepType stepType) { }

        private void Payer_OnStepEnd(StepType stepType)
        {
            if (GlobalGameSettings.IsStepByStepMovement)
            {
                foreach (EnemyMovement enemy in _enemies)
                    enemy.DoStep(_player);
            }
        }
    }
}
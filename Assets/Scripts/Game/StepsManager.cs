using System;
using System.Collections;
using Game.Enemies;
using Game.Player;
using Game.Utils;
using UnityEngine;

namespace Game
{
    public class StepsManager : MonoBehaviour
    {
        [SerializeField] private PlayerMovement _player;
        [SerializeField] private EnemyMovement[] _enemies;

        private void Start()
        {
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

                StartCoroutine(WaitForEnemyMove());
            }
        }
        
        private IEnumerator WaitForEnemyMove()
        {
            _player.CanMove = false;
            yield return new WaitForSeconds(_enemies[0].TotalStepTime);
            _player.CanMove = true;
        }
    }
}
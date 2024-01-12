using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Project
{
    public class RandomSpawnPointStrategy : ISpawnPointStrategy
    {
        List<Transform> _unusedSpawnPoints;
        Transform[] _spawnPoints;

        public RandomSpawnPointStrategy(Transform[] spawnPoints)
        {
            _spawnPoints = spawnPoints;
            _unusedSpawnPoints = new List<Transform>(spawnPoints);
        }

        public Transform NextSpawnPoint()
        {
            if (!_unusedSpawnPoints.Any())
            {
                _unusedSpawnPoints = new List<Transform>(_spawnPoints);
            }

            var randomIndex = Random.Range(0, _spawnPoints.Length);
            Transform result = _unusedSpawnPoints[randomIndex];
            _unusedSpawnPoints.RemoveAt(randomIndex);
            return result;
        }
    }

}

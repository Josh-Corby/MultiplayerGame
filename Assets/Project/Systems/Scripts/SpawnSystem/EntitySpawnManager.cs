using UnityEngine;

namespace Project
{
    public abstract class EntitySpawnManager : MonoBehaviour
    {
        [SerializeField] protected SpawnPointStrategyType _spawnPointStrategyType = SpawnPointStrategyType.Linear;
        [SerializeField] protected Transform[] _spawnPoints;

        protected ISpawnPointStrategy _spawnPointStrategy;
        protected enum SpawnPointStrategyType
        {
            Linear,
            Random
        }

        protected virtual void Awake()
        {
            // expression body switch statement
            _spawnPointStrategy = _spawnPointStrategyType switch
            {
                SpawnPointStrategyType.Linear => new LinearSpawnPointStrategy(_spawnPoints),
                SpawnPointStrategyType.Random => new RandomSpawnPointStrategy(_spawnPoints),
                _ => _spawnPointStrategy // default case
            };
        }

        public abstract void Spawn();
    }
}

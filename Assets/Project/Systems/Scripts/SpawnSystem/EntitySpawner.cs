namespace Project
{
    public class EntitySpawner<T> where T : Entity
    {
        private IEntityFactory<T> _entityFactory;
        private ISpawnPointStrategy _spawnPointStrategy;

        public EntitySpawner(IEntityFactory<T> entityFactory, ISpawnPointStrategy spawnPointStrategy)
        {
            _entityFactory = entityFactory;
            _spawnPointStrategy = spawnPointStrategy;
        }

        public T Spawn()
        {
            return _entityFactory.Create(_spawnPointStrategy.NextSpawnPoint());
        }
    }
}

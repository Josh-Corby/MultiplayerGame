using UnityEngine;

namespace Project
{
    public class EntityFactory<T> : IEntityFactory<T> where T : Entity
    {
        private EntityData[] _data;

        public EntityFactory(EntityData[] data)
        {
            _data = data;
        }

        public T Create(Transform spawnPoint)
        {
            EntityData entityData = _data[Random.Range(0, _data.Length)];
            GameObject instance = GameObject.Instantiate(entityData.prefab, spawnPoint.position, spawnPoint.rotation);
            return instance.GetComponent<T>();
        }
    }
}

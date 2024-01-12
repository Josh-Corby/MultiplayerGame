using UnityEngine;

namespace Project
{
    public interface ISpawnPointStrategy
    {
        Transform NextSpawnPoint();
    }
}

using UnityEngine;

namespace Project
{
    [CreateAssetMenu(menuName = "Player/Collectible Data", fileName = "CollectibleData")]
    public class CollectibleData : EntityData
    {
        public int score;
        // additional properties specific to collectibles
    }
}

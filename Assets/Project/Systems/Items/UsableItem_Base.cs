using Unity.Netcode;

namespace Project
{
    public abstract class UsableItem_Base : NetworkBehaviour
    {
        public string InteractionPrompt => throw new System.NotImplementedException();

        public abstract void Use();
    }
}

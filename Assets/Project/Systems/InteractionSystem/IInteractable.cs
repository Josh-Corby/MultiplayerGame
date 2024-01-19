using Unity.Netcode;

public interface IInteractable
{
    public string InteractionPrompt { get; }

    public void Interact(Interactor interactor);
}

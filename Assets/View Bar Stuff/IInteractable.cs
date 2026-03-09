public interface IInteractable
{
    void OnInteract();
    UnityEngine.Transform transform { get; }
}
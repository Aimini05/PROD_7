using UnityEngine;

namespace AmazingMaze.Player
{
    // Base class for objects the player can use with F.
    // Future chest/key scripts can inherit this and implement their own interaction.
    public abstract class Interactable : MonoBehaviour
    {
        // Return true only when an interaction actually happens.
        // The player is supplied for future inventory or permission checks.
        public abstract bool Interact(GridPlayerController player);
    }
}

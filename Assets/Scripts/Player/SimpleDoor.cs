using System;
using UnityEngine;

namespace AmazingMaze.Player
{
    [DisallowMultipleComponent]
    public sealed class SimpleDoor : Interactable
    {
        public bool IsOpen { get; private set; }
        // A sound script can subscribe here to announce that the passage is open.
        public event Action Opened;

        public override bool Interact(GridPlayerController player)
        {
            if (IsOpen) return false;
            Open();
            return true;
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            // Prototype opening: remove the panel and its obstruction, with no key check.
            foreach (var panel in GetComponentsInChildren<Renderer>()) panel.enabled = false;
            foreach (var obstacle in GetComponentsInChildren<Collider>()) obstacle.enabled = false;
            Physics.SyncTransforms();
            Opened?.Invoke();
        }
    }
}

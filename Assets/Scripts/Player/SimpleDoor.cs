using System;
using UnityEngine;

namespace AmazingMaze.Player
{
    [DisallowMultipleComponent]
    public sealed class SimpleDoor : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        // A sound script can subscribe here to announce that the passage is open.
        public event Action Opened;

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

using UnityEngine;

namespace AmazingMaze.Player
{
    // Add to floors and steps only. Furniture and door tops must not count as ground.
    [DisallowMultipleComponent]
    public sealed class WalkableSurface : MonoBehaviour { }
}

using UnityEngine;

namespace AmazingMaze.Player
{
    public class BumpSurface : MonoBehaviour
    {
        public enum SurfaceType
        {
            Wall,
            Furniture
        }

        public SurfaceType surfaceType = SurfaceType.Wall;
    }
}
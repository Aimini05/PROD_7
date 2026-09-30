using UnityEngine;

namespace AmazingMaze.Player
{
    // Scene-view aid only. It does not move objects or draw anything in the built game.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridPlayerController))]
    public sealed class MovementGridGizmo : MonoBehaviour
    {
        [Header("Scene view grid")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Vector2 minimumXZ = new Vector2(-14f, -6f);
        [SerializeField] private Vector2 maximumXZ = new Vector2(12f, 42f);
        [Tooltip("Keep above the floors so lines remain visible. This does not change movement height.")]
        [SerializeField] private float displayHeight = 0.65f;
        [SerializeField] private bool showTileCenters = true;
        [SerializeField] private bool showDoors = true;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showGrid) return;
            var player = GetComponent<GridPlayerController>();
            if (player == null) return;
            float size = Mathf.Max(0.1f, player.TileSize);
            Vector3 origin = player.GridOrigin;
            // Limit drawing even if very large bounds or tiny tiles are entered by mistake.
            int minX = Mathf.FloorToInt((minimumXZ.x - origin.x) / size);
            int minZ = Mathf.FloorToInt((minimumXZ.y - origin.z) / size);
            int maxX = Mathf.Clamp(Mathf.CeilToInt((maximumXZ.x - origin.x) / size), minX, minX + 100);
            int maxZ = Mathf.Clamp(Mathf.CeilToInt((maximumXZ.y - origin.z) / size), minZ, minZ + 100);
            float left = origin.x + (minX - 0.5f) * size;
            float right = origin.x + (maxX + 0.5f) * size;
            float bottom = origin.z + (minZ - 0.5f) * size;
            float top = origin.z + (maxZ + 0.5f) * size;

            // Cell boundaries lie half a tile away from the positions the player occupies.
            Gizmos.color = new Color(0f, 1f, 1f, 0.45f);
            for (int x = minX; x <= maxX + 1; x++)
            {
                float worldX = origin.x + (x - 0.5f) * size;
                Gizmos.DrawLine(new Vector3(worldX, displayHeight, bottom), new Vector3(worldX, displayHeight, top));
            }
            for (int z = minZ; z <= maxZ + 1; z++)
            {
                float worldZ = origin.z + (z - 0.5f) * size;
                Gizmos.DrawLine(new Vector3(left, displayHeight, worldZ), new Vector3(right, displayHeight, worldZ));
            }
            if (showTileCenters)
            {
                Gizmos.color = Color.yellow;
                for (int x = minX; x <= maxX; x++)
                    for (int z = minZ; z <= maxZ; z++)
                        Gizmos.DrawSphere(new Vector3(origin.x + x * size, displayHeight, origin.z + z * size), size * 0.035f);
            }

            Gizmos.color = Color.green;
            Vector3 playerPoint = new Vector3(transform.position.x, displayHeight, transform.position.z);
            Gizmos.DrawWireSphere(playerPoint, size * 0.2f);
            Gizmos.DrawRay(playerPoint, transform.forward * size * 0.5f);

            if (!showDoors) return;
            // Magenta markers show actual doors, not suggested snap positions.
            foreach (var door in FindObjectsByType<SimpleDoor>(FindObjectsSortMode.None))
            {
                if (door.gameObject.scene != gameObject.scene) continue;
                Vector3 position = door.transform.position;
                position.y = displayHeight;
                Gizmos.color = door.IsOpen ? Color.green : Color.magenta;
                Gizmos.DrawWireSphere(position, size * 0.12f);
                UnityEditor.Handles.Label(position + Vector3.up * 0.1f, door.name);
            }
        }
#endif
    }
}

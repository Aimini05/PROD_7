using System;
using System.Collections.Generic;
using System.Linq;
using AmazingMaze.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmazingMaze.Editor
{
    /// <summary>
    /// Snaps doors to the boundaries between movement tiles in GameTestAimi only.
    /// Walls, floors, props, and Game.unity are never changed by this tool.
    /// </summary>
    public static class GridDoorLayoutTool
    {
        private const string TestScenePath = "Assets/Scenes/GameTestAimi.unity";
        [MenuItem("Tools/The Amazing Maze/Align GameTestAimi Doors to Grid")]
        public static void AlignGameTestDoorsToGrid()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            GridPlayerController player = UnityEngine.Object.FindFirstObjectByType<GridPlayerController>();
            if (player == null || player.gameObject.scene != scene)
            {
                throw new InvalidOperationException($"No GridPlayerController was found in {TestScenePath}.");
            }

            SimpleDoor[] doors = UnityEngine.Object.FindObjectsByType<SimpleDoor>(FindObjectsSortMode.None)
                .Where(door => door.gameObject.scene == scene)
                .OrderBy(door => door.name, StringComparer.Ordinal)
                .ToArray();
            if (doors.Length == 0)
            {
                throw new InvalidOperationException($"No SimpleDoor objects were found in {TestScenePath}.");
            }

            float tileSize = player.TileSize;
            Vector3 gridOrigin = player.GridOrigin;
            List<string> alignmentReport = new List<string>();

            foreach (SimpleDoor door in doors)
            {
                Vector3 oldPosition = door.transform.position;
                bool thinOnX = IsThinOnX(door);
                Vector3 alignedPosition = oldPosition;

                if (thinOnX)
                {
                    alignedPosition.x = SnapToBoundary(oldPosition.x, gridOrigin.x, tileSize);
                    alignedPosition.z = SnapToCenter(oldPosition.z, gridOrigin.z, tileSize);
                }
                else
                {
                    alignedPosition.x = SnapToCenter(oldPosition.x, gridOrigin.x, tileSize);
                    alignedPosition.z = SnapToBoundary(oldPosition.z, gridOrigin.z, tileSize);
                }

                door.transform.position = alignedPosition;
                PrefabUtility.RecordPrefabInstancePropertyModifications(door.transform);
                alignmentReport.Add(
                    $"{door.name}: ({oldPosition.x:0.###}, {oldPosition.z:0.###}) -> " +
                    $"({alignedPosition.x:0.###}, {alignedPosition.z:0.###}), " +
                    $"{(thinOnX ? "X boundary / Z lane" : "Z boundary / X lane")}");
            }

            EditorSceneManager.SaveScene(scene, TestScenePath);
            Debug.Log(
                $"Aligned {doors.Length} doors in {TestScenePath}. " +
                "Walls and props were not changed; Game.unity was not changed.\n" +
                string.Join("\n", alignmentReport));
        }

        private static bool IsThinOnX(SimpleDoor door)
        {
            Collider[] colliders = door.GetComponentsInChildren<Collider>(true);
            Bounds bounds = GetCombinedBounds(colliders, door.transform.position);
            return bounds.size.x < bounds.size.z;
        }

        private static Bounds GetCombinedBounds(Collider[] colliders, Vector3 fallbackPosition)
        {
            if (colliders.Length == 0) return new Bounds(fallbackPosition, Vector3.one);

            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) bounds.Encapsulate(colliders[i].bounds);
            return bounds;
        }

        private static float SnapToCenter(float value, float origin, float tileSize)
        {
            return origin + Mathf.Round((value - origin) / tileSize) * tileSize;
        }

        private static float SnapToBoundary(float value, float origin, float tileSize)
        {
            float halfTile = tileSize * 0.5f;
            return origin + halfTile + Mathf.Round((value - origin - halfTile) / tileSize) * tileSize;
        }
    }
}

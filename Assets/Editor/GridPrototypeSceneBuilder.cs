using AmazingMaze.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AmazingMaze.Editor
{
    public static class GridPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const float TileSize = 2f;

        [MenuItem("Tools/The Amazing Maze/Rebuild Grid Prototype")]
        public static void RebuildGridPrototype()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLighting();
            CreateEnvironment();
            CreatePlayer();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Grid prototype created at {ScenePath}");
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject volumeObject = new GameObject("Global Volume");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
        }

        private static void CreateEnvironment()
        {
            Transform environment = new GameObject("Grid Prototype").transform;

            GameObject floor = CreateCube("Floor", new Vector3(0f, -0.1f, 6f), new Vector3(18f, 0.2f, 18f), environment);
            floor.isStatic = true;
            floor.AddComponent<WalkableSurface>();

            for (int x = -4; x <= 4; x++)
            {
                CreateWall(x, -1, environment);
                CreateWall(x, 7, environment);
            }

            for (int z = 0; z <= 6; z++)
            {
                CreateWall(-4, z, environment);
                CreateWall(4, z, environment);
            }

            int[,] interiorWalls =
            {
                { -2, 1 }, { -1, 1 }, { 1, 1 }, { 2, 1 },
                { 2, 2 }, { -2, 3 }, { 0, 3 }, { 2, 3 },
                { -2, 4 }, { 0, 4 }, { -2, 5 }, { 2, 5 }
            };

            for (int i = 0; i < interiorWalls.GetLength(0); i++)
            {
                CreateWall(interiorWalls[i, 0], interiorWalls[i, 1], environment);
            }
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("Player");
            player.transform.position = Vector3.zero;

            CapsuleCollider collider = player.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.height = 1.8f;
            collider.radius = 0.35f;
            player.AddComponent<GridPlayerController>();

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(player.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
        }

        private static void CreateWall(int gridX, int gridZ, Transform parent)
        {
            GameObject wall = CreateCube(
                $"Wall ({gridX}, {gridZ})",
                new Vector3(gridX * TileSize, 1f, gridZ * TileSize),
                new Vector3(TileSize, 2f, TileSize),
                parent);
            wall.isStatic = true;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Transform parent)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            return cube;
        }
    }
}

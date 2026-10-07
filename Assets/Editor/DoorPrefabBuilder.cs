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
    /// Creates the shared door prefab and converts scene doors to prefab instances.
    /// </summary>
    public static class DoorPrefabBuilder
    {
        private const string TestScenePath = "Assets/Scenes/GameTestAimi.unity";
        private const string GameScenePath = "Assets/Scenes/Game.unity";
        private const string PrefabPath = "Assets/Prefabs/Door.prefab";
        private const string OpenCloseSoundPath = "Assets/Audio/SFX/Door_OpenClose.wav";
        private const string LockedSoundPath = "Assets/Audio/SFX/LockedDoor.wav";

        [MenuItem("Tools/The Amazing Maze/Create Door Prefab and Update GameTestAimi")]
        public static void CreateDoorPrefabAndUpdateTestScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
            SimpleDoor[] doors = UnityEngine.Object.FindObjectsByType<SimpleDoor>(FindObjectsSortMode.None)
                .Where(door => door.gameObject.scene == scene)
                .OrderBy(door => GetHierarchyPath(door.transform), StringComparer.Ordinal)
                .ToArray();

            if (doors.Length == 0)
            {
                throw new InvalidOperationException($"No SimpleDoor objects were found in {TestScenePath}.");
            }

            AudioClip openCloseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(OpenCloseSoundPath);
            AudioClip lockedSound = AssetDatabase.LoadAssetAtPath<AudioClip>(LockedSoundPath);
            if (openCloseSound == null || lockedSound == null)
            {
                throw new InvalidOperationException("The expected door audio clips could not be loaded.");
            }

            GameObject prefabAsset = CreatePrefabAsset(doors[0], openCloseSound, lockedSound);
            foreach (SimpleDoor oldDoor in doors)
            {
                ReplaceWithPrefabInstance(oldDoor, prefabAsset, scene);
            }

            EditorSceneManager.SaveScene(scene, TestScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Application.isBatchMode)
            {
                Selection.activeObject = prefabAsset;
                EditorGUIUtility.PingObject(prefabAsset);
            }

            Debug.Log($"Created {PrefabPath} and converted {doors.Length} doors in {TestScenePath}. Game.unity was not changed.");
        }

        [MenuItem("Tools/The Amazing Maze/Replace Manual Doors in Game")]
        public static void ReplaceManualDoorsInGame()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null || prefabAsset.GetComponent<SimpleDoor>() == null)
            {
                throw new InvalidOperationException($"A valid SimpleDoor prefab was not found at {PrefabPath}.");
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            SimpleDoor[] manualDoors = UnityEngine.Object.FindObjectsByType<SimpleDoor>(FindObjectsSortMode.None)
                .Where(door => door.gameObject.scene == scene)
                .Where(door => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(door.gameObject) != PrefabPath)
                .OrderBy(door => GetHierarchyPath(door.transform), StringComparer.Ordinal)
                .ToArray();

            foreach (SimpleDoor oldDoor in manualDoors)
            {
                ReplaceWithPrefabInstance(oldDoor, prefabAsset, scene);
            }

            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Converted {manualDoors.Length} manual doors to {PrefabPath} instances in {GameScenePath}.");
        }

        [MenuItem("Tools/The Amazing Maze/Organize New House and Convert Doors")]
        public static void OrganizeNewHouseAndConvertDoors()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefabAsset == null || prefabAsset.GetComponent<SimpleDoor>() == null)
            {
                throw new InvalidOperationException($"A valid SimpleDoor prefab was not found at {PrefabPath}.");
            }

            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            GameObject newHouse = FindOrCreateRoot(scene, "New House");
            Transform floors = FindOrCreateFolder(newHouse.transform, "Floors and Steps");
            Transform walls = FindOrCreateFolder(newHouse.transform, "Walls");
            Transform doors = FindOrCreateFolder(newHouse.transform, "Doors");
            Transform props = FindOrCreateFolder(newHouse.transform, "Props");
            Transform labels = FindOrCreateFolder(newHouse.transform, "Room Labels");

            List<GameObject> looseRoots = scene.GetRootGameObjects()
                .Where(root => root != newHouse)
                .ToList();

            int convertedDoors = 0;
            int organizedObjects = 0;
            foreach (GameObject rootObject in looseRoots)
            {
                GameObject root = rootObject;
                string name = root.name;
                Transform destination = null;

                if (name.StartsWith("Wall (", StringComparison.Ordinal))
                {
                    destination = walls;
                }
                else if (name.StartsWith("Door (", StringComparison.Ordinal))
                {
                    SimpleDoor manualDoor = root.GetComponent<SimpleDoor>();
                    if (manualDoor != null &&
                        PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root) != PrefabPath)
                    {
                        root = ReplaceWithPrefabInstance(manualDoor, prefabAsset, scene);
                        convertedDoors++;
                    }
                    destination = doors;
                }
                else if ((name.StartsWith("Floor (", StringComparison.Ordinal) ||
                          name.StartsWith("Floor_Grass (", StringComparison.Ordinal) ||
                          name.StartsWith("Step (", StringComparison.Ordinal)))
                {
                    destination = floors;
                }
                else if (name.StartsWith("Text (TMP)_", StringComparison.Ordinal))
                {
                    destination = labels;
                }
                else if (IsNewHouseProp(name))
                {
                    destination = props;
                }

                if (destination == null) continue;
                root.transform.SetParent(destination, true);
                organizedObjects++;
            }

            EditorSceneManager.SaveScene(scene, GameScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Organized {organizedObjects} new-house objects under 'New House' and converted " +
                $"{convertedDoors} manual doors to {PrefabPath} instances.");
        }

        private static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            GameObject existing = scene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == name);
            if (existing != null) return existing;

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static Transform FindOrCreateFolder(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return existing;

            GameObject folder = new GameObject(name);
            folder.transform.SetParent(parent, false);
            return folder.transform;
        }

        private static bool IsNewHouseProp(string name)
        {
            return name.StartsWith("Chest (", StringComparison.Ordinal) ||
                   name.StartsWith("Shelf (", StringComparison.Ordinal) ||
                   name.StartsWith("Couch (", StringComparison.Ordinal) ||
                   name.StartsWith("CouchTABLE (", StringComparison.Ordinal) ||
                   name.StartsWith("Fireplace (", StringComparison.Ordinal) ||
                   name.StartsWith("TV (", StringComparison.Ordinal) ||
                   name == "mat";
        }

        private static GameObject CreatePrefabAsset(SimpleDoor source, AudioClip openCloseSound, AudioClip lockedSound)
        {
            GameObject template = UnityEngine.Object.Instantiate(source.gameObject);
            template.name = "Door";
            template.transform.SetParent(null);
            template.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            template.transform.localScale = Vector3.one;
            template.SetActive(true);

            SimpleDoor templateDoor = template.GetComponent<SimpleDoor>();
            SerializedObject serializedDoor = new SerializedObject(templateDoor);
            serializedDoor.FindProperty("isLocked").boolValue = false;
            serializedDoor.FindProperty("openSound").objectReferenceValue = openCloseSound;
            serializedDoor.FindProperty("lockedSound").objectReferenceValue = lockedSound;
            serializedDoor.FindProperty("minimumPassableDuration").floatValue = 1.5f;
            serializedDoor.FindProperty("closeAfterPassage").boolValue = true;
            serializedDoor.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
            UnityEngine.Object.DestroyImmediate(template);

            if (prefabAsset == null)
            {
                throw new InvalidOperationException($"Unity could not create {PrefabPath}.");
            }

            return prefabAsset;
        }

        private static GameObject ReplaceWithPrefabInstance(SimpleDoor oldDoor, GameObject prefabAsset, Scene scene)
        {
            Transform oldTransform = oldDoor.transform;
            Transform parent = oldTransform.parent;
            int siblingIndex = oldTransform.GetSiblingIndex();
            string objectName = oldDoor.gameObject.name;
            Vector3 localPosition = oldTransform.localPosition;
            Quaternion localRotation = oldTransform.localRotation;
            Vector3 localScale = oldTransform.localScale;
            int layer = oldDoor.gameObject.layer;
            string tag = oldDoor.gameObject.tag;
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(oldDoor.gameObject);
            bool active = oldDoor.gameObject.activeSelf;
            bool locked = oldDoor.IsLocked;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, scene);
            instance.name = objectName;
            instance.transform.SetParent(parent, false);
            instance.transform.SetSiblingIndex(siblingIndex);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            instance.layer = layer;
            instance.tag = tag;
            GameObjectUtility.SetStaticEditorFlags(instance, staticFlags);
            instance.SetActive(active);

            SimpleDoor instanceDoor = instance.GetComponent<SimpleDoor>();
            if (locked)
            {
                SerializedObject serializedDoor = new SerializedObject(instanceDoor);
                serializedDoor.FindProperty("isLocked").boolValue = true;
                serializedDoor.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(instanceDoor);
            }

            UnityEngine.Object.DestroyImmediate(oldDoor.gameObject);
            return instance;
        }

        private static string GetHierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }
    }
}

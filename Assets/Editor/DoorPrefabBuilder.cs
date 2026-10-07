using System;
using System.Linq;
using AmazingMaze.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AmazingMaze.Editor
{
    /// <summary>
    /// Creates the shared door prefab and converts only GameTestAimi's existing doors.
    /// Game.unity is deliberately never opened or modified by this tool.
    /// </summary>
    public static class DoorPrefabBuilder
    {
        private const string TestScenePath = "Assets/Scenes/GameTestAimi.unity";
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

        private static void ReplaceWithPrefabInstance(SimpleDoor oldDoor, GameObject prefabAsset, Scene scene)
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

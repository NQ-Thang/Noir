using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class MultiSceneSetup
{
    private const string CoreScenePath = "Assets/_Project/Scenes/Core_Scene.unity";
    private const string SpiritRealmScenePath = "Assets/_Project/Scenes/SpiritRealm_Scene.unity";
    private const string SampleScenePath = "Assets/_Project/Scenes/SampleScene.unity";

    [MenuItem("Tools/Multi-Scene Architecture/Setup or Verify Scenes", false, 10)]
    public static void SetupAndVerify()
    {
        Debug.Log("=== Starting Multi-Scene Architecture Setup & Verification ===");

        // 1. Ensure Scenes Directory exists
        string scenesDir = "Assets/_Project/Scenes";
        if (!Directory.Exists(scenesDir)) Directory.CreateDirectory(scenesDir);

        // 2. Setup / Verify Core_Scene
        SetupCoreScene();

        // 3. Setup / Verify SpiritRealm_Scene
        SetupSpiritRealmScene();

        // 4. Setup Build Settings
        SetupBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Multi-Scene Architecture Setup Completed Successfully! ===");
    }

    [MenuItem("Tools/Multi-Scene Architecture/Open Both Scenes in Hierarchy (Multi-Scene View)", false, 20)]
    public static void OpenBothScenes()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Scene coreScene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            Scene spiritScene = EditorSceneManager.OpenScene(SpiritRealmScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(spiritScene);
            Debug.Log("[MultiSceneSetup] Opened Core_Scene (Persistent) and SpiritRealm_Scene (Active Gameplay).");
        }
    }

    [MenuItem("Tools/Multi-Scene Architecture/Open Spirit Realm Scene Only", false, 21)]
    public static void OpenSpiritRealmOnly()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SpiritRealmScenePath, OpenSceneMode.Single);
            Debug.Log("[MultiSceneSetup] Opened SpiritRealm_Scene in Single mode.");
        }
    }

    [MenuItem("Tools/Multi-Scene Architecture/Open Core Scene Only", false, 22)]
    public static void OpenCoreOnly()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            Debug.Log("[MultiSceneSetup] Opened Core_Scene in Single mode.");
        }
    }

    private static void SetupCoreScene()
    {
        Scene coreScene;
        bool isNew = !File.Exists(CoreScenePath);

        if (isNew)
        {
            coreScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
        else
        {
            coreScene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
        }

        // Ensure [Core] GameObject with GameManager and SceneLoader
        GameObject coreGo = GameObject.Find("[Core]");
        if (coreGo == null)
        {
            coreGo = new GameObject("[Core]");
        }
        coreGo.transform.position = Vector3.zero;

        if (coreGo.GetComponent<GameManager>() == null)
        {
            coreGo.AddComponent<GameManager>();
        }

        if (coreGo.GetComponent<SceneLoader>() == null)
        {
            coreGo.AddComponent<SceneLoader>();
        }

        // Ensure EventSystem
        GameObject eventSystemGo = GameObject.Find("EventSystem");
        if (eventSystemGo == null)
        {
            eventSystemGo = new GameObject("EventSystem");
        }
        eventSystemGo.transform.position = Vector3.zero;

        if (eventSystemGo.GetComponent<EventSystem>() == null)
        {
            eventSystemGo.AddComponent<EventSystem>();
        }

        if (eventSystemGo.GetComponent<InputSystemUIInputModule>() == null &&
            eventSystemGo.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        EditorSceneManager.MarkSceneDirty(coreScene);
        EditorSceneManager.SaveScene(coreScene, CoreScenePath);
        Debug.Log($"[MultiSceneSetup] Core_Scene verified and saved at: {CoreScenePath}");
    }

    private static void SetupSpiritRealmScene()
    {
        if (!File.Exists(SpiritRealmScenePath))
        {
            if (File.Exists(SampleScenePath))
            {
                File.Copy(SampleScenePath, SpiritRealmScenePath, true);
                AssetDatabase.ImportAsset(SpiritRealmScenePath, ImportAssetOptions.ForceUpdate);
                Debug.Log($"[MultiSceneSetup] Copied existing demo level from {SampleScenePath} to {SpiritRealmScenePath}");
            }
            else
            {
                Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(newScene, SpiritRealmScenePath);
                Debug.Log($"[MultiSceneSetup] Created new SpiritRealm_Scene at: {SpiritRealmScenePath}");
            }
        }
        else
        {
            Debug.Log($"[MultiSceneSetup] SpiritRealm_Scene already exists at: {SpiritRealmScenePath}");
        }
    }

    private static void SetupBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(CoreScenePath, true),
            new EditorBuildSettingsScene(SpiritRealmScenePath, true),
            new EditorBuildSettingsScene(SampleScenePath, true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[MultiSceneSetup] Configured EditorBuildSettings scenes: 0: Core_Scene, 1: SpiritRealm_Scene, 2: SampleScene.");
    }
}

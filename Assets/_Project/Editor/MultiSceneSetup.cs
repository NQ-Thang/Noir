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

    [MenuItem("Tools/Multi-Scene Architecture/Setup Large Room (Spirit Realm)", false, 15)]
    public static void SetupLargeRoomInSpiritRealm()
    {
        Scene spiritScene = SceneManager.GetSceneByPath(SpiritRealmScenePath);
        if (!spiritScene.isLoaded)
        {
            spiritScene = EditorSceneManager.OpenScene(SpiritRealmScenePath, OpenSceneMode.Additive);
        }

        // 1. Remove old "Square" if present
        GameObject oldSquare = GameObject.Find("Square");
        if (oldSquare != null && oldSquare.scene == spiritScene)
        {
            Undo.DestroyObjectImmediate(oldSquare);
        }

        // Get standard square sprite
        Sprite squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        if (squareSprite == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite Square");
            if (guids.Length > 0) squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer == -1) groundLayer = 6;

        int wallLayer = LayerMask.NameToLayer("Wall");
        if (wallLayer == -1) wallLayer = 8;

        // 2. Ground_Path (Đường kẻ mỏng mặt đất)
        GameObject groundGo = GameObject.Find("Ground_Path");
        if (groundGo == null || groundGo.scene != spiritScene)
        {
            groundGo = new GameObject("Ground_Path");
            SceneManager.MoveGameObjectToScene(groundGo, spiritScene);
            Undo.RegisterCreatedObjectUndo(groundGo, "Create Ground_Path");
        }
        groundGo.layer = groundLayer;
        groundGo.transform.position = Vector3.zero;
        groundGo.transform.localScale = new Vector3(120f, 0.05f, 1f);

        SpriteRenderer srGround = groundGo.GetComponent<SpriteRenderer>();
        if (srGround == null) srGround = groundGo.AddComponent<SpriteRenderer>();
        if (squareSprite != null) srGround.sprite = squareSprite;
        srGround.color = new Color(1f, 1f, 1f, 0.9f);

        BoxCollider2D colGround = groundGo.GetComponent<BoxCollider2D>();
        if (colGround == null) colGround = groundGo.AddComponent<BoxCollider2D>();
        colGround.size = new Vector2(1f, 40f);
        colGround.offset = new Vector2(0f, -19.5f);

        // 3. Wall_Left
        GameObject wallLeft = GameObject.Find("Wall_Left");
        if (wallLeft == null || wallLeft.scene != spiritScene)
        {
            wallLeft = new GameObject("Wall_Left");
            SceneManager.MoveGameObjectToScene(wallLeft, spiritScene);
            Undo.RegisterCreatedObjectUndo(wallLeft, "Create Wall_Left");
        }
        wallLeft.layer = wallLayer;
        wallLeft.transform.position = new Vector3(-60f, 12.5f, 0f);
        wallLeft.transform.localScale = new Vector3(0.05f, 25f, 1f);

        SpriteRenderer srWallLeft = wallLeft.GetComponent<SpriteRenderer>();
        if (srWallLeft == null) srWallLeft = wallLeft.AddComponent<SpriteRenderer>();
        if (squareSprite != null) srWallLeft.sprite = squareSprite;
        srWallLeft.color = new Color(1f, 1f, 1f, 0.6f);

        BoxCollider2D colWallLeft = wallLeft.GetComponent<BoxCollider2D>();
        if (colWallLeft == null) colWallLeft = wallLeft.AddComponent<BoxCollider2D>();
        colWallLeft.size = new Vector2(40f, 1f);
        colWallLeft.offset = new Vector2(-19.5f, 0f);

        // 4. Wall_Right
        GameObject wallRight = GameObject.Find("Wall_Right");
        if (wallRight == null || wallRight.scene != spiritScene)
        {
            wallRight = new GameObject("Wall_Right");
            SceneManager.MoveGameObjectToScene(wallRight, spiritScene);
            Undo.RegisterCreatedObjectUndo(wallRight, "Create Wall_Right");
        }
        wallRight.layer = wallLayer;
        wallRight.transform.position = new Vector3(60f, 12.5f, 0f);
        wallRight.transform.localScale = new Vector3(0.05f, 25f, 1f);

        SpriteRenderer srWallRight = wallRight.GetComponent<SpriteRenderer>();
        if (srWallRight == null) srWallRight = wallRight.AddComponent<SpriteRenderer>();
        if (squareSprite != null) srWallRight.sprite = squareSprite;
        srWallRight.color = new Color(1f, 1f, 1f, 0.6f);

        BoxCollider2D colWallRight = wallRight.GetComponent<BoxCollider2D>();
        if (colWallRight == null) colWallRight = wallRight.AddComponent<BoxCollider2D>();
        colWallRight.size = new Vector2(40f, 1f);
        colWallRight.offset = new Vector2(19.5f, 0f);

        // 5. Ceiling
        GameObject ceiling = GameObject.Find("Ceiling");
        if (ceiling == null || ceiling.scene != spiritScene)
        {
            ceiling = new GameObject("Ceiling");
            SceneManager.MoveGameObjectToScene(ceiling, spiritScene);
            Undo.RegisterCreatedObjectUndo(ceiling, "Create Ceiling");
        }
        ceiling.layer = wallLayer;
        ceiling.transform.position = new Vector3(0f, 25f, 0f);
        ceiling.transform.localScale = new Vector3(120f, 0.05f, 1f);

        SpriteRenderer srCeiling = ceiling.GetComponent<SpriteRenderer>();
        if (srCeiling == null) srCeiling = ceiling.AddComponent<SpriteRenderer>();
        if (squareSprite != null) srCeiling.sprite = squareSprite;
        srCeiling.color = new Color(1f, 1f, 1f, 0.3f);

        BoxCollider2D colCeiling = ceiling.GetComponent<BoxCollider2D>();
        if (colCeiling == null) colCeiling = ceiling.AddComponent<BoxCollider2D>();
        colCeiling.size = new Vector2(1f, 40f);
        colCeiling.offset = new Vector2(0f, 19.5f);

        // 6. CameraConfinerBounds
        GameObject confinerGo = GameObject.Find("CameraConfinerBounds");
        if (confinerGo == null || confinerGo.scene != spiritScene)
        {
            confinerGo = new GameObject("CameraConfinerBounds");
            SceneManager.MoveGameObjectToScene(confinerGo, spiritScene);
            Undo.RegisterCreatedObjectUndo(confinerGo, "Create CameraConfinerBounds");
        }
        confinerGo.layer = 2; // Ignore Raycast
        confinerGo.transform.position = Vector3.zero;

        PolygonCollider2D polyCol = confinerGo.GetComponent<PolygonCollider2D>();
        if (polyCol == null) polyCol = confinerGo.AddComponent<PolygonCollider2D>();
        polyCol.isTrigger = true;
        polyCol.points = new Vector2[]
        {
            new Vector2(-60f, -2f),
            new Vector2(60f, -2f),
            new Vector2(60f, 26f),
            new Vector2(-60f, 26f)
        };

        // 7. SpawnPoint_Default
        GameObject spawnGo = GameObject.Find("SpawnPoint_Default");
        if (spawnGo == null || spawnGo.scene != spiritScene)
        {
            spawnGo = new GameObject("SpawnPoint_Default");
            SceneManager.MoveGameObjectToScene(spawnGo, spiritScene);
            Undo.RegisterCreatedObjectUndo(spawnGo, "Create SpawnPoint_Default");
        }
        spawnGo.tag = "Respawn";
        spawnGo.transform.position = new Vector3(-45f, 1f, 0f);
        if (spawnGo.GetComponent<SpawnPoint>() == null)
        {
            spawnGo.AddComponent<SpawnPoint>();
        }

        // 8. Global Light 2D
        GameObject lightGo = GameObject.Find("Global Light 2D");
        if (lightGo != null && lightGo.scene == spiritScene)
        {
            lightGo.transform.position = new Vector3(0f, 12.5f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(spiritScene);
        EditorSceneManager.SaveScene(spiritScene);
        Debug.Log("[MultiSceneSetup] Setup large room in SpiritRealm_Scene successfully! (Ground is a thin line: 120m x 0.05m)");
    }

    private static void SetupSpiritRealmScene()
    {
        SetupLargeRoomInSpiritRealm();
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

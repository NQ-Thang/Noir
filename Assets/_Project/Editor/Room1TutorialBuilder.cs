using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor utility to construct Room 1 (Tutorial Room) inside SpiritRealm_Scene.
/// Sized for fast player movement (moveSpeed = 7.5, maxJumpH ~ 4.2m, maxJumpDist ~ 12.2m, swordRange = 8m).
/// Progression flows Left -> Right:
/// 1. Movement -> 2. Jump & Wall Jump -> 3. Melee Combat -> 4. Mandatory Throw & Dash -> 5. Synthesis Challenge.
/// </summary>
public static class Room1TutorialBuilder
{
    private const string SpiritRealmScenePath = "Assets/_Project/Scenes/SpiritRealm_Scene.unity";
    private const string RoomRootName = "--- ROOM 1: TUTORIAL ---";

    // Dark-fantasy minimal palette matching ART_DIRECTION.md (high readability against dark background)
    private static readonly Color GroundColor = new Color(0.18f, 0.20f, 0.24f, 1f);
    private static readonly Color WallColor = new Color(0.26f, 0.29f, 0.35f, 1f);
    private static readonly Color PlatformColor = new Color(0.22f, 0.25f, 0.30f, 1f);
    private static readonly Color SafetyFloorColor = new Color(0.12f, 0.13f, 0.16f, 0.9f);
    private static readonly Color BoundaryColor = new Color(0.10f, 0.10f, 0.12f, 0.8f);
    private static readonly Color EnemyMarkerColor = new Color(0.85f, 0.25f, 0.25f, 0.55f);
    private static readonly Color GoalColor = new Color(0.85f, 0.92f, 1.0f, 0.85f);

    [MenuItem("Tools/Level Design/Build Room 1 (Tutorial) in SpiritRealm_Scene", false, 30)]
    public static void BuildRoom1()
    {
        Scene spiritScene = SceneManager.GetSceneByPath(SpiritRealmScenePath);
        if (!spiritScene.isLoaded)
        {
            spiritScene = EditorSceneManager.OpenScene(SpiritRealmScenePath, OpenSceneMode.Additive);
        }

        Scene previousActive = SceneManager.GetActiveScene();
        SceneManager.SetActiveScene(spiritScene);

        Sprite squareSprite = LoadSquareSprite();
        int groundLayer = GetLayerOrDefault("Ground", 6);
        int wallLayer = GetLayerOrDefault("Wall", 8);

        // Clean up legacy flat-room objects or previous Room 1 build
        DestroyIfInScene(spiritScene, "Ground_Path");
        DestroyIfInScene(spiritScene, "Wall_Left");
        DestroyIfInScene(spiritScene, "Wall_Right");
        DestroyIfInScene(spiritScene, "Ceiling");
        DestroyIfInScene(spiritScene, RoomRootName);

        GameObject roomRoot = new GameObject(RoomRootName);
        SceneManager.MoveGameObjectToScene(roomRoot, spiritScene);
        Undo.RegisterCreatedObjectUndo(roomRoot, "Build Room 1 Tutorial");

        // ------------------------------------------------------------------
        // 0. OUTER BOUNDS, CAMERA CONFINER & SPAWN POINT (x: -80 -> +140, y: -8 -> +38)
        // ------------------------------------------------------------------
        GameObject boundsGroup = CreateGroup("00_Bounds_And_Spawn", roomRoot.transform);
        CreateBox("Wall_Left", boundsGroup.transform, new Vector2(-80f, 15f), new Vector2(2f, 48f), wallLayer, BoundaryColor, squareSprite);
        CreateBox("Wall_Right", boundsGroup.transform, new Vector2(140f, 15f), new Vector2(2f, 48f), wallLayer, BoundaryColor, squareSprite);
        CreateBox("Ceiling", boundsGroup.transform, new Vector2(30f, 38.5f), new Vector2(222f, 1f), wallLayer, BoundaryColor, squareSprite);
        CreateBox("Safety_Floor_Bottom", boundsGroup.transform, new Vector2(30f, -7.5f), new Vector2(222f, 2f), groundLayer, SafetyFloorColor, squareSprite);

        SetupCameraConfiner(spiritScene, new Vector2[]
        {
            new Vector2(-80f, -8f),
            new Vector2(140f, -8f),
            new Vector2(140f, 38f),
            new Vector2(-80f, 38f)
        });

        SetupSpawnPoint(spiritScene, new Vector3(-74f, 1.5f, 0f));

        // ------------------------------------------------------------------
        // 1. ZONE 1: MOVEMENT (x: -78 -> -48)
        // ------------------------------------------------------------------
        GameObject z1 = CreateGroup("01_Zone_Movement", roomRoot.transform);
        CreateGroundByTop("Z1_Ground_Main", z1.transform, -78f, -48f, 0f, 6f, groundLayer, GroundColor, squareSprite);
        CreateGroundByTop("Z1_Step_Low", z1.transform, -60f, -54f, 1.0f, 1.0f, groundLayer, PlatformColor, squareSprite);

        CreateTutorialPrompt(
            "Prompt_01_Movement",
            z1.transform,
            new Vector3(-70f, 4.5f, 0f),
            "1. DI CHUYỂN\n[A / D] hoặc [← / →] để di chuyển sang phải"
        );

        // ------------------------------------------------------------------
        // 2. ZONE 2: JUMP & WALL JUMP (x: -48 -> 0)
        // ------------------------------------------------------------------
        GameObject z2 = CreateGroup("02_Zone_Jump_And_WallJump", roomRoot.transform);

        // 2A: Jump & Variable Height
        CreateGroundByTop("Z2_Ground_Start", z2.transform, -48f, -38f, 0f, 6f, groundLayer, GroundColor, squareSprite);
        CreateGroundByTop("Z2_Platform_Step1", z2.transform, -36f, -30f, 2.2f, 1.0f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z2_Pit_SafetyFloor", z2.transform, -38f, -16.5f, -3.5f, 2f, groundLayer, SafetyFloorColor, squareSprite);
        CreateGroundByTop("Z2_Pit_EscapeStep", z2.transform, -37.5f, -34.5f, -1.2f, 0.7f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z2_Platform_Step2", z2.transform, -23.5f, -16.5f, 3.8f, 1.0f, groundLayer, PlatformColor, squareSprite);

        CreateTutorialPrompt(
            "Prompt_02A_Jump",
            z2.transform,
            new Vector3(-41f, 5.2f, 0f),
            "2. NHẢY\nNhấn [SPACE] để nhảy\n(Giữ lâu hơn để nhảy cao hơn)"
        );

        // 2B: Spacious Wall Slide & Wall Jump Chimney (5.5m inner gap, 10.5m exit)
        CreateGroundByTop("Z2_WallJump_BaseFloor", z2.transform, -16.5f, -4f, 0f, 6f, groundLayer, GroundColor, squareSprite);
        CreateBoxFromBounds("Z2_WallJump_LeftWall", z2.transform, -12.5f, -11.0f, 4.0f, 14.5f, wallLayer, WallColor, squareSprite);
        CreateBoxFromBounds("Z2_WallJump_RightWall", z2.transform, -5.5f, -4.0f, 0.0f, 10.5f, wallLayer, WallColor, squareSprite);
        CreateGroundByTop("Z2_Platform_Exit", z2.transform, -4.0f, 3.0f, 10.5f, 1.2f, groundLayer, PlatformColor, squareSprite);

        CreateTutorialPrompt(
            "Prompt_02B_WallJump",
            z2.transform,
            new Vector3(-8.5f, 16.0f, 0f),
            "3. BẨY TƯỜNG (WALL JUMP)\nNhảy áp sát tường để trượt, nhấn [SPACE] bật sang tường đối diện"
        );

        // ------------------------------------------------------------------
        // 3. ZONE 3: MELEE ATTACK & FOCUS HEAL (x: +3 -> +32)
        // ------------------------------------------------------------------
        GameObject z3 = CreateGroup("03_Zone_Melee_Combat", roomRoot.transform);
        CreateGroundByTop("Z3_Ground_Arena", z3.transform, 3f, 32f, 0f, 6f, groundLayer, GroundColor, squareSprite);
        CreateGroundByTop("Z3_Step_Exit", z3.transform, 28f, 32f, 2.0f, 2.0f, groundLayer, PlatformColor, squareSprite);

        CreateMarker("Enemy_Spawn_Melee_1", z3.transform, new Vector3(13f, 1.0f, 0f), EnemyMarkerColor, squareSprite, "QUÁI (Thêm sau)");
        CreateMarker("Enemy_Spawn_Melee_2", z3.transform, new Vector3(22f, 1.0f, 0f), EnemyMarkerColor, squareSprite, "QUÁI (Thêm sau)");

        CreateTutorialPrompt(
            "Prompt_03_Melee",
            z3.transform,
            new Vector3(17.5f, 5.5f, 0f),
            "4. TẤN CÔNG THƯỜNG & HỒI MÁU\n[Chuột Trái] + Trỏ chuột: Chém thường (Tích Anima)\n[Giữ S] khi đứng yên: Vận công hồi máu"
        );

        // ------------------------------------------------------------------
        // 4. ZONE 4: MANDATORY THROW & DASH TO SWORD (x: +32 -> +84)
        // ------------------------------------------------------------------
        GameObject z4 = CreateGroup("04_Zone_Throw_And_Dash", roomRoot.transform);

        // 4A: 17m Wide Gap (exceeds 12.2m max jump -> forces Jump + Throw Sword + Dash!)
        CreateGroundByTop("Z4_Ledge_Start", z4.transform, 32f, 39f, 2.0f, 8.0f, groundLayer, GroundColor, squareSprite);
        CreateGroundByTop("Z4_SafetyFloor", z4.transform, 39f, 56f, -4.0f, 2f, groundLayer, SafetyFloorColor, squareSprite);
        CreateGroundByTop("Z4_SafetyStep_1", z4.transform, 38.5f, 41.0f, -1.8f, 0.7f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z4_SafetyStep_2", z4.transform, 36.0f, 38.5f, 0.2f, 0.7f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z4_Ledge_Target", z4.transform, 56f, 68f, 2.0f, 8.0f, groundLayer, GroundColor, squareSprite);
        CreateBoxFromBounds("Z4_HangingWall_Anchor", z4.transform, 55.5f, 57.0f, 6.0f, 11.5f, wallLayer, WallColor, squareSprite);

        CreateTutorialPrompt(
            "Prompt_04A_ThrowDash",
            z4.transform,
            new Vector3(47.5f, 8.5f, 0f),
            "5. PHI KIẾM & LƯỚT (BẮT BUỘC NHẢY + DASH)\nNhảy ra vực → [Chuột Phải] lần 1: Ném kiếm qua vực\n[Chuột Phải] lần 2: Dash tới kiếm  |  [R]: Thu hồi"
        );

        // 4B: 7.5m High Cliff (exceeds 4.2m max jump -> forces Jump + Throw into high wall + Dash!)
        CreateGroundByTop("Z4_HighCliff", z4.transform, 68f, 84f, 9.5f, 15.5f, wallLayer, WallColor, squareSprite);

        CreateTutorialPrompt(
            "Prompt_04B_HighClimb",
            z4.transform,
            new Vector3(63.5f, 11.5f, 0f),
            "6. VƯỢT VÁCH CAO\nNhảy lên & ném kiếm găm vào vách tường cao rồi Dash lên đỉnh!"
        );

        // ------------------------------------------------------------------
        // 5. ZONE 5: SYNTHESIS CHALLENGE (x: +84 -> +136)
        // ------------------------------------------------------------------
        GameObject z5 = CreateGroup("05_Zone_Synthesis", roomRoot.transform);

        // 7m jump -> 18m gap with mid-air hanging wall -> Wall Jump / Dash to Goal at y = 14.5
        CreateGroundByTop("Z5_Platform_Mid", z5.transform, 91f, 98f, 11.0f, 1.2f, groundLayer, PlatformColor, squareSprite);
        CreateBoxFromBounds("Z5_HangingWall_Mid", z5.transform, 109.5f, 111.0f, 12.5f, 19.5f, wallLayer, WallColor, squareSprite);
        CreateGroundByTop("Z5_Platform_Goal", z5.transform, 116f, 136f, 14.5f, 1.8f, groundLayer, GroundColor, squareSprite);

        // Safety floor + return steps below Zone 5
        CreateGroundByTop("Z5_SafetyFloor", z5.transform, 84f, 138f, 0.0f, 6f, groundLayer, SafetyFloorColor, squareSprite);
        CreateGroundByTop("Z5_ReturnStep_1", z5.transform, 86f, 89.5f, 2.8f, 0.8f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z5_ReturnStep_2", z5.transform, 83f, 86.5f, 5.5f, 0.8f, groundLayer, PlatformColor, squareSprite);
        CreateGroundByTop("Z5_ReturnStep_3", z5.transform, 80f, 83.5f, 8.0f, 0.8f, groundLayer, PlatformColor, squareSprite);

        CreateMarker("Enemy_Spawn_Synthesis_Final", z5.transform, new Vector3(124f, 15.5f, 0f), EnemyMarkerColor, squareSprite, "QUÁI CUỐI (Thêm sau)");
        GameObject goalExit = CreateMarker("Room1_Goal_Exit", z5.transform, new Vector3(132f, 16.0f, 0f), GoalColor, squareSprite, "CỔNG SANG ROOM 2");
        BoxCollider2D goalCol = goalExit.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(1.5f, 3.0f);
        goalExit.AddComponent<RoomTransition>();

        CreateTutorialPrompt(
            "Prompt_05_Synthesis",
            z5.transform,
            new Vector3(106f, 21.0f, 0f),
            "7. TỔNG HỢP\nKết hợp: Nhảy + Ném kiếm găm tường + Dash + Wall Jump + Chiến đấu!"
        );

        // Keep Global Light 2D centered
        GameObject lightGo = GameObject.Find("Global Light 2D");
        if (lightGo != null && lightGo.scene == spiritScene)
        {
            lightGo.transform.position = new Vector3(30f, 15f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(spiritScene);
        EditorSceneManager.SaveScene(spiritScene);

        if (previousActive.IsValid())
        {
            SceneManager.SetActiveScene(previousActive);
        }

        Debug.Log("[Room1TutorialBuilder] Room 1 (Spacious & Dash-enforced) built successfully in SpiritRealm_Scene!");
    }

    private static GameObject CreateGroup(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void CreateGroundByTop(string name, Transform parent, float minX, float maxX, float topY, float thickness, int layer, Color color, Sprite sprite)
    {
        float width = maxX - minX;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = topY - thickness * 0.5f;
        CreateBox(name, parent, new Vector2(centerX, centerY), new Vector2(width, thickness), layer, color, sprite);
    }

    private static void CreateBoxFromBounds(string name, Transform parent, float minX, float maxX, float minY, float maxY, int layer, Color color, Sprite sprite)
    {
        float width = maxX - minX;
        float height = maxY - minY;
        Vector2 center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        CreateBox(name, parent, center, new Vector2(width, height), layer, color, sprite);
    }

    private static GameObject CreateBox(string name, Transform parent, Vector2 center, Vector2 size, int layer, Color color, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(center.x, center.y, 0f);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.layer = layer;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.color = color;

        go.AddComponent<BoxCollider2D>();
        return go;
    }

    private static GameObject CreateMarker(string name, Transform parent, Vector3 pos, Color color, Sprite sprite, string label)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;

        GameObject visual = new GameObject("Placeholder_Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(0.9f, 1.6f, 1f);
        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = -1;

        CreateTutorialPrompt("Label", go.transform, pos + new Vector3(0f, 1.5f, 0f), label, 22, new Vector2(320f, 60f));
        return go;
    }

    private static void CreateTutorialPrompt(string name, Transform parent, Vector3 worldPos, string textContent, int fontSize = 24, Vector2? panelSize = null)
    {
        Vector2 size = panelSize ?? new Vector2(600f, 145f);

        GameObject canvasGo = new GameObject(name);
        canvasGo.transform.SetParent(parent, false);
        canvasGo.transform.position = worldPos;
        canvasGo.transform.localScale = new Vector3(0.015f, 0.015f, 1f);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = size;

        // Subtle dark backdrop for readability
        GameObject bgGo = new GameObject("Backdrop");
        bgGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.05f, 0.06f, 0.08f, 0.72f);

        // Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 10f);
        textRect.offsetMax = new Vector2(-14f, -10f);

        Text uiText = textGo.AddComponent<Text>();
        uiText.text = textContent;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.fontSize = fontSize;
        uiText.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void SetupCameraConfiner(Scene spiritScene, Vector2[] points)
    {
        GameObject confinerGo = FindRootInScene(spiritScene, "CameraConfinerBounds");
        if (confinerGo == null)
        {
            confinerGo = new GameObject("CameraConfinerBounds");
            SceneManager.MoveGameObjectToScene(confinerGo, spiritScene);
        }
        confinerGo.layer = 2; // Ignore Raycast
        confinerGo.transform.position = Vector3.zero;

        PolygonCollider2D polyCol = confinerGo.GetComponent<PolygonCollider2D>();
        if (polyCol == null) polyCol = confinerGo.AddComponent<PolygonCollider2D>();
        polyCol.isTrigger = true;
        polyCol.points = points;
    }

    private static void SetupSpawnPoint(Scene spiritScene, Vector3 position)
    {
        GameObject spawnGo = FindRootInScene(spiritScene, "SpawnPoint_Default");
        if (spawnGo == null)
        {
            spawnGo = new GameObject("SpawnPoint_Default");
            SceneManager.MoveGameObjectToScene(spawnGo, spiritScene);
        }
        spawnGo.tag = "Respawn";
        spawnGo.transform.position = position;
        if (spawnGo.GetComponent<SpawnPoint>() == null)
        {
            spawnGo.AddComponent<SpawnPoint>();
        }
    }

    private static GameObject FindRootInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }

    private static void DestroyIfInScene(Scene scene, string name)
    {
        GameObject go = FindRootInScene(scene, name);
        if (go != null)
        {
            Undo.DestroyObjectImmediate(go);
        }
    }

    private static int GetLayerOrDefault(string layerName, int fallback)
    {
        int layer = LayerMask.NameToLayer(layerName);
        return layer == -1 ? fallback : layer;
    }

    private static Sprite LoadSquareSprite()
    {
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.2d.sprite/Editor/ObjectMenuCreation/DefaultAssets/Textures/v2/Square.png");
        if (s != null) return s;

        string[] guids = AssetDatabase.FindAssets("t:Sprite Square");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }
}

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

/// <summary>
/// Quản lý việc nạp và dỡ bỏ scene theo kiến trúc Multi-Scene Metroidvania:
/// Core_Scene (Persistent Scene) chứa Player, Main Camera, CM Camera, UI, GameManager.
/// Các Gameplay Scene (như SpiritRealm_Scene) chứa Map, Quái, Ánh sáng, Bounds và SpawnPoint.
/// </summary>
[DisallowMultipleComponent]
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    public const string CoreSceneName = "Core_Scene";
    public const string SpiritRealmSceneName = "SpiritRealm_Scene";

    public static event Action<string> OnSceneLoadStarted;
    public static event Action<string> OnSceneLoadCompleted;
    public static event Action<string> OnSceneUnloadStarted;
    public static event Action<string> OnSceneUnloadCompleted;

    [Header("Settings")]
    [Tooltip("Scene gameplay mặc định được nạp khi khởi chạy game")]
    [SerializeField] private string defaultGameplayScene = SpiritRealmSceneName;
    [SerializeField] private bool autoLoadDefaultOnStart = true;

    [Header("Persistent References (Optional - Auto resolved if empty)")]
    [SerializeField] private Transform persistentPlayer;
    [SerializeField] private CinemachineConfiner2D cmConfiner;

    [Header("Runtime Status")]
    [SerializeField] private string activeGameplayScene;
    public string ActiveGameplayScene => activeGameplayScene;
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        ResolvePersistentReferences();

        if (autoLoadDefaultOnStart)
        {
            // Kiểm tra xem đã có gameplay scene nào đang nạp chưa (khi mở Multi-scene trong Editor)
            bool hasGameplayScene = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.name != CoreSceneName && scene.isLoaded)
                {
                    hasGameplayScene = true;
                    activeGameplayScene = scene.name;
                    OnRoomSetup(scene, "Default");
                    break;
                }
            }

            // Nếu chỉ có Core_Scene đang mở, nạp scene gameplay mặc định (SpiritRealm_Scene)
            if (!hasGameplayScene && !string.IsNullOrEmpty(defaultGameplayScene))
            {
                LoadGameplayScene(defaultGameplayScene, "Default");
            }
            else
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Booting)
                {
                    GameManager.Instance.SetState(GameState.Gameplay);
                }
            }
        }
    }

    public void ResolvePersistentReferences()
    {
        if (persistentPlayer == null)
        {
            if (Player.Instance != null)
            {
                persistentPlayer = Player.Instance.transform;
            }
            else
            {
                GameObject playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) persistentPlayer = playerGo.transform;
            }
        }

        if (cmConfiner == null)
        {
            cmConfiner = FindFirstObjectByType<CinemachineConfiner2D>();
        }
    }

    /// <summary>
    /// Nạp một gameplay scene mới theo chế độ Additive, đưa Player tới vị trí spawnPointID chỉ định.
    /// </summary>
    public void LoadGameplayScene(string sceneName, string targetSpawnPointID = "Default", Action onComplete = null)
    {
        if (IsLoading)
        {
            Debug.LogWarning($"[SceneLoader] Currently loading another scene. Cannot load '{sceneName}'.");
            return;
        }

        StartCoroutine(LoadGameplaySceneRoutine(sceneName, targetSpawnPointID, onComplete));
    }

    private IEnumerator LoadGameplaySceneRoutine(string sceneName, string targetSpawnPointID, Action onComplete)
    {
        IsLoading = true;
        OnSceneLoadStarted?.Invoke(sceneName);

        // 1. Dỡ scene gameplay cũ nếu có và khác scene mới
        if (!string.IsNullOrEmpty(activeGameplayScene) && activeGameplayScene != sceneName)
        {
            Scene oldScene = SceneManager.GetSceneByName(activeGameplayScene);
            if (oldScene.isLoaded)
            {
                OnSceneUnloadStarted?.Invoke(activeGameplayScene);
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(oldScene);
                while (unloadOp != null && !unloadOp.isDone)
                {
                    yield return null;
                }
                OnSceneUnloadCompleted?.Invoke(activeGameplayScene);
            }
        }

        // 2. Nạp scene mới theo chế độ Additive
        Scene targetScene = SceneManager.GetSceneByName(sceneName);
        if (!targetScene.isLoaded)
        {
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (loadOp != null && !loadOp.isDone)
            {
                yield return null;
            }
        }

        // 3. Thiết lập Room: Spawn Player và cập nhật Camera Confiner
        targetScene = SceneManager.GetSceneByName(sceneName);
        if (targetScene.isLoaded)
        {
            OnRoomSetup(targetScene, targetSpawnPointID);
        }

        IsLoading = false;
        OnSceneLoadCompleted?.Invoke(sceneName);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Gameplay);
        }

        onComplete?.Invoke();
    }

    private void OnRoomSetup(Scene roomScene, string targetSpawnPointID)
    {
        SceneManager.SetActiveScene(roomScene);
        activeGameplayScene = roomScene.name;
        ResolvePersistentReferences();

        // 1. Đặt vị trí Player theo SpawnPoint trong room mới
        SpawnPoint targetSpawn = null;
        SpawnPoint fallbackSpawn = null;
        GameObject[] rootObjects = roomScene.GetRootGameObjects();

        foreach (var root in rootObjects)
        {
            var spawns = root.GetComponentsInChildren<SpawnPoint>(true);
            foreach (var sp in spawns)
            {
                if (fallbackSpawn == null) fallbackSpawn = sp;
                if (!string.IsNullOrEmpty(targetSpawnPointID) && sp.PointID.Equals(targetSpawnPointID, StringComparison.OrdinalIgnoreCase))
                {
                    targetSpawn = sp;
                    break;
                }
            }
            if (targetSpawn != null) break;
        }

        SpawnPoint chosenSpawn = targetSpawn ?? fallbackSpawn;
        if (chosenSpawn != null && persistentPlayer != null)
        {
            persistentPlayer.position = chosenSpawn.transform.position;
            Rigidbody2D rb = persistentPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log($"[SceneLoader] Positioned Player at SpawnPoint: '{chosenSpawn.PointID}' ({chosenSpawn.transform.position}) in '{roomScene.name}'.");
        }

        // 2. Cập nhật Camera Confiner theo bounds của room mới
        Collider2D roomConfiner = null;
        foreach (var root in rootObjects)
        {
            if (root.name.Contains("Confiner") || root.CompareTag("CameraConfiner"))
            {
                var col = root.GetComponent<Collider2D>();
                if (col != null)
                {
                    roomConfiner = col;
                    break;
                }
            }
        }

        if (roomConfiner == null)
        {
            // Fallback: tìm bất kỳ PolygonCollider2D trigger nào trong scene
            foreach (var root in rootObjects)
            {
                var cols = root.GetComponentsInChildren<PolygonCollider2D>(true);
                foreach (var c in cols)
                {
                    if (c.isTrigger)
                    {
                        roomConfiner = c;
                        break;
                    }
                }
                if (roomConfiner != null) break;
            }
        }

        if (cmConfiner != null && roomConfiner != null)
        {
            cmConfiner.BoundingShape2D = roomConfiner;
            cmConfiner.InvalidateBoundingShapeCache();
            Debug.Log($"[SceneLoader] Linked CinemachineConfiner2D to '{roomConfiner.gameObject.name}' in '{roomScene.name}'.");
        }

        Debug.Log($"[SceneLoader] Room setup complete for: '{roomScene.name}'. Set as Active Scene.");
    }

    /// <summary>
    /// Nạp lại gameplay scene hiện tại.
    /// </summary>
    public void ReloadCurrentGameplayScene(string spawnPointID = "Default", Action onComplete = null)
    {
        if (!string.IsNullOrEmpty(activeGameplayScene))
        {
            LoadGameplayScene(activeGameplayScene, spawnPointID, onComplete);
        }
    }
}

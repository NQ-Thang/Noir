using UnityEngine;

/// <summary>
/// Cổng chuyển phòng (Door / Transition Trigger) dùng cho kiến trúc Multi-Scene (1 Room = 1 Scene).
/// Khi Player đi vào vùng Trigger, tự động gọi SceneLoader để unload room cũ và load room mới.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour
{
    [Header("Target Room Settings")]
    [Tooltip("Tên Scene đích trong Build Settings (VD: SpiritRealm_Room2)")]
    [SerializeField] private string targetSceneName;

    [Tooltip("PointID của SpawnPoint ở Scene đích (VD: Door_Left, Default)")]
    [SerializeField] private string targetSpawnPointID = "Default";

    private bool isTriggered = false;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isTriggered) return;
        if (!other.CompareTag("Player")) return;

        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.Log($"[RoomTransition] '{gameObject.name}' chưa gắn targetSceneName (chờ tạo Room tiếp theo).");
            return;
        }

        if (SceneLoader.Instance == null || SceneLoader.Instance.IsLoading) return;

        if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            Debug.LogWarning($"[RoomTransition] Scene '{targetSceneName}' chưa có trong Build Settings!");
            return;
        }

        isTriggered = true;
        SceneLoader.Instance.LoadGameplayScene(targetSceneName, targetSpawnPointID);
    }

    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}

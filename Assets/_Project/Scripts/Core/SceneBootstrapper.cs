using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hỗ trợ quy trình làm việc trong Unity Editor và khởi động game:
/// Nếu lập trình viên bấm Play trực tiếp từ SpiritRealm_Scene (hoặc bất kỳ level scene nào),
/// bootstrapper sẽ tự động nạp Core_Scene (Persistent Scene) ở chế độ Additive trong nền.
/// </summary>
public static class SceneBootstrapper
{
    private const string CoreSceneName = "Core_Scene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        // 1. Nếu GameManager đã tồn tại trong scene (tức Core_Scene đã có sẵn) -> không nạp lại
        if (GameManager.Instance != null || Object.FindFirstObjectByType<GameManager>() != null)
        {
            return;
        }

        // 2. Kiểm tra danh sách tất cả scene đang có (kể cả khi đang mở Multi-Scene trong Editor)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (s.name == CoreSceneName)
            {
                return;
            }
        }

        // 3. Chỉ tự động nạp Core_Scene khi người dùng Play trực tiếp từ một level scene đơn lẻ
        if (Application.CanStreamedLevelBeLoaded(CoreSceneName))
        {
            Debug.Log($"[SceneBootstrapper] Auto-loading Persistent Core Scene ('{CoreSceneName}') additively...");
            SceneManager.LoadScene(CoreSceneName, LoadSceneMode.Additive);
        }
    }
}

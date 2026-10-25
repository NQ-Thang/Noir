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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Nếu scene khởi động chính đã là Core_Scene thì không cần làm gì thêm
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name == CoreSceneName)
        {
            return;
        }

        // Nếu khởi động từ SpiritRealm_Scene hoặc level khác, kiểm tra xem Core_Scene đã được nạp chưa
        Scene coreScene = SceneManager.GetSceneByName(CoreSceneName);
        if (!coreScene.isLoaded && Application.CanStreamedLevelBeLoaded(CoreSceneName))
        {
            Debug.Log($"[SceneBootstrapper] Auto-loading Persistent Core Scene ('{CoreSceneName}') additively...");
            SceneManager.LoadScene(CoreSceneName, LoadSceneMode.Additive);
        }
    }
}

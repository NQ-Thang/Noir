using UnityEngine;

/// <summary>
/// Đánh dấu vị trí xuất hiện của nhân vật (Mặc) trong màn chơi / phòng.
/// Mỗi phòng có thể có nhiều SpawnPoint tương ứng với các cửa vào (ví dụ: Default, Door_Left, Door_Right).
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Tên định danh cho vị trí xuất hiện (VD: Default, Door_West, Door_East)")]
    [SerializeField] private string pointID = "Default";
    public string PointID => pointID;

    [Header("Gizmo Display")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
    [SerializeField] private float gizmoRadius = 0.4f;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, gizmoRadius * 0.5f);
    }
}

using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    public float minDistance = 1.0f;  // Khoảng cách gần nhất (Zoom sát lưng)
    public float maxDistance = 4.0f;  // Khoảng cách xa nhất
    public float smooth = 10.0f;      // Tốc độ thụt ra thụt vào
    public LayerMask collisionLayer;  // Đặt là Default hoặc Environment (Layer của tường/đất)

    private Vector3 _direction;
    private float _currentDistance;

    void Awake()
    {
        // Ghi nhớ hướng của Camera so với cục cha (Pivot)
        _direction = transform.localPosition.normalized;
        _currentDistance = maxDistance;
    }

    void LateUpdate()
    {
        // Điểm gốc (đầu nhân vật)
        Vector3 pivotPos = transform.parent.position;
        // Điểm camera muốn nằm tới
        Vector3 desiredCameraPos = pivotPos + _direction * maxDistance;

        RaycastHit hit;
        // Bắn tia từ đầu nhân vật tới camera xem có vướng tường không
        if (Physics.Linecast(pivotPos, desiredCameraPos, out hit, collisionLayer))
        {
            // Nếu đụng tường -> Ép khoảng cách thu ngắn lại (cách tường 0.2f để không bị lẹm)
            _currentDistance = Mathf.Clamp(hit.distance - 0.2f, minDistance, maxDistance);
        }
        else
        {
            // Không vướng gì -> Nhả lò xo ra max
            _currentDistance = maxDistance;
        }

        // Di chuyển camera mượt mà
        transform.localPosition = Vector3.Lerp(transform.localPosition, _direction * _currentDistance, Time.deltaTime * smooth);
    }
}
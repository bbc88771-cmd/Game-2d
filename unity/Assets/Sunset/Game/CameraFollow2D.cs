using UnityEngine;

namespace Sunset.Game
{
    /// <summary>
    /// Плавно ведёт камеру за целью (вид сверху). Держит фиксированный Z, чтобы
    /// ортокамера смотрела на плоскость спрайтов. Можно ограничить рамкой острова.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;

        [Tooltip("Сглаживание следования (меньше — резче).")]
        public float smoothTime = 0.15f;

        [Tooltip("Ограничивать камеру рамкой (мир-координаты).")]
        public bool useBounds = false;
        public Vector2 boundsMin, boundsMax;

        private Vector3 _vel;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);

            if (useBounds)
            {
                var cam = GetComponent<Camera>();
                if (cam != null && cam.orthographic)
                {
                    float halfH = cam.orthographicSize;
                    float halfW = halfH * cam.aspect;
                    // не показываем за рамкой острова (если он больше вьюпорта)
                    if (boundsMax.x - boundsMin.x > halfW * 2f)
                        goal.x = Mathf.Clamp(goal.x, boundsMin.x + halfW, boundsMax.x - halfW);
                    else goal.x = (boundsMin.x + boundsMax.x) * 0.5f;
                    if (boundsMax.y - boundsMin.y > halfH * 2f)
                        goal.y = Mathf.Clamp(goal.y, boundsMin.y + halfH, boundsMax.y - halfH);
                    else goal.y = (boundsMin.y + boundsMax.y) * 0.5f;
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _vel, smoothTime);
        }
    }
}

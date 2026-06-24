using UnityEngine;
using Sunset.Core;

namespace Sunset.Game
{
    /// <summary>
    /// Управление персонажем «вид сверху» (прототип стартового острова). Физика —
    /// Unity Rigidbody2D без гравитации; направление и скорость считает чистый
    /// <see cref="Movement2D"/>. Параметры вынесены в инспектор, чтобы тюнинг шёл
    /// без правки кода. Графику (спрайт/анимации) можно заменить, не трогая логику.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class PlayerController2D : MonoBehaviour
    {
        [Tooltip("Скорость передвижения (единиц/сек).")]
        public float speed = 5f;

        [Tooltip("Отражать спрайт по горизонтали по направлению движения.")]
        public bool flipSprite = true;

        private Rigidbody2D _rb;
        private SpriteRenderer _sprite;

        /// <summary>Текущая скорость (для камеры/логики/триггеров).</summary>
        public Vector2 Velocity { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void FixedUpdate()
        {
            float ix = Input.GetAxisRaw("Horizontal");
            float iy = Input.GetAxisRaw("Vertical");
            Movement2D.Desired(ix, iy, speed, out float vx, out float vy);
            Velocity = new Vector2(vx, vy);
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = Velocity;
#else
            _rb.velocity = Velocity;
#endif
            if (flipSprite && _sprite != null && Mathf.Abs(vx) > 0.01f)
                _sprite.flipX = vx < 0f;
        }
    }
}

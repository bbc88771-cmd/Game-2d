using UnityEngine;
using Sunset.Core;

namespace Sunset.Game
{
    /// <summary>
    /// Простой враг-преследователь (заглушка ИИ): если игрок в радиусе агро —
    /// движется к нему через тот же <see cref="Movement2D"/>, иначе стоит. Без урона
    /// (прототип). Демонстрирует «шов» для будущего ИИ и триггеров «Некого».
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [DisallowMultipleComponent]
    public class EnemyStub : MonoBehaviour
    {
        public Transform target;
        public float speed = 1.8f;
        public float aggroRange = 6f;

        private Rigidbody2D _rb;
        private SpriteRenderer _sprite;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _sprite = GetComponentInChildren<SpriteRenderer>();
        }

        private void FixedUpdate()
        {
            float vx = 0f, vy = 0f;
            if (target != null)
            {
                Vector2 to = (Vector2)(target.position - transform.position);
                if (to.sqrMagnitude <= aggroRange * aggroRange && to.sqrMagnitude > 0.04f)
                {
                    to.Normalize();
                    Movement2D.Desired(to.x, to.y, speed, out vx, out vy);
                }
            }
            var v = new Vector2(vx, vy);
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = v;
#else
            _rb.velocity = v;
#endif
            if (_sprite != null && Mathf.Abs(vx) > 0.01f) _sprite.flipX = vx < 0f;
        }
    }
}

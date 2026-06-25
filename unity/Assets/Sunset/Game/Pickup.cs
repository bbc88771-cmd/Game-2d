using System;
using UnityEngine;

namespace Sunset.Game
{
    /// <summary>
    /// Подбираемый предмет: при входе игрока сообщает о сборе и исчезает. Сама
    /// «ценность»/тип — данные для будущей экономики; пока просто счётчик находок.
    /// </summary>
    [DisallowMultipleComponent]
    public class Pickup : MonoBehaviour
    {
        /// <summary>Вызывается, когда игрок подобрал предмет.</summary>
        public Action<Pickup> Collected;

        /// <summary>Какой ресурс и сколько даёт (для строительства).</summary>
        public string resource;
        public int amount;

        private bool _taken;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_taken) return;
            var player = other.GetComponent<PlayerController2D>();
            if (player == null && other.attachedRigidbody != null)
                player = other.attachedRigidbody.GetComponent<PlayerController2D>();
            if (player == null) return;

            _taken = true;
            Collected?.Invoke(this);
            Destroy(gameObject);
        }
    }
}

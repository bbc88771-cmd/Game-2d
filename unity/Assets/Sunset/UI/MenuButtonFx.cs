using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sunset.UI
{
    /// <summary>
    /// Ховер-эффект пункта главного меню (порт .menu-btn из веб-CSS): при
    /// наведении текст уезжает вправо (аналог padding-left: 1.5em), а слева
    /// «вырастает» ромб-маркер (transform: rotate(45deg) scale(0→1)). Обе
    /// анимации ~0.18 с со сглаживанием, как transition в вебе.
    /// Ссылки на текст/ромб/кнопку задаёт SunsetMainMenu при сборке UI.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Сдвиг текста при наведении, px (в вебе — padding-left 1.5em).")]
        public float shift = 48f;

        [Tooltip("Длительность анимации, с (в вебе — transition .18s).")]
        public float duration = 0.18f;

        public RectTransform label;    // текст пункта (двигается вправо)
        public RectTransform diamond;  // ромб-маркер (масштаб 0→1)
        public Button button;          // выключенная кнопка не реагирует

        private float _t;              // прогресс 0..1
        private bool _hover;

        public void OnPointerEnter(PointerEventData e)
            => _hover = button == null || button.interactable;

        public void OnPointerExit(PointerEventData e) => _hover = false;

        private void OnDisable() { _hover = false; _t = 0f; Apply(0f); }

        private void Update()
        {
            float target = _hover ? 1f : 0f;
            if (Mathf.Approximately(_t, target)) return;
            _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));
            Apply(_t * _t * (3f - 2f * _t)); // smoothstep ≈ ease
        }

        private void Apply(float k)
        {
            if (label != null)
            {
                var p = label.anchoredPosition;
                p.x = shift * k;
                label.anchoredPosition = p;
            }
            if (diamond != null) diamond.localScale = new Vector3(k, k, 1f);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Присутствие «Некого» во время игры (главный канал общения). Показывает редкие
    /// короткие реплики ненавязчивым «шёпотом» в углу экрана: спонтанно (с большими
    /// случайными паузами) и по триггерам события через <see cref="Trigger"/>.
    /// Не перехватывает ввод и сам угасает — чтобы игрок иногда о нём забывал.
    ///
    /// Логика выбора реплик — в <see cref="NekoAmbient"/>. Повесь на объект игровой
    /// сцены; для событий вызывай <c>Trigger("night")</c> и т.п.
    /// </summary>
    [DisallowMultipleComponent]
    public class NekoPresence : MonoBehaviour
    {
        [Tooltip("Запускать спонтанные реплики по таймеру.")]
        public bool spontaneous = true;

        [Tooltip("Сколько секунд держится реплика на экране.")]
        public float holdSeconds = 5.5f;

        private static readonly Color ColNeko = new Color(1f, 0.45f, 0.42f, 1f);

        private NekoState _state;
        private TextMeshProUGUI _label;
        private CanvasGroup _group;
        private readonly System.Random _rng = new System.Random();
        private readonly List<string> _recent = new List<string>();
        private float _lastShownTime = -999f;
        private Coroutine _fade;

        private void Awake()
        {
            _state = SunsetSave.Load();
            BuildUi();
        }

        private void Start()
        {
            if (spontaneous) StartCoroutine(SpontaneousLoop());
        }

        // ---------- публичный API ----------

        /// <summary>Показать реплику по ключу события (night/wounded/boss/…).</summary>
        public void Trigger(string key)
        {
            if (Time.time - _lastShownTime < NekoAmbient.TriggerMinGap) return; // не частим
            string line = NekoAmbient.ForTrigger(key, _recent, _rng);
            if (!string.IsNullOrEmpty(line)) Show(line);
        }

        /// <summary>Показать произвольную реплику (например, из NekoBrain).</summary>
        public void Say(string line)
        {
            if (!string.IsNullOrEmpty(line)) Show(line);
        }

        // ---------- внутреннее ----------

        private IEnumerator SpontaneousLoop()
        {
            yield return new WaitForSeconds(NekoAmbient.FirstDelay(_rng));
            while (true)
            {
                // не перебиваем недавнюю реплику и не частим
                if (Time.time - _lastShownTime >= NekoAmbient.TriggerMinGap)
                {
                    string line = NekoAmbient.Spontaneous(_state, _recent, _rng);
                    if (!string.IsNullOrEmpty(line)) Show(line);
                }
                yield return new WaitForSeconds(NekoAmbient.NextSpontaneousDelay(_rng));
            }
        }

        private void Show(string line)
        {
            _lastShownTime = Time.time;
            Remember(line);
            _label.text = line;
            if (_fade != null) StopCoroutine(_fade);
            _fade = StartCoroutine(FadeCycle());
        }

        private void Remember(string line)
        {
            _recent.Add(line);
            if (_recent.Count > 4) _recent.RemoveAt(0); // анти-повтор: помним последние 4
        }

        private IEnumerator FadeCycle()
        {
            yield return Fade(_group, 1f, 0.6f);
            yield return new WaitForSeconds(holdSeconds);
            yield return Fade(_group, 0f, 1.2f);
        }

        private IEnumerator Fade(CanvasGroup g, float to, float dur)
        {
            float from = g.alpha, t = 0f;
            while (t < dur) { g.alpha = Mathf.Lerp(from, to, t / dur); t += Time.deltaTime; yield return null; }
            g.alpha = to;
        }

        // ---------- построение UI ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("NekoPresenceCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // поверх игры, но это лишь текст
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.GetComponent<RectTransform>();

            // ненавязчивая реплика снизу по центру
            var go = new GameObject("NekoLine", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0, 120);
            rt.sizeDelta = new Vector2(1100, 90);

            _group = go.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false; _group.blocksRaycasts = false;

            _label = go.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 28;
            _label.color = ColNeko;
            _label.fontStyle = FontStyles.Italic;
            _label.alignment = TextAlignmentOptions.Bottom;
            _label.enableWordWrapping = true;
            _label.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null) _label.font = TMP_Settings.defaultFontAsset;
        }
    }
}

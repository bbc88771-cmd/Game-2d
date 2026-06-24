using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Вступительная катсцена (фаза 3 порта): 4 нарративных слайда с эффектом
    /// Ken Burns, поднимающимися угольками, вспышкой на смене кадра и текстовыми
    /// битами, которые сменяют друг друга. Весь UI строится из кода (как в
    /// <see cref="SunsetDialogue"/>), чтобы не хранить хрупкие .unity/.prefab.
    ///
    /// Данные слайдов — в <see cref="CutsceneData"/>. Фоновые спрайты грузятся из
    /// Resources/Sunset/&lt;image&gt;; если спрайта нет — кадр заливается тёмным
    /// цветом (катсцена остаётся проходимой без ассетов).
    ///
    /// Сборку сцены одним кликом см. Sunset → Build Cutscene Scene (редактор).
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetCutscene : MonoBehaviour
    {
        [Tooltip("Длительность катсцены (сек), если озвучка не задаёт её сама.")]
        public float fallbackDurationSec = 0f; // 0 → взять сумму CutsceneData.FallbackDurationsMs

        [Tooltip("Сцена после катсцены (пусто — остаться). По умолчанию — выбор героя.")]
        public string nextScene = SceneFlow.HeroSelect;

        /// <summary>Вызывается, когда катсцена доиграла или была пропущена.</summary>
        public event Action Finished;

        // палитра
        private static readonly Color ColBlack = new Color(0f, 0f, 0f, 1f);
        private static readonly Color ColMissing = new Color(0.06f, 0.05f, 0.05f, 1f);
        private static readonly Color ColCaption = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColNekoVoice = new Color(1f, 0.23f, 0.23f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.81f, 0.48f, 1f);

        private CanvasGroup _group;
        private RectTransform _frame;
        private Image _frameImg;
        private Image _flash;
        private TextMeshProUGUI _caption;
        private RectTransform _embersRoot;
        private SunsetAudio _audio;

        private bool _skipped;
        private bool _done;

        private void Start()
        {
            BuildUi();
            _audio = gameObject.AddComponent<SunsetAudio>();
            _audio.StartAmbient(0.18f); // приглушённый дрон под катсцену (как в вебе)
            StartCoroutine(Play());
        }

        // ---------- проигрывание ----------

        private IEnumerator Play()
        {
            SpawnEmbers(_embersRoot, 26);

            double totalMs = fallbackDurationSec > 0f
                ? fallbackDurationSec * 1000.0
                : SumFallback();
            int[] slideDur = CutsceneData.SlideDurationsMs(totalMs);

            for (int si = 0; si < CutsceneData.Count; si++)
            {
                if (_skipped) break;
                var slide = CutsceneData.Slides[si];

                // смена фона: затемнить → подставить → проявить
                yield return Fade(_frameImg, 0f, 0.3f);
                if (_skipped) break;
                ApplyFrame(slide);
                StartCoroutine(KenBurns(slide.kenBurnsOut, 4f));
                yield return Fade(_frameImg, 1f, 0.25f);

                // вспышка на смене кадра
                StartCoroutine(FlashOnce());
                if (_audio != null) _audio.Bell(slide.bellFreq);

                _caption.color = slide.neko ? ColNekoVoice : ColCaption;

                var beats = slide.beats;
                float beatDur = Mathf.Max(3.5f, slideDur[si] / 1000f / Mathf.Max(1, beats.Length));

                for (int bi = 0; bi < beats.Length; bi++)
                {
                    if (_skipped) break;
                    _caption.text = beats[bi];
                    yield return ShowCaption(true);
                    yield return WaitOrSkip(beatDur);
                    if (bi < beats.Length - 1 && !_skipped)
                        yield return ShowCaption(false);
                }

                if (!_skipped && si < CutsceneData.Count - 1)
                {
                    yield return ShowCaption(false);
                    yield return WaitOrSkip(0.6f);
                }
            }

            // финал: убрать подпись и плавно затемнить весь экран
            yield return ShowCaption(false);
            yield return WaitOrSkip(0.6f);
            yield return FadeGroup(_group, 0f, 0.9f);

            Finish();
        }

        private void Finish()
        {
            if (_done) return;
            _done = true;
            if (_audio != null) _audio.StopAmbient();
            Debug.Log("[Sunset] Катсцена завершена.");
            Finished?.Invoke();
            if (!string.IsNullOrEmpty(nextScene)) SceneFlow.Go(nextScene);
        }

        public void Skip()
        {
            if (_done) return;
            _skipped = true;
        }

        private double SumFallback()
        {
            double s = 0;
            foreach (var v in CutsceneData.FallbackDurationsMs) s += v;
            return s;
        }

        private void ApplyFrame(CutsceneSlide slide)
        {
            _frame.localScale = Vector3.one;
            _frame.anchoredPosition = Vector2.zero;

            Sprite sprite = string.IsNullOrEmpty(slide.image)
                ? null
                : Resources.Load<Sprite>("Sunset/" + slide.image);

            if (sprite != null)
            {
                _frameImg.sprite = sprite;
                _frameImg.color = Color.white;
                _frameImg.type = Image.Type.Simple;
                _frameImg.preserveAspect = false;
            }
            else
            {
                _frameImg.sprite = null;
                // null-кадр (космос) — чёрный; отсутствующий спрайт — тёмная заливка
                _frameImg.color = string.IsNullOrEmpty(slide.image) ? ColBlack : ColMissing;
            }
        }

        // ---------- эффекты ----------

        private IEnumerator KenBurns(bool outward, float dur)
        {
            // kb (наезд): scale 1.0→1.13, сдвиг к (-2.5%,-1.5%)
            // kb2 (отъезд): scale 1.12→1.0, сдвиг от (2%,1.5%) к нулю
            float fromS = outward ? 1.12f : 1.0f;
            float toS = outward ? 1.0f : 1.13f;
            Vector2 fromP = outward ? new Vector2(0.02f, 0.015f) : Vector2.zero;
            Vector2 toP = outward ? Vector2.zero : new Vector2(-0.025f, -0.015f);

            float w = _frame.rect.width, h = _frame.rect.height;
            float t = 0f;
            while (t < dur && !_skipped)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dur);
                float s = Mathf.Lerp(fromS, toS, k);
                _frame.localScale = new Vector3(s, s, 1f);
                Vector2 p = Vector2.Lerp(fromP, toP, k);
                _frame.anchoredPosition = new Vector2(p.x * w, p.y * h);
                t += Time.deltaTime;
                yield return null;
            }
            _frame.localScale = new Vector3(toS, toS, 1f);
        }

        private IEnumerator FlashOnce()
        {
            // 0 → 0.4 (12%) → 0 за 0.7с
            const float dur = 0.7f;
            float t = 0f;
            while (t < dur)
            {
                float k = t / dur;
                float a = k < 0.12f ? Mathf.Lerp(0f, 0.4f, k / 0.12f) : Mathf.Lerp(0.4f, 0f, (k - 0.12f) / 0.88f);
                SetAlpha(_flash, a);
                t += Time.deltaTime;
                yield return null;
            }
            SetAlpha(_flash, 0f);
        }

        private IEnumerator ShowCaption(bool show)
        {
            // плавное появление/исчезновение подписи (как класс .show: opacity .9s)
            float dur = show ? 0.9f : 0.4f;
            float from = _caption.alpha;
            float to = show ? 1f : 0f;
            float t = 0f;
            while (t < dur && !_skipped)
            {
                _caption.alpha = Mathf.Lerp(from, to, t / dur);
                t += Time.deltaTime;
                yield return null;
            }
            _caption.alpha = to;
        }

        private void SpawnEmbers(RectTransform host, int n)
        {
            for (int i = 0; i < n; i++)
            {
                var go = NewUi("Ember", host);
                var rt = go.GetComponent<RectTransform>();
                float size = 2f + UnityEngine.Random.value * 4f;
                rt.anchorMin = rt.anchorMax = new Vector2(UnityEngine.Random.value, 0f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(size, size);

                var img = go.AddComponent<Image>();
                img.color = ColEmber;
                img.raycastTarget = false;

                float life = 3f + UnityEngine.Random.value * 4f;
                float delay = UnityEngine.Random.value * 6f;
                StartCoroutine(EmberRise(rt, img, life, delay));
            }
        }

        private IEnumerator EmberRise(RectTransform rt, Image img, float life, float delay)
        {
            yield return WaitOrSkip(delay);
            float riseH = _embersRoot.rect.height * 0.78f;
            while (!_done)
            {
                float t = 0f;
                while (t < life && !_done)
                {
                    float k = t / life;
                    float a = k < 0.12f ? Mathf.Lerp(0f, 0.85f, k / 0.12f) : Mathf.Lerp(0.85f, 0f, (k - 0.12f) / 0.88f);
                    SetAlpha(img, a);
                    float scale = Mathf.Lerp(1f, 0.3f, k);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    rt.anchoredPosition = new Vector2(0f, -12f + k * riseH);
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // ---------- хелперы анимации ----------

        private IEnumerator Fade(Graphic g, float to, float dur)
        {
            float from = g.color.a;
            float t = 0f;
            while (t < dur && !_skipped)
            {
                SetAlpha(g, Mathf.Lerp(from, to, t / dur));
                t += Time.deltaTime;
                yield return null;
            }
            SetAlpha(g, to);
        }

        private IEnumerator FadeGroup(CanvasGroup cg, float to, float dur)
        {
            float from = cg.alpha;
            float t = 0f;
            while (t < dur)
            {
                cg.alpha = Mathf.Lerp(from, to, t / dur);
                t += Time.deltaTime;
                yield return null;
            }
            cg.alpha = to;
        }

        private IEnumerator WaitOrSkip(float sec)
        {
            float t = 0f;
            while (t < sec && !_skipped)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        private static void SetAlpha(Graphic g, float a)
        {
            var c = g.color; c.a = a; g.color = c;
        }

        // ---------- построение UI ----------

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("CutsceneCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _group = canvasGo.AddComponent<CanvasGroup>();
            var root = canvasGo.GetComponent<RectTransform>();

            // чёрная подложка (на случай прозрачных краёв кадра)
            var back = NewUi("Backdrop", root);
            Stretch(back, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backImg = back.AddComponent<Image>();
            backImg.color = ColBlack;
            backImg.raycastTarget = false;

            // кадр (фон со слайдом + Ken Burns)
            var frameGo = NewUi("Frame", root);
            Stretch(frameGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _frame = frameGo.GetComponent<RectTransform>();
            _frameImg = frameGo.AddComponent<Image>();
            _frameImg.color = ColBlack;
            _frameImg.raycastTarget = false;

            // угольки
            var embersGo = NewUi("Embers", root);
            Stretch(embersGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _embersRoot = embersGo.GetComponent<RectTransform>();
            embersGo.AddComponent<RectMask2D>();

            // вспышка
            var flashGo = NewUi("Flash", root);
            Stretch(flashGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _flash = flashGo.AddComponent<Image>();
            _flash.color = new Color(1f, 1f, 1f, 0f);
            _flash.raycastTarget = false;

            // подпись (текстовые биты) — снизу по центру
            var capGo = NewUi("Caption", root);
            Anchored(capGo, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0, 150), new Vector2(1400, 360));
            _caption = capGo.AddComponent<TextMeshProUGUI>();
            _caption.fontSize = 40;
            _caption.color = ColCaption;
            _caption.alignment = TextAlignmentOptions.Bottom;
            _caption.enableWordWrapping = true;
            _caption.alpha = 0f;
            if (TMP_Settings.defaultFontAsset != null) _caption.font = TMP_Settings.defaultFontAsset;

            // кнопка «пропустить»
            var skipGo = NewUi("Skip", root);
            Anchored(skipGo, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 40), new Vector2(220, 64));
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(1f, 1f, 1f, 0.04f);
            var skipBtn = skipGo.AddComponent<Button>();
            skipBtn.onClick.AddListener(Skip);
            var skipLbl = NewUi("Lbl", skipGo.transform);
            Stretch(skipLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var skipTmp = skipLbl.AddComponent<TextMeshProUGUI>();
            skipTmp.text = "пропустить ▸";
            skipTmp.fontSize = 26;
            skipTmp.color = new Color(0.95f, 0.91f, 0.81f, 0.6f);
            skipTmp.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null) skipTmp.font = TMP_Settings.defaultFontAsset;
        }

        // ---------- мини-утилиты uGUI ----------

        private static GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        private static void Anchored(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }
    }
}

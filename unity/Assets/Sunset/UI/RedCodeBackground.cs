using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Полноэкранный «красный код» (фаза 7 порта): плотный поток псевдо-кода,
    /// который очень быстро перематывается и в случайных местах резко вспыхивает
    /// «найденными» фрагментами — эффект лихорадочного поиска. Во время «скана»
    /// поток ярче, в нём мелькают личные данные игрока, а поверх всплывает читаемое
    /// «досье» (имя, время, откуда, система…) — будто «Некий» сканирует тебя.
    ///
    /// Создаёт собственный Canvas с низким sortingOrder, поэтому ложится позади
    /// прочего UI. Логика строк — в <see cref="CodeStream"/>, досье — в
    /// <see cref="PlayerScan"/>. Демо-сцена: Sunset → Build Red Code Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class RedCodeBackground : MonoBehaviour
    {
        [Tooltip("Базовая яркость фонового потока (0..1).")]
        public float baseOpacity = 0.16f;

        [Tooltip("Размер шрифта кода.")]
        public int fontSize = 22;

        [Tooltip("Запускать собственный планировщик случайных сканов.")]
        public bool autoScan = false;

        private static readonly Color ColDim = new Color(1f, 0.18f, 0.18f, 1f);
        private static readonly Color ColBright = new Color(1f, 0.55f, 0.5f, 1f);

        private CodeStream _stream;
        private TextMeshProUGUI _code;
        private CanvasGroup _group;
        private TextMeshProUGUI _readout;
        private CanvasGroup _readoutGroup;

        private string[] _lines;
        private int _cols, _rows;
        private float _accum;
        private bool _scanning;
        private string[] _hits = Array.Empty<string>();
        private System.Random _rng = new System.Random();
        private Coroutine _scanCo, _schedulerCo;
        private Func<ScanInput> _probe;

        private void Awake()
        {
            _stream = new CodeStream(_rng);
            BuildUi();
            RebuildGrid();
        }

        private void Start()
        {
            if (autoScan && _probe == null) _probe = DefaultProbe;
            if (autoScan) StartScheduler();
        }

        /// <summary>Источник данных игрока для скана (имя/время/система…).</summary>
        public void SetProbe(Func<ScanInput> probe) => _probe = probe;

        public void SetOpacity(float o)
        {
            baseOpacity = Mathf.Clamp01(o);
            if (!_scanning && _group != null) _group.alpha = baseOpacity;
        }

        // ---------- поток ----------

        private void Update()
        {
            _accum += Time.deltaTime;
            if (_accum < 0.028f) return; // ~30 кадров/с — очень быстрый скролл
            _accum = 0f;

            int shift = (_scanning ? 2 : 1) + _rng.Next(4);
            for (int i = 0; i < shift; i++)
            {
                Array.Copy(_lines, 1, _lines, 0, _lines.Length - 1);
                _lines[_lines.Length - 1] = _stream.Line(_cols);
            }
            RenderText();
        }

        private void RenderText()
        {
            double flashP = _scanning ? 0.13 : 0.06;
            double hitP = _scanning && _hits.Length > 0 ? 0.12 : 0.0;

            var sb = new StringBuilder(_rows * (_cols + 12));
            for (int i = 0; i < _lines.Length; i++)
            {
                string ln = _lines[i];
                if (hitP > 0 && _rng.NextDouble() < hitP)
                {
                    string hit = _hits[_rng.Next(_hits.Length)];
                    int keep = Math.Min(hit.Length, ln.Length);
                    sb.Append("<b><color=#ff8c80>").Append(Esc(hit)).Append("</color></b>")
                      .Append(Esc(ln.Substring(keep)));
                }
                else if (_rng.NextDouble() < flashP)
                {
                    int x = (int)(_rng.NextDouble() * ln.Length * 0.7);
                    int w = 6 + _rng.Next(20);
                    if (x + w > ln.Length) w = Math.Max(0, ln.Length - x);
                    sb.Append(Esc(ln.Substring(0, x)))
                      .Append("<b><color=#ff8c80>").Append(Esc(ln.Substring(x, w))).Append("</color></b>")
                      .Append(Esc(ln.Substring(x + w)));
                }
                else
                {
                    sb.Append(Esc(ln));
                }
                if (i < _lines.Length - 1) sb.Append('\n');
            }
            _code.text = sb.ToString();
        }

        // экранируем спецсимволы rich text (TMP)
        private static string Esc(string s)
            => s.IndexOf('<') < 0 ? s : s.Replace("<", "<noparse><</noparse>");

        // ---------- скан ----------

        /// <summary>Короткий «бросок» яркости без скана (на dur секунд).</summary>
        public void Burst(float dur = 0.9f, float peak = 0.6f)
        {
            if (_group == null) return;
            StartCoroutine(BurstCo(dur, peak));
        }

        private IEnumerator BurstCo(float dur, float peak)
        {
            _group.alpha = peak;
            yield return new WaitForSeconds(dur);
            if (!_scanning) _group.alpha = baseOpacity;
        }

        /// <summary>
        /// Запускает скан: ~dur секунд поток ярче и плотнее, в коде мелькают личные
        /// данные, поверх всплывает читаемое «досье».
        /// </summary>
        public void ScanBurst(float dur = 5f)
        {
            var input = _probe != null ? _probe() : DefaultProbe();
            if (_scanCo != null) StopCoroutine(_scanCo);
            _scanCo = StartCoroutine(ScanCo(input, dur));
        }

        private IEnumerator ScanCo(ScanInput input, float dur)
        {
            _scanning = true;
            _hits = PlayerScan.Hits(input).ToArray();
            if (_group != null) _group.alpha = 0.5f;
            ShowReadout(input, dur);

            yield return new WaitForSeconds(dur);

            _scanning = false;
            _hits = Array.Empty<string>();
            if (_group != null) _group.alpha = baseOpacity;
            _scanCo = null;
        }

        private void ShowReadout(ScanInput input, float dur)
        {
            if (_readout == null) return;
            _readout.text = PlayerScan.ReadoutText(input);
            StartCoroutine(ReadoutCo(dur));
        }

        private IEnumerator ReadoutCo(float dur)
        {
            float t = 0f;
            while (t < 0.3f) { _readoutGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.3f); t += Time.deltaTime; yield return null; }
            _readoutGroup.alpha = 1f;
            yield return new WaitForSeconds(Mathf.Max(0f, dur - 0.6f));
            t = 0f;
            while (t < 0.3f) { _readoutGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.3f); t += Time.deltaTime; yield return null; }
            _readoutGroup.alpha = 0f;
        }

        // ---------- планировщик ----------

        public void StartScheduler()
        {
            if (_schedulerCo == null) _schedulerCo = StartCoroutine(Scheduler());
        }

        public void StopScheduler()
        {
            if (_schedulerCo != null) { StopCoroutine(_schedulerCo); _schedulerCo = null; }
        }

        private IEnumerator Scheduler()
        {
            // первый скан — через 4–10 с, дальше — раз в 12–28 с (как в веб-версии)
            yield return new WaitForSeconds(4f + (float)_rng.NextDouble() * 6f);
            while (true)
            {
                ScanBurst();
                yield return new WaitForSeconds(12f + (float)_rng.NextDouble() * 16f);
            }
        }

        // ---------- данные игрока по умолчанию ----------

        private static ScanInput DefaultProbe()
        {
            var s = new ScanInput { now = DateTime.Now };
            try
            {
                string tz = TimeZoneInfo.Local.Id;
                if (!string.IsNullOrEmpty(tz) && tz.Contains("/"))
                {
                    var parts = tz.Split('/');
                    s.region = parts[0];
                    s.city = parts[parts.Length - 1].Replace('_', ' ');
                }
            }
            catch { }
            s.os = OsName();
            s.device = Application.platform == RuntimePlatform.WebGLPlayer ? "браузер" : SystemInfo.deviceType.ToString();
            s.entry = "напрямую";
            s.screenW = Screen.width;
            s.screenH = Screen.height;
            s.lang = Application.systemLanguage.ToString();
            s.cpuThreads = SystemInfo.processorCount;
            return s;
        }

        private static string OsName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor: return "Windows";
                case RuntimePlatform.Android: return "Android";
                case RuntimePlatform.IPhonePlayer: return "iOS";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor: return "macOS";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor: return "Linux";
                case RuntimePlatform.WebGLPlayer: return "браузер";
                default: return "unknown";
            }
        }

        // ---------- построение UI ----------

        private void RebuildGrid()
        {
            // приблизительные размеры моноширинного глифа от кегля
            float charW = fontSize * 0.62f;
            float lineH = fontSize * 1.12f;
            // в системе координат канваса (reference 1920×1080)
            _cols = Mathf.CeilToInt(1920f / charW) + 4;
            _rows = Mathf.CeilToInt(1080f / lineH) + 4;
            _lines = new string[_rows];
            for (int i = 0; i < _rows; i++) _lines[i] = _stream.Line(_cols);
            RenderText();
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("RedCodeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100; // позади прочего UI
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.GetComponent<RectTransform>();

            var back = NewUi("Black", root);
            Stretch(back, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var backImg = back.AddComponent<Image>();
            backImg.color = new Color(0.02f, 0f, 0f, 1f);
            backImg.raycastTarget = false;

            var codeGo = NewUi("Code", root);
            Stretch(codeGo, Vector2.zero, Vector2.one, new Vector2(-40, -40), new Vector2(40, 40));
            _group = codeGo.AddComponent<CanvasGroup>();
            _group.alpha = baseOpacity;
            _group.interactable = false; _group.blocksRaycasts = false;
            _code = codeGo.AddComponent<TextMeshProUGUI>();
            _code.fontSize = fontSize;
            _code.color = ColDim;
            _code.richText = true;
            _code.enableWordWrapping = false;
            _code.overflowMode = TextOverflowModes.Overflow;
            _code.alignment = TextAlignmentOptions.TopLeft;
            _code.raycastTarget = false;
            ApplyMonoFont(_code);

            // панель «досье»
            var readoutGo = NewUi("Readout", root);
            Anchored(readoutGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(620, 420));
            _readoutGroup = readoutGo.AddComponent<CanvasGroup>();
            _readoutGroup.alpha = 0f;
            _readoutGroup.interactable = false; _readoutGroup.blocksRaycasts = false;
            var rbg = readoutGo.AddComponent<Image>();
            rbg.color = new Color(0.05f, 0f, 0f, 0.78f);
            rbg.raycastTarget = false;
            var rTxt = NewUi("Txt", readoutGo.transform);
            Stretch(rTxt, Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -24));
            _readout = rTxt.AddComponent<TextMeshProUGUI>();
            _readout.fontSize = 24;
            _readout.color = new Color(1f, 0.4f, 0.38f, 1f);
            _readout.alignment = TextAlignmentOptions.TopLeft;
            _readout.raycastTarget = false;
            ApplyMonoFont(_readout);
        }

        private static void ApplyMonoFont(TMP_Text t)
        {
            // моноширинный, если доступен; иначе обычный TMP-шрифт
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        }

        private static GameObject NewUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
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

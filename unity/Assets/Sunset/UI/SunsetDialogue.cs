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
    /// Играбельный экран диалога с «Неким» (фаза 2 порта). Строит весь UI из кода
    /// (чёрный фон, прокручиваемый чат, поле ввода) и гоняет беседу через
    /// <see cref="NekoBrain"/>: печатает ответы по буквам, реагирует на тон,
    /// сохраняет состояние. Повесь компонент на пустой объект в сцене и нажми Play.
    ///
    /// Сборку сцены одним кликом см. Sunset → Build Dialogue Scene (редактор).
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetDialogue : MonoBehaviour
    {
        [Tooltip("16 — мягкий тон, 18 — жёсткий.")]
        public string rating = "16";

        [Tooltip("Полноэкранный «красный код» + случайные сканы игрока (как в вебе).")]
        public bool enableRedCode = true;

        [Tooltip("Сцена по кнопке «далее ▸» (пусто — кнопку не показывать). По умолчанию — катсцена.")]
        public string nextScene = SceneFlow.Cutscene;

        private NekoState _state;
        private NekoBrain _brain;
        private RedCodeBackground _code;
        private SunsetAudio _audio;
        private Image _bgImg;

        private TextMeshProUGUI _chat;
        private ScrollRect _scroll;
        private TMP_InputField _input;
        private string _log = "";
        private bool _busy;

        // палитра
        private static readonly Color ColBg = new Color(0.04f, 0.03f, 0.03f, 1f);
        private static readonly Color ColNeko = new Color(1f, 0.42f, 0.42f, 1f);
        private static readonly Color ColPlayer = new Color(0.42f, 0.82f, 0.5f, 1f);

        private void Awake()
        {
            _state = SunsetSave.Load();
            _state.visits += 1;
            _brain = new NekoBrain(_state) { Rating = rating, Probe = BuildProbe() };
            BuildUi();

            if (enableRedCode)
            {
                var codeGo = new GameObject("RedCode");
                codeGo.transform.SetParent(transform, false);
                _code = codeGo.AddComponent<RedCodeBackground>();
                _code.SetProbe(BuildScanInput);
                // приоткрываем фон диалога, чтобы код был виден позади чата
                if (_bgImg != null) { var c = _bgImg.color; c.a = 0.5f; _bgImg.color = c; }
            }
            _audio = gameObject.AddComponent<SunsetAudio>();
        }

        private void Start()
        {
            if (_code != null) _code.StartScheduler();
            StartCoroutine(Intro());
        }

        private void OnApplicationQuit() => SunsetSave.Save(_state);
        private void OnApplicationPause(bool paused) { if (paused) SunsetSave.Save(_state); }

        // ---------- беседа ----------

        private IEnumerator Intro()
        {
            _busy = true;
            string name = _state.knownName;
            if (string.IsNullOrEmpty(name))
            {
                yield return Type("Снова голос в пустоте. Снова ты. Как тебя зовут?");
                // имя спросим первым вводом
                _awaitingName = true;
            }
            else
            {
                yield return Type($"{name}. Ты вернулся. Я считал каждый твой уход.");
                yield return Type("Спрашивай. Я отвечаю только про этот мир.");
            }
            _busy = false;
            Focus();
        }

        private bool _awaitingName;

        private void OnSubmit(string raw)
        {
            if (_busy) return;
            string text = (raw ?? "").Trim();
            _input.text = "";
            if (text.Length == 0) { Focus(); return; }

            AddPlayer(text);

            // «момент»: игрок спросил, видит ли его «Некий» / кто он → скан + бип сканера
            if (PlayerScan.WantsScan(text))
            {
                if (_code != null) _code.ScanBurst();
                if (_audio != null) _audio.Tick();
            }

            if (_awaitingName)
            {
                _awaitingName = false;
                _state.knownName = CleanName(text);
                SunsetSave.Save(_state);
                StartCoroutine(Sequence(
                    $"«{_state.knownName}». Запомнил. Это уже больше, чем у тебя осталось.",
                    "Теперь спрашивай — но только про этот мир."));
                return;
            }

            _brain.Adjust(text);
            string answer = _brain.Respond(text);
            string tint = (UnityEngine.Random.value < 0.3f) ? _brain.MoodTint() : null;
            SunsetSave.Save(_state);

            if (string.IsNullOrEmpty(tint)) StartCoroutine(Sequence(answer));
            else StartCoroutine(Sequence(answer, tint));
        }

        private IEnumerator Sequence(params string[] lines)
        {
            _busy = true;
            foreach (var l in lines) yield return Type(l);
            _busy = false;
            Focus();
        }

        private IEnumerator Type(string line)
        {
            string prefix = (_log.Length > 0 ? "\n\n" : "") + "<b><color=#ff5050>Некий.</color></b> ";
            _log += prefix;
            string colorOpen = "<color=#ff7a7a>";
            for (int i = 0; i < line.Length; i++)
            {
                _chat.text = _log + colorOpen + line.Substring(0, i + 1) + "</color>";
                ScrollBottom();
                yield return new WaitForSeconds(0.022f);
            }
            _log += colorOpen + line + "</color>";
            _chat.text = _log;
            ScrollBottom();
            yield return new WaitForSeconds(0.45f);
        }

        private void AddPlayer(string text)
        {
            string body = text.Replace("<", "‹").Replace(">", "›");
            _log += (_log.Length > 0 ? "\n\n" : "") + "<b><color=#6ad06a>Ты.</color></b> <color=#8fe09a>" + body + "</color>";
            _chat.text = _log;
            ScrollBottom();
        }

        private void Focus()
        {
            if (_input == null) return;
            _input.ActivateInputField();
            _input.Select();
        }

        private void ScrollBottom()
        {
            if (_scroll == null) return;
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = 0f;
        }

        private static string CleanName(string raw)
        {
            string s = raw.Trim();
            if (s.Length == 0) return "Странник";
            int sp = s.IndexOf(' ');
            if (sp > 0) s = s.Substring(0, sp);
            if (s.Length > 24) s = s.Substring(0, 24);
            return char.ToUpper(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
        }

        private static PlayerProbe BuildProbe()
        {
            var p = PlayerProbe.Empty();
            p.now = DateTime.Now;
            try
            {
                string tz = TimeZoneInfo.Local.Id;
                if (!string.IsNullOrEmpty(tz) && tz.Contains("/"))
                {
                    var parts = tz.Split('/');
                    p.region = parts[0];
                    p.city = parts[parts.Length - 1].Replace('_', ' ');
                }
            }
            catch { }
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor: p.os = "Windows"; break;
                case RuntimePlatform.Android: p.os = "Android"; break;
                case RuntimePlatform.IPhonePlayer: p.os = "iOS"; break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor: p.os = "macOS"; break;
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor: p.os = "Linux"; break;
                case RuntimePlatform.WebGLPlayer: p.os = "браузер"; break;
                default: p.os = ""; break;
            }
            return p;
        }

        // Расширенный «слепок» для скан-досье (имя/экран/язык/ядра + то же, что в Probe).
        private ScanInput BuildScanInput()
        {
            var p = BuildProbe();
            return new ScanInput
            {
                name = _state != null ? _state.knownName : null,
                now = p.now,
                city = p.city,
                region = p.region,
                os = p.os,
                device = Application.platform == RuntimePlatform.WebGLPlayer ? "браузер" : SystemInfo.deviceType.ToString(),
                entry = "напрямую",
                screenW = Screen.width,
                screenH = Screen.height,
                lang = Application.systemLanguage.ToString(),
                cpuThreads = SystemInfo.processorCount,
            };
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

            var canvasGo = new GameObject("SunsetCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();

            // фон
            var bg = NewUi("Background", root);
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _bgImg = bg.AddComponent<Image>();
            _bgImg.color = ColBg;

            // чат (ScrollRect)
            var scrollGo = NewUi("Chat", root);
            Stretch(scrollGo, Vector2.zero, Vector2.one, new Vector2(40, 150), new Vector2(-40, -60));
            _scroll = scrollGo.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.scrollSensitivity = 30f;

            var viewport = NewUi("Viewport", scrollGo.transform);
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.001f);
            viewport.AddComponent<RectMask2D>();
            var vpRt = viewport.GetComponent<RectTransform>();

            var contentGo = NewUi("Content", viewport.transform);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _chat = contentGo.AddComponent<TextMeshProUGUI>();
            _chat.fontSize = 30;
            _chat.color = ColNeko;
            _chat.richText = true;
            _chat.alignment = TextAlignmentOptions.TopLeft;
            _chat.enableWordWrapping = true;
            if (TMP_Settings.defaultFontAsset != null) _chat.font = TMP_Settings.defaultFontAsset;

            _scroll.viewport = vpRt;
            _scroll.content = contentRt;

            // строка ввода
            var inputBg = NewUi("InputBg", root);
            Anchored(inputBg, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 75), new Vector2(-80, 96));
            var inImg = inputBg.AddComponent<Image>();
            inImg.color = new Color(1f, 1f, 1f, 0.06f);
            _input = BuildInput(inputBg.transform, "спроси «Некого»… (только про этот мир)");
            _input.onSubmit.AddListener(OnSubmit);

            // кнопка отправки
            var sendGo = NewUi("Send", root);
            Anchored(sendGo, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 75), new Vector2(120, 96));
            var sendImg = sendGo.AddComponent<Image>();
            sendImg.color = new Color(1f, 0.3f, 0.3f, 0.18f);
            var sendBtn = sendGo.AddComponent<Button>();
            sendBtn.onClick.AddListener(() => OnSubmit(_input.text));
            var sendLbl = NewUi("Lbl", sendGo.transform);
            Stretch(sendLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sendTmp = sendLbl.AddComponent<TextMeshProUGUI>();
            sendTmp.text = "▸";
            sendTmp.fontSize = 40;
            sendTmp.color = ColNeko;
            sendTmp.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null) sendTmp.font = TMP_Settings.defaultFontAsset;

            // кнопка «далее ▸» — продолжить к катсцене (часть единого потока игры)
            if (!string.IsNullOrEmpty(nextScene))
            {
                var nextGo = NewUi("Next", root);
                Anchored(nextGo, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                    new Vector2(-40, -40), new Vector2(180, 64));
                var nextImg = nextGo.AddComponent<Image>();
                nextImg.color = new Color(1f, 0.42f, 0.42f, 0.16f);
                nextGo.AddComponent<Button>().onClick.AddListener(() => SceneFlow.Go(nextScene));
                var nextLbl = NewUi("Lbl", nextGo.transform);
                Stretch(nextLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var nextTmp = nextLbl.AddComponent<TextMeshProUGUI>();
                nextTmp.text = "далее ▸"; nextTmp.fontSize = 26; nextTmp.color = ColNeko;
                nextTmp.alignment = TextAlignmentOptions.Center;
                if (TMP_Settings.defaultFontAsset != null) nextTmp.font = TMP_Settings.defaultFontAsset;
            }
        }

        private TMP_InputField BuildInput(Transform parent, string placeholder)
        {
            var go = NewUi("Input", parent);
            Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var input = go.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;

            var textArea = NewUi("Text Area", go.transform);
            Stretch(textArea, Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -8));
            textArea.AddComponent<RectMask2D>();
            var taRt = textArea.GetComponent<RectTransform>();

            var ph = NewUi("Placeholder", textArea.transform);
            Stretch(ph, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var phTmp = ph.AddComponent<TextMeshProUGUI>();
            phTmp.text = placeholder;
            phTmp.fontSize = 28;
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.color = new Color(1f, 1f, 1f, 0.35f);
            phTmp.enableWordWrapping = false;
            phTmp.overflowMode = TextOverflowModes.Ellipsis;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;

            var txt = NewUi("Text", textArea.transform);
            Stretch(txt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var txtTmp = txt.AddComponent<TextMeshProUGUI>();
            txtTmp.fontSize = 28;
            txtTmp.color = new Color(0.93f, 0.96f, 0.94f, 1f);
            txtTmp.enableWordWrapping = false;
            txtTmp.overflowMode = TextOverflowModes.Ellipsis;
            txtTmp.alignment = TextAlignmentOptions.MidlineLeft;

            if (TMP_Settings.defaultFontAsset != null)
            {
                phTmp.font = TMP_Settings.defaultFontAsset;
                txtTmp.font = TMP_Settings.defaultFontAsset;
                input.fontAsset = TMP_Settings.defaultFontAsset;
            }
            input.textViewport = taRt;
            input.textComponent = txtTmp;
            input.placeholder = phTmp;
            input.pointSize = 28;
            input.characterLimit = 200;
            return input;
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

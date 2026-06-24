using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Лобби (фаза 5 порта): карусель локаций-островов с замком по прогрессу,
    /// список игроков, выбор сложности, моды, степпер прогресса (у хоста) и кнопка
    /// «Начать игру», заблокированная, пока выбранную локацию не открыли все игроки.
    /// Сеть имитируется локально (как в веб-прототипе): «друзья» подсаживаются по
    /// таймеру со случайным прогрессом — это наглядно показывает работу гейта.
    ///
    /// Логика — в Core (<see cref="LobbyState"/>, <see cref="LobbyGate"/>,
    /// <see cref="LobbyCode"/>, <see cref="GameLocations"/>, <see cref="GameMods"/>).
    /// UI строится из кода. Сборку сцены см. Sunset → Build Lobby Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetLobby : MonoBehaviour
    {
        [Tooltip("true — хост (может менять сложность/прогресс и стартовать), false — гость.")]
        public bool isHost = true;

        [Tooltip("Код лобби. Пусто — сгенерируется автоматически.")]
        public string code = "";

        [Tooltip("Имитировать подключение «друзей» со случайным прогрессом.")]
        public bool simulateJoins = true;

        // палитра
        private static readonly Color ColBg = new Color(0.05f, 0.04f, 0.03f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.54f, 0.17f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColPanel = new Color(0.04f, 0.03f, 0.02f, 0.55f);
        private static readonly Color ColMuted = new Color(0.95f, 0.91f, 0.81f, 0.5f);
        private static readonly Color ColOk = new Color(0.5f, 0.85f, 0.5f, 1f);
        private static readonly Color ColBad = new Color(1f, 0.45f, 0.42f, 1f);
        private static readonly Color ColLockTint = new Color(0.25f, 0.2f, 0.18f, 1f);

        private static readonly string[] GuestNames = { "Странник", "Кузнец", "Следопыт", "Жрица", "Вард" };

        private NekoState _neko;
        private GameSettings _settings;
        private LobbyState _lobby;
        private List<ModDef> _mods;
        private System.Random _rng = new System.Random();

        private RectTransform _root;

        // ссылки для перерисовки
        private RectTransform _playersGrid;
        private TextMeshProUGUI _pcount;
        private RectTransform _diffRow;
        private RectTransform _modsList;
        private Image _locImage;
        private TextMeshProUGUI _locCounter, _locName, _locWho, _locDesc, _locGate, _progN;
        private GameObject _locLock;
        private RectTransform _dotsRow;
        private Button _prevBtn, _nextBtn, _startBtn;

        private void Awake()
        {
            _settings = SettingsSave.Load();
            _neko = SunsetSave.Load();
            _mods = GameMods.Defaults();

            if (string.IsNullOrEmpty(code)) code = LobbyCode.Generate(_rng);
            int unlocked = Mathf.Clamp(_neko.unlockedLocations, 1, GameLocations.Count);
            var host = new LobbyPlayer(string.IsNullOrEmpty(_neko.knownName) ? "Вы" : _neko.knownName, true, unlocked);
            _lobby = new LobbyState(isHost ? LobbyMode.Host : LobbyMode.Guest, code, host);

            BuildUi();
            RenderAll();
        }

        private void Start()
        {
            if (simulateJoins) StartCoroutine(SimulateJoins());
        }

        // ---------- симуляция подключений ----------

        private IEnumerator SimulateJoins()
        {
            yield return new WaitForSeconds(1.8f);
            while (!_lobby.IsFull)
            {
                int idx = _lobby.Count - 1; // 0 = первый гость
                string name = idx >= 0 && idx < GuestNames.Length ? GuestNames[idx] : "Игрок";
                // у друга свой прогресс — случайно 1..(N-1), демонстрирует блокировку старта
                int unlocked = 1 + _rng.Next(GameLocations.Count - 1);
                _lobby.AddGuest(new LobbyPlayer(name, false, unlocked));
                RenderPlayers();
                RenderLocation(); // новый игрок мог заблокировать выбранную локацию
                yield return new WaitForSeconds(1.8f + (float)_rng.NextDouble() * 2.4f);
            }
        }

        // ---------- перерисовка ----------

        private void RenderAll()
        {
            RenderPlayers();
            RenderDifficulty();
            RenderMods();
            RenderLocation();
        }

        private void RenderPlayers()
        {
            ClearChildren(_playersGrid);
            for (int i = 0; i < LobbyState.MaxPlayers; i++)
            {
                var p = i < _lobby.Count ? _lobby.players[i] : null;
                var slot = NewUi("Slot", _playersGrid);
                slot.AddComponent<LayoutElement>().preferredHeight = 64;
                var img = slot.AddComponent<Image>();
                img.color = p != null && p.host ? new Color(1f, 0.54f, 0.17f, 0.18f) : ColPanel;

                string text = p != null
                    ? $"<b>{Initial(p.name)}</b>  {p.name}  <color=#{(p.host ? "ff8a2b" : "8fe09a")}>{(p.host ? "хост" : "готов")}</color>"
                    : "<color=#7a6f63>+  Свободно</color>";
                var lbl = NewUi("Lbl", slot.transform);
                Stretch(lbl, Vector2.zero, Vector2.one, new Vector2(18, 0), Vector2.zero);
                var t = lbl.AddComponent<TextMeshProUGUI>();
                t.text = text; t.fontSize = 24; t.color = ColGold; t.richText = true;
                t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);
            }
            if (_pcount != null) _pcount.text = $"{_lobby.Count}/{LobbyState.MaxPlayers}";
        }

        private void RenderDifficulty()
        {
            ClearChildren(_diffRow);
            bool guest = _lobby.mode == LobbyMode.Guest;
            for (int i = 0; i < GameDifficulties.Count; i++)
            {
                var d = GameDifficulties.All[i];
                bool active = _settings.difficulty == d.id;
                var go = NewUi("Diff_" + d.id, _diffRow);
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = 48; le.flexibleWidth = 1;
                var img = go.AddComponent<Image>();
                img.color = active ? new Color(1f, 0.54f, 0.17f, 0.22f) : ColPanel;
                var btn = go.AddComponent<Button>();
                btn.interactable = !guest;
                string id = d.id;
                btn.onClick.AddListener(() =>
                {
                    _settings.difficulty = id;
                    SettingsSave.Save(_settings);
                    RenderDifficulty();
                });
                var lbl = NewUi("Lbl", go.transform);
                Stretch(lbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var t = lbl.AddComponent<TextMeshProUGUI>();
                t.text = d.name; t.fontSize = 20; t.color = active ? ColGold : ColMuted;
                t.alignment = TextAlignmentOptions.Center; ApplyFont(t);
            }
        }

        private void RenderMods()
        {
            ClearChildren(_modsList);
            foreach (var m in _mods)
            {
                var row = NewUi("Mod_" + m.name, _modsList);
                row.AddComponent<LayoutElement>().preferredHeight = 54;
                row.AddComponent<Image>().color = m.friend ? new Color(0.3f, 0.4f, 0.7f, 0.12f) : ColPanel;

                string tag = m.custom ? "  <color=#ff8a2b>[свой]</color>" : (m.friend ? "  <color=#8aa0ff>[у друга]</color>" : "");
                var info = NewUi("Info", row.transform);
                Anchored(info, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                    new Vector2(16, 0), new Vector2(-90, 0));
                var t = info.AddComponent<TextMeshProUGUI>();
                t.text = $"{m.name}{tag}\n<size=16><color=#a89b8a>{m.sub}</color></size>";
                t.fontSize = 20; t.color = ColGold; t.richText = true;
                t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);

                var box = NewUi("Toggle", row.transform);
                Anchored(box, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                    new Vector2(-14, 0), new Vector2(34, 34));
                box.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
                var toggle = box.AddComponent<Toggle>();
                toggle.isOn = m.on;
                var check = NewUi("Check", box.transform);
                Stretch(check, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
                var checkImg = check.AddComponent<Image>();
                checkImg.color = ColEmber;
                toggle.graphic = checkImg;
                toggle.targetGraphic = box.GetComponent<Image>();
                var mref = m;
                toggle.onValueChanged.AddListener(v => mref.on = v);
            }
        }

        private void RenderLocation()
        {
            _lobby.ClampLocIndex();
            int i = _lobby.locIndex;
            int total = GameLocations.Count;
            var loc = GameLocations.All[i];
            bool hostHas = _lobby.HostHasLocation(i);

            var sprite = Resources.Load<Sprite>("Sunset/" + loc.image);
            if (sprite != null) { _locImage.sprite = sprite; _locImage.color = hostHas ? Color.white : ColLockTint; }
            else { _locImage.sprite = null; _locImage.color = hostHas ? new Color(0.12f, 0.1f, 0.09f, 1f) : ColLockTint; }

            _locCounter.text = $"{i + 1} / {total}";
            _locName.text = loc.name;
            _locWho.text = loc.who;
            _locDesc.text = loc.desc;
            _locLock.SetActive(!hostHas);

            // точки
            ClearChildren(_dotsRow);
            for (int k = 0; k < total; k++)
            {
                bool open = _lobby.HostHasLocation(k);
                bool cur = k == i;
                var dot = NewUi("Dot_" + k, _dotsRow);
                var le = dot.AddComponent<LayoutElement>();
                le.preferredWidth = 44; le.preferredHeight = 44;
                var img = dot.AddComponent<Image>();
                img.color = cur ? ColEmber : ColPanel;
                int kk = k;
                dot.AddComponent<Button>().onClick.AddListener(() => { _lobby.locIndex = kk; RenderLocation(); });
                var lbl = NewUi("Lbl", dot.transform);
                Stretch(lbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var t = lbl.AddComponent<TextMeshProUGUI>();
                t.text = open ? (k + 1).ToString() : "🔒";
                t.fontSize = 18; t.color = cur ? new Color(0.1f, 0.08f, 0.04f) : ColMuted;
                t.alignment = TextAlignmentOptions.Center; ApplyFont(t);
            }

            if (_prevBtn != null) _prevBtn.interactable = i > 0;
            if (_nextBtn != null) _nextBtn.interactable = i < total - 1;

            var gate = _lobby.Gate();
            _locGate.text = gate.message;
            _locGate.color = gate.ok ? ColOk : ColBad;
            if (_startBtn != null) _startBtn.interactable = gate.ok;
            if (_progN != null) _progN.text = _lobby.HostUnlocked.ToString();
        }

        // ---------- действия ----------

        private void StepProgress(int delta)
        {
            var host = _lobby.Host;
            if (host == null) return;
            host.unlocked = Mathf.Clamp(host.unlocked + delta, 1, GameLocations.Count);
            _neko.unlockedLocations = host.unlocked;
            SunsetSave.Save(_neko);
            RenderLocation();
        }

        private void OnStart()
        {
            var gate = _lobby.Gate();
            if (!gate.ok) { RenderLocation(); return; }
            var loc = GameLocations.All[_lobby.locIndex];
            Debug.Log($"[Sunset] Старт игры из лобби: «{loc.name}», сложность {GameDifficulties.NameOf(_settings.difficulty)}, игроков {_lobby.Count}.");
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

            var canvasGo = new GameObject("LobbyCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo.GetComponent<RectTransform>();

            var bg = NewUi("Background", _root);
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bg.AddComponent<Image>(); bgImg.color = ColBg; bgImg.raycastTarget = false;

            // ----- шапка -----
            var header = NewUi("Header", _root);
            Anchored(header, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(-80, 80));
            var headT = header.AddComponent<TextMeshProUGUI>();
            headT.text = $"Лобби      <size=28><color=#a89b8a>{(_lobby.mode == LobbyMode.Host ? "Код" : "Лобби")}: <b>{code}</b></color></size>";
            headT.fontSize = 44; headT.color = ColGold; headT.richText = true;
            headT.alignment = TextAlignmentOptions.Left; ApplyFont(headT);

            // ----- левая колонка -----
            var left = NewUi("Left", _root);
            Anchored(left, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(40, 0), new Vector2(820, -140));
            var leftRt = left.GetComponent<RectTransform>();
            leftRt.offsetMin = new Vector2(40, 40); leftRt.offsetMax = new Vector2(860, -100);
            var lv = left.AddComponent<VerticalLayoutGroup>();
            lv.spacing = 16; lv.childControlWidth = true; lv.childControlHeight = false;
            lv.childForceExpandWidth = true; lv.childForceExpandHeight = false;

            _pcount = AddSectionHeader(left.transform, "Игроки", out var _);
            _playersGrid = AddColumn(left.transform, 12);
            AddSectionHeader(left.transform, "Сложность", out var _);
            _diffRow = AddRow(left.transform, 8, 52);
            AddSectionHeader(left.transform, "Моды", out var _);
            _modsList = AddColumn(left.transform, 8);

            if (_lobby.mode == LobbyMode.Host)
            {
                var startGo = NewUi("Start", left.transform);
                startGo.AddComponent<LayoutElement>().preferredHeight = 70;
                startGo.AddComponent<Image>().color = new Color(1f, 0.54f, 0.17f, 0.22f);
                _startBtn = startGo.AddComponent<Button>();
                _startBtn.onClick.AddListener(OnStart);
                var sl = NewUi("Lbl", startGo.transform);
                Stretch(sl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var st = sl.AddComponent<TextMeshProUGUI>();
                st.text = "Начать игру"; st.fontSize = 30; st.color = ColGold;
                st.alignment = TextAlignmentOptions.Center; ApplyFont(st);
            }
            else
            {
                AddParagraph(left.transform, "Ждём, пока хост начнёт игру…", ColMuted, 26);
            }

            // ----- правая колонка: карусель локаций -----
            BuildLocationPanel();

            // ----- кнопка «назад» -----
            var back = NewUi("Back", _root);
            Anchored(back, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -100), new Vector2(200, 50));
            back.AddComponent<Image>().color = ColPanel;
            back.AddComponent<Button>().onClick.AddListener(() => Debug.Log("[Sunset] Выход из лобби в меню (связка сцен — отдельная фаза)."));
            var bl = NewUi("Lbl", back.transform);
            Stretch(bl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bt = bl.AddComponent<TextMeshProUGUI>();
            bt.text = "← В меню"; bt.fontSize = 22; bt.color = ColGold;
            bt.alignment = TextAlignmentOptions.Center; ApplyFont(bt);
        }

        private void BuildLocationPanel()
        {
            var aside = NewUi("Aside", _root);
            var rt = aside.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(900, 40); rt.offsetMax = new Vector2(-40, -100);

            AddParagraph(aside.transform, "Локация", ColEmber, 36, anchorTop: true, height: 50, topOffset: 0);
            AddParagraph(aside.transform,
                "5 островов — следующий открывается после победы над боссами предыдущего. Листай, чтобы выбрать.",
                ColMuted, 20, anchorTop: true, height: 60, topOffset: -54);

            // карточка локации
            var card = NewUi("Card", aside.transform);
            Anchored(card, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -120), new Vector2(-120, 520));
            card.AddComponent<Image>().color = ColPanel;

            var imgWrap = NewUi("Img", card.transform);
            Anchored(imgWrap, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -12), new Vector2(-24, 300));
            _locImage = imgWrap.AddComponent<Image>();
            _locImage.color = new Color(0.12f, 0.1f, 0.09f, 1f);
            _locImage.preserveAspect = false;

            var counter = NewUi("Counter", imgWrap.transform);
            Anchored(counter, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-10, -10), new Vector2(110, 36));
            counter.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var cl = NewUi("Lbl", counter.transform);
            Stretch(cl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _locCounter = cl.AddComponent<TextMeshProUGUI>();
            _locCounter.fontSize = 20; _locCounter.color = ColGold;
            _locCounter.alignment = TextAlignmentOptions.Center; ApplyFont(_locCounter);

            _locLock = NewUi("Lock", imgWrap.transform);
            Stretch(_locLock, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _locLock.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            var lockLbl = NewUi("Lbl", _locLock.transform);
            Stretch(lockLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var lockT = lockLbl.AddComponent<TextMeshProUGUI>();
            lockT.text = "🔒\nНе открыта"; lockT.fontSize = 30; lockT.color = ColGold;
            lockT.alignment = TextAlignmentOptions.Center; ApplyFont(lockT);

            // мета (имя/жители/описание)
            var name = NewUi("Name", card.transform);
            Anchored(name, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -320), new Vector2(-32, 50));
            _locName = name.AddComponent<TextMeshProUGUI>();
            _locName.fontSize = 30; _locName.color = ColGold;
            _locName.alignment = TextAlignmentOptions.Left; ApplyFont(_locName);

            var who = NewUi("Who", card.transform);
            Anchored(who, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -366), new Vector2(-32, 34));
            _locWho = who.AddComponent<TextMeshProUGUI>();
            _locWho.fontSize = 20; _locWho.color = ColEmber;
            _locWho.alignment = TextAlignmentOptions.Left; ApplyFont(_locWho);

            var desc = NewUi("Desc", card.transform);
            Anchored(desc, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -404), new Vector2(-32, 100));
            _locDesc = desc.AddComponent<TextMeshProUGUI>();
            _locDesc.fontSize = 20; _locDesc.color = ColGold;
            _locDesc.enableWordWrapping = true;
            _locDesc.alignment = TextAlignmentOptions.TopLeft; ApplyFont(_locDesc);

            // стрелки
            _prevBtn = AddArrow(aside.transform, "‹", new Vector2(0, 1), new Vector2(20, -340));
            _prevBtn.onClick.AddListener(() => { _lobby.StepLocation(-1); RenderLocation(); });
            _nextBtn = AddArrow(aside.transform, "›", new Vector2(1, 1), new Vector2(-20, -340));
            _nextBtn.onClick.AddListener(() => { _lobby.StepLocation(1); RenderLocation(); });

            // точки
            _dotsRow = AddRow(aside.transform, 10, 44);
            var dotsRt = _dotsRow.GetComponent<RectTransform>();
            dotsRt.anchorMin = new Vector2(0, 1); dotsRt.anchorMax = new Vector2(1, 1);
            dotsRt.pivot = new Vector2(0.5f, 1);
            dotsRt.anchoredPosition = new Vector2(0, -660); dotsRt.sizeDelta = new Vector2(-200, 44);

            // сообщение гейта
            var gate = NewUi("Gate", aside.transform);
            Anchored(gate, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -716), new Vector2(0, 70));
            _locGate = gate.AddComponent<TextMeshProUGUI>();
            _locGate.fontSize = 22; _locGate.color = ColBad; _locGate.enableWordWrapping = true;
            _locGate.alignment = TextAlignmentOptions.TopLeft; ApplyFont(_locGate);

            // степпер прогресса (только хост)
            if (_lobby.mode == LobbyMode.Host)
            {
                var prog = NewUi("Progress", aside.transform);
                Anchored(prog, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                    new Vector2(0, -800), new Vector2(0, 56));
                var ph = prog.AddComponent<HorizontalLayoutGroup>();
                ph.spacing = 12; ph.childAlignment = TextAnchor.MiddleLeft;
                ph.childControlWidth = false; ph.childControlHeight = true;
                ph.childForceExpandWidth = false;

                AddInlineLabel(prog.transform, "Открыто локаций:", 22, ColMuted, 220);
                AddStepperBtn(prog.transform, "−", () => StepProgress(-1));
                var n = AddInlineLabel(prog.transform, "1", 28, ColGold, 50);
                _progN = n;
                AddStepperBtn(prog.transform, "+", () => StepProgress(1));
            }
        }

        // ---------- секции/виджеты ----------

        private TextMeshProUGUI AddSectionHeader(Transform parent, string title, out GameObject go)
        {
            go = NewUi("H_" + title, parent);
            go.AddComponent<LayoutElement>().preferredHeight = 40;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = title; t.fontSize = 28; t.color = ColEmber;
            t.alignment = TextAlignmentOptions.Left; ApplyFont(t);
            // вернём ссылку на «счётчик» рядом с «Игроки» — допишем его в текст RenderPlayers
            if (title == "Игроки")
            {
                t.text = "Игроки";
                var cnt = NewUi("Count", go.transform);
                Anchored(cnt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                    new Vector2(140, 0), new Vector2(-140, 0));
                var ct = cnt.AddComponent<TextMeshProUGUI>();
                ct.fontSize = 22; ct.color = ColMuted;
                ct.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(ct);
                return ct;
            }
            return t;
        }

        private RectTransform AddColumn(Transform parent, float spacing)
        {
            var go = NewUi("Col", parent);
            go.AddComponent<LayoutElement>().flexibleHeight = 0;
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.childControlWidth = true; v.childControlHeight = false;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.GetComponent<RectTransform>();
        }

        private RectTransform AddRow(Transform parent, float spacing, float height)
        {
            var go = NewUi("Row", parent);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing; h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            return go.GetComponent<RectTransform>();
        }

        private Button AddArrow(Transform parent, string glyph, Vector2 anchor, Vector2 pos)
        {
            var go = NewUi("Arrow", parent);
            Anchored(go, anchor, anchor, anchor, pos, new Vector2(48, 80));
            go.AddComponent<Image>().color = ColPanel;
            var btn = go.AddComponent<Button>();
            var lbl = NewUi("Lbl", go.transform);
            Stretch(lbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = glyph; t.fontSize = 40; t.color = ColGold;
            t.alignment = TextAlignmentOptions.Center; ApplyFont(t);
            return btn;
        }

        private TextMeshProUGUI AddInlineLabel(Transform parent, string text, int size, Color color, float width)
        {
            var go = NewUi("L", parent);
            go.AddComponent<LayoutElement>().preferredWidth = width;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);
            return t;
        }

        private void AddStepperBtn(Transform parent, string glyph, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi("Step", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 48; le.preferredHeight = 48;
            go.AddComponent<Image>().color = ColPanel;
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var lbl = NewUi("Lbl", go.transform);
            Stretch(lbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = lbl.AddComponent<TextMeshProUGUI>();
            t.text = glyph; t.fontSize = 30; t.color = ColGold;
            t.alignment = TextAlignmentOptions.Center; ApplyFont(t);
        }

        private void AddParagraph(Transform parent, string text, Color color, int size,
            bool anchorTop = false, float height = 36, float topOffset = 0)
        {
            var go = NewUi("P", parent);
            if (anchorTop)
            {
                Anchored(go, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                    new Vector2(0, topOffset), new Vector2(0, height));
            }
            else
            {
                go.AddComponent<LayoutElement>().preferredHeight = height;
            }
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.enableWordWrapping = true;
            t.alignment = TextAlignmentOptions.TopLeft; ApplyFont(t);
        }

        // ---------- утилиты ----------

        private static string Initial(string name)
        {
            string s = (name ?? "?").Trim();
            return s.Length > 0 ? char.ToUpper(s[0]).ToString() : "?";
        }

        private static void ClearChildren(RectTransform rt)
        {
            if (rt == null) return;
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
        }

        private static void ApplyFont(TMP_Text t)
        {
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

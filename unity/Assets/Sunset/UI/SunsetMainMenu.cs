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
    /// Главное меню (фаза 4 порта). Строит весь UI из кода (фон + логотип, колонка
    /// кнопок слева, «Некий» в меню, модальные окна настроек/дневника/модов/выхода).
    /// Логика — в Core (<see cref="GameSettings"/>, <see cref="MenuPresence"/>,
    /// <see cref="Diary"/>, <see cref="GameDifficulties"/>).
    ///
    /// Кнопки «Новая игра», «Создать лобби», «Ввести код» пока показывают заглушку-
    /// модалку (как в веб-прототипе) — соответствующие сцены подключаются отдельно.
    ///
    /// Сборку сцены одним кликом см. Sunset → Build Main Menu Scene (редактор).
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetMainMenu : MonoBehaviour
    {
        private const string SaveKey = "sunset_save"; // черновик сейва (для кнопки «Продолжить»)

        // палитра (в тон веб-CSS)
        private static readonly Color ColBg = new Color(0.04f, 0.03f, 0.02f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.54f, 0.17f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColBtnBg = new Color(0.04f, 0.03f, 0.02f, 0.5f);
        private static readonly Color ColNeko = new Color(1f, 0.6f, 0.45f, 1f);
        private static readonly Color ColModalBack = new Color(0f, 0f, 0f, 0.72f);
        private static readonly Color ColCard = new Color(0.08f, 0.06f, 0.05f, 0.98f);

        private NekoState _neko;
        private GameSettings _settings;
        private string _prevExit;
        private long _timeAwayMs;
        private bool _sessionLaunched;

        private RectTransform _root;
        private Button _continueBtn;
        private TextMeshProUGUI _nekoLabel;
        private Coroutine _nekoHide, _dwell;

        // модалка
        private GameObject _modalRoot;
        private TextMeshProUGUI _modalTitle;
        private RectTransform _modalBody;

        private void Awake()
        {
            _settings = SettingsSave.Load();
            _neko = SunsetSave.Load();

            _prevExit = _neko.lastExit;
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _timeAwayMs = _neko.lastSeenUnix > 0 ? nowMs - _neko.lastSeenUnix : 0;
            _neko.visits += 1;
            SunsetSave.Save(_neko);

            BuildUi();
        }

        private void Start() => StartCoroutine(Presence());

        private void OnApplicationQuit() => RecordExit();
        private void OnApplicationPause(bool paused) { if (paused) RecordExit(); }

        private void RecordExit()
        {
            _neko.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _neko.lastExit = _sessionLaunched ? "played" : "peek";
            SunsetSave.Save(_neko);
        }

        // ---------- присутствие «Некого» ----------

        private IEnumerator Presence()
        {
            string line = MenuPresence.Greeting(_neko, _prevExit, _timeAwayMs);
            if (!string.IsNullOrEmpty(line))
            {
                yield return new WaitForSeconds(1.4f);
                ShowNeko(line, 7f);
            }
            // «зависание»: если игрок долго стоит в меню, ничего не запуская
            yield return new WaitForSeconds(25f);
            if (!_sessionLaunched && !_modalRoot.activeSelf)
                ShowNeko(MenuPresence.DwellLine(_neko), 6f);
        }

        private void ShowNeko(string text, float seconds)
        {
            if (_nekoLabel == null) return;
            _nekoLabel.text = text;
            _nekoLabel.gameObject.SetActive(true);
            if (_nekoHide != null) StopCoroutine(_nekoHide);
            _nekoHide = StartCoroutine(HideNekoAfter(seconds));
        }

        private IEnumerator HideNekoAfter(float s)
        {
            yield return new WaitForSeconds(s);
            if (_nekoLabel != null) _nekoLabel.gameObject.SetActive(false);
        }

        // ---------- действия кнопок ----------

        private void OnNewGame()
        {
            _sessionLaunched = true;
            RecordExit(); // запоминаем «played» до загрузки следующей сцены
            // новая игра начинается со вступления «Некого» (диалог + красный код)
            SceneFlow.Go(SceneFlow.Dialogue);
        }

        private void OnContinue()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            _sessionLaunched = true;
            RecordExit();
            SceneFlow.Go(SceneFlow.HeroSelect);
        }

        private void OnCreateLobby()
        {
            _sessionLaunched = true;
            PlayerPrefs.SetString("sunset_lobby_mode", "host");
            PlayerPrefs.SetString("sunset_lobby_code", "");
            PlayerPrefs.Save();
            RecordExit();
            SceneFlow.Go(SceneFlow.Lobby);
        }

        private void OnJoinLobby()
        {
            var body = NewVerticalGroup();
            AddParagraph(body.transform, "Введите код лобби друга, чтобы войти в общий мир.", ColGold);
            var input = AddInputField(body.transform, "КОД (например ABC123)");
            input.characterLimit = 6;
            var status = AddParagraphRef(body.transform, "", ColEmber);

            AddPrimaryButton(body.transform, "Войти", () =>
            {
                string code = LobbyCode.Normalize(input.text);
                if (!LobbyCode.IsValid(code))
                {
                    status.text = "Введите корректный код (минимум 4 символа).";
                    return;
                }
                _sessionLaunched = true;
                PlayerPrefs.SetString("sunset_lobby_mode", "guest");
                PlayerPrefs.SetString("sunset_lobby_code", code);
                PlayerPrefs.Save();
                RecordExit();
                SceneFlow.Go(SceneFlow.Lobby);
            });
            AddPrimaryButton(body.transform, "Отмена", CloseModal);
            OpenModal("Ввести код", body);
        }

        // ---------- настройки ----------

        private void OpenSettings()
        {
            var body = NewVerticalGroup();

            var music = AddSlider(body.transform, "Музыка", _settings.music);
            var sfx = AddSlider(body.transform, "Звуки", _settings.sfx);
            var lang = AddDropdown(body.transform, "Язык",
                new[] { "Русский", "English (перевод позже)" },
                _settings.lang == "en" ? 1 : 0);
            var diffNames = new List<string>();
            int diffIdx = 0;
            for (int i = 0; i < GameDifficulties.Count; i++)
            {
                diffNames.Add(GameDifficulties.All[i].name);
                if (GameDifficulties.All[i].id == _settings.difficulty) diffIdx = i;
            }
            var diff = AddDropdown(body.transform, "Сложность по умолчанию", diffNames.ToArray(), diffIdx);
            var rating = AddDropdown(body.transform, "Контент (цензура «Некого»)",
                new[] { "16+ — сдержанно, журит за мат", "18+ — резче и свободнее" },
                _settings.rating == "18" ? 1 : 0);
            var fs = AddToggle(body.transform, "Полноэкранный режим", _settings.fullscreen);

            AddPrimaryButton(body.transform, "Сохранить", () =>
            {
                _settings.music = Mathf.RoundToInt(music.value);
                _settings.sfx = Mathf.RoundToInt(sfx.value);
                _settings.lang = lang.value == 1 ? "en" : "ru";
                _settings.difficulty = GameDifficulties.All[Mathf.Clamp(diff.value, 0, GameDifficulties.Count - 1)].id;
                _settings.rating = rating.value == 1 ? "18" : "16";
                _settings.fullscreen = fs.isOn;
                SettingsSave.Save(_settings);
                Screen.fullScreen = _settings.fullscreen;
                CloseModal();
            });

            OpenModal("Настройки", body);
        }

        // ---------- дневник ----------

        private void OpenJournal()
        {
            var body = NewVerticalGroup();
            AddHint(body.transform, Diary.Intro);

            string heroId = PlayerPrefs.GetString("sunset_hero", "");
            string path = NekoPath.Estimate(_neko);
            Color baseCol = JournalColor(path);

            var entries = Diary.Build(_neko, heroId, UnityEngine.Random.Range(0, 999999));
            foreach (var e in entries)
            {
                string draw = DrawingNote(e.drawing, e.drawingAltered);
                string tail = draw != null ? $"   <size=15><i><color=#9a8d7c>(рисунок: {draw})</color></i></size>" : "";
                switch (e.kind)
                {
                    case DiaryKind.Neko: // запись рукой «Некого»
                        AddParagraph(body.transform, "✎ " + e.text, new Color(1f, 0.38f, 0.36f), italic: true);
                        break;
                    case DiaryKind.Creepy: // странная, чужая запись
                        AddParagraph(body.transform, e.text, new Color(0.86f, 0.24f, 0.24f), italic: true);
                        break;
                    case DiaryKind.Edited: // переписано задним числом
                        AddParagraph(body.transform,
                            "«" + e.text + "»   <size=15><color=#a06b6b>(изменено)</color></size>" + tail, baseCol);
                        if (!string.IsNullOrEmpty(e.note))
                            AddParagraph(body.transform, "— " + e.note + " — Н.", ColEmber, italic: true);
                        break;
                    default:
                        AddParagraph(body.transform, "«" + e.text + "»" + tail, baseCol);
                        break;
                }
            }
            AddPrimaryButton(body.transform, "Закрыть", CloseModal);
            OpenModal("Дневник", body);
        }

        // цвет записей по пути: теплее на добром, холоднее/серее на тёмном
        private static Color JournalColor(string path)
        {
            switch (path)
            {
                case NekoPath.Good: return new Color(0.98f, 0.92f, 0.78f);
                case NekoPath.Bad: return new Color(0.74f, 0.76f, 0.80f);
                case NekoPath.Middle: return new Color(0.90f, 0.88f, 0.82f);
                default: return ColGold;
            }
        }

        // текстовое описание рисунка (без emoji — TMP-шрифт без эмодзи)
        private static string DrawingNote(string drawing, bool altered)
        {
            switch (drawing)
            {
                case "pebble": return altered ? "камушек, но с трещиной" : "камушек с улыбкой";
                case "tree": return "человечек под деревом";
                case "heart": return altered ? "сердечко с трещиной" : "маленькое сердечко";
                case "food": return "кусок еды с ножками";
                case "cup": return "кружка с волнами";
                default: return null;
            }
        }

        // ---------- моды ----------

        private void OpenMods()
        {
            var body = NewVerticalGroup();
            AddParagraph(body.transform,
                "Включайте и отключайте моды. На своих модах можно играть всегда; " +
                "в общем лобби действуют моды хоста.", ColGold);

            (string, string, bool)[] mods =
            {
                ("Расширенный бестиарий", "Доп. мобы и дроп-таблицы", true),
                ("Больше построек", "+12 зданий и декор", false),
                ("Хардкорная выживалка", "Голод, жажда, температура", false),
            };
            foreach (var (name, sub, on) in mods)
                AddToggle(body.transform, name + "  —  " + sub, on);

            AddHint(body.transform, "Добавить свой мод — положите папку в mods/ (загрузка появится позже).");
            AddPrimaryButton(body.transform, "Готово", CloseModal);
            OpenModal("Моды", body);
        }

        // ---------- выход ----------

        private void OpenExit()
        {
            var body = NewVerticalGroup();
            AddParagraph(body.transform, "Выйти из игры?", ColGold);
            AddPrimaryButton(body.transform, "Выйти", () =>
            {
                RecordExit();
                Application.Quit();
                CloseModal();
            });
            AddPrimaryButton(body.transform, "Остаться", CloseModal);
            OpenModal("Выход", body);
        }

        // ---------- модальная система ----------

        private void OpenModal(string title, string text)
        {
            var body = NewVerticalGroup();
            AddParagraph(body.transform, text, ColGold);
            AddPrimaryButton(body.transform, "Ок", CloseModal);
            OpenModal(title, body);
        }

        private void OpenModal(string title, GameObject body)
        {
            _modalTitle.text = title;
            for (int i = _modalBody.childCount - 1; i >= 0; i--)
                Destroy(_modalBody.GetChild(i).gameObject);
            body.transform.SetParent(_modalBody, false);
            var rt = body.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            _modalRoot.SetActive(true);
        }

        private void CloseModal() => _modalRoot.SetActive(false);

        private void RefreshContinue()
        {
            if (_continueBtn != null) _continueBtn.interactable = PlayerPrefs.HasKey(SaveKey);
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

            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo.GetComponent<RectTransform>();

            // фон
            var bg = NewUi("Background", _root);
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bg.AddComponent<Image>();
            var bgSprite = Resources.Load<Sprite>("Sunset/menu_bg");
            if (bgSprite != null) { bgImg.sprite = bgSprite; bgImg.color = Color.white; bgImg.preserveAspect = false; }
            else bgImg.color = ColBg;
            bgImg.raycastTarget = false;

            // затемнение слева — плавный градиент (а не жёсткая панель), чтобы
            // текст читался, но переход в арт был мягким, как в веб-версии
            var shade = NewUi("LeftShade", _root);
            Anchored(shade, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(0, 0), new Vector2(1000, 0));
            var shadeImg = shade.AddComponent<Image>();
            shadeImg.sprite = MakeHorizontalFade(new Color(0f, 0f, 0f, 0.8f));
            shadeImg.color = Color.white;
            shadeImg.raycastTarget = false;

            // логотип
            var logoSprite = Resources.Load<Sprite>("Sunset/logo");
            if (logoSprite != null)
            {
                var logo = NewUi("Logo", _root);
                Anchored(logo, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(70, -60), new Vector2(560, 200));
                var logoImg = logo.AddComponent<Image>();
                logoImg.sprite = logoSprite;
                logoImg.preserveAspect = true;
                logoImg.raycastTarget = false;
            }
            else
            {
                var title = NewUi("Title", _root);
                Anchored(title, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(70, -70), new Vector2(700, 120));
                var t = title.AddComponent<TextMeshProUGUI>();
                t.text = "SUNSET\nOF THE WORLD";
                t.fontSize = 56; t.color = ColGold; t.alignment = TextAlignmentOptions.TopLeft;
                ApplyFont(t);
            }

            // колонка кнопок (слева)
            var col = NewUi("Buttons", _root);
            Anchored(col, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(70, -30), new Vector2(520, 560));
            var vlg = col.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            AddMenuButton(col.transform, "Новая игра", OnNewGame);
            _continueBtn = AddMenuButton(col.transform, "Продолжить", OnContinue);
            AddMenuButton(col.transform, "Создать лобби", OnCreateLobby);
            AddMenuButton(col.transform, "Ввести код", OnJoinLobby);
            AddMenuButton(col.transform, "Настройки", OpenSettings);
            AddMenuButton(col.transform, "Дневник", OpenJournal);
            AddMenuButton(col.transform, "Выход", OpenExit);
            RefreshContinue();

            // кнопка модов (внизу слева)
            var mods = NewUi("Mods", _root);
            Anchored(mods, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(70, 70), new Vector2(200, 56));
            var modsImg = mods.AddComponent<Image>();
            modsImg.color = ColBtnBg;
            var modsBtn = mods.AddComponent<Button>();
            modsBtn.onClick.AddListener(OpenMods);
            var modsLbl = NewUi("Lbl", mods.transform);
            Stretch(modsLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var modsT = modsLbl.AddComponent<TextMeshProUGUI>();
            modsT.text = "Моды"; modsT.fontSize = 26; modsT.color = ColEmber;
            modsT.alignment = TextAlignmentOptions.Center; ApplyFont(modsT);

            // build-tag
            var tag = NewUi("BuildTag", _root);
            Anchored(tag, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-40, 28), new Vector2(460, 30));
            var tagT = tag.AddComponent<TextMeshProUGUI>();
            tagT.text = "PROTOTYPE BUILD · UNITY"; tagT.fontSize = 18; tagT.characterSpacing = 6f;
            tagT.color = new Color(0.95f, 0.91f, 0.81f, 0.4f);
            tagT.alignment = TextAlignmentOptions.Right; ApplyFont(tagT);

            // реплика «Некого» (присутствие)
            var neko = NewUi("MenuNeko", _root);
            Anchored(neko, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-60, 60), new Vector2(720, 120));
            _nekoLabel = neko.AddComponent<TextMeshProUGUI>();
            _nekoLabel.fontSize = 26; _nekoLabel.color = ColNeko; _nekoLabel.fontStyle = FontStyles.Italic;
            _nekoLabel.alignment = TextAlignmentOptions.BottomRight; _nekoLabel.enableWordWrapping = true;
            ApplyFont(_nekoLabel);
            neko.SetActive(false);

            BuildModal();
        }

        private void BuildModal()
        {
            _modalRoot = NewUi("Modal", _root);
            Stretch(_modalRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var backImg = _modalRoot.AddComponent<Image>();
            backImg.color = ColModalBack;
            var backBtn = _modalRoot.AddComponent<Button>();
            backBtn.transition = Selectable.Transition.None;
            backBtn.onClick.AddListener(CloseModal);

            var card = NewUi("Card", _modalRoot.transform);
            Anchored(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900, 700));
            var cardImg = card.AddComponent<Image>();
            cardImg.color = ColCard;
            // клик по карточке не закрывает модалку
            card.AddComponent<Button>().transition = Selectable.Transition.None;

            var titleGo = NewUi("Title", card.transform);
            Anchored(titleGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -20), new Vector2(-60, 70));
            _modalTitle = titleGo.AddComponent<TextMeshProUGUI>();
            _modalTitle.fontSize = 40; _modalTitle.color = ColEmber;
            _modalTitle.alignment = TextAlignmentOptions.Left; ApplyFont(_modalTitle);

            var x = NewUi("Close", card.transform);
            Anchored(x, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-20, -20), new Vector2(56, 56));
            x.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            x.AddComponent<Button>().onClick.AddListener(CloseModal);
            var xl = NewUi("Lbl", x.transform);
            Stretch(xl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var xt = xl.AddComponent<TextMeshProUGUI>();
            xt.text = "✕"; xt.fontSize = 30; xt.color = ColGold;
            xt.alignment = TextAlignmentOptions.Center; ApplyFont(xt);

            var bodyGo = NewUi("Body", card.transform);
            Anchored(bodyGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f),
                new Vector2(0, -45), new Vector2(-80, -130));
            _modalBody = bodyGo.GetComponent<RectTransform>();

            _modalRoot.SetActive(false);
        }

        // ---------- виджеты ----------

        private Button AddMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi("Btn_" + label, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 58;
            // подложка прозрачна по умолчанию, подсвечивается лишь при наведении —
            // как в веб-версии (просто жирный текст на фоне арта)
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 0.54f, 0.17f, 0.16f);
            colors.pressedColor = new Color(1f, 0.54f, 0.17f, 0.26f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(1f, 1f, 1f, 0f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            var lblGo = NewUi("Lbl", go.transform);
            Stretch(lblGo, Vector2.zero, Vector2.one, new Vector2(8, 0), Vector2.zero);
            var t = lblGo.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 36; t.color = ColGold; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Left; ApplyFont(t);
            return btn;
        }

        private GameObject NewVerticalGroup()
        {
            var go = NewUi("Group", null);
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go;
        }

        private void AddParagraph(Transform parent, string text, Color color, bool italic = false)
        {
            var go = NewUi("P", parent);
            go.AddComponent<LayoutElement>().minHeight = 36;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 26; t.color = color;
            if (italic) t.fontStyle = FontStyles.Italic;
            t.alignment = TextAlignmentOptions.TopLeft; t.enableWordWrapping = true;
            ApplyFont(t);
        }

        private void AddHint(Transform parent, string text)
            => AddParagraph(parent, text, new Color(0.95f, 0.91f, 0.81f, 0.55f), italic: true);

        // Абзац с возвратом ссылки (для статус-строк, которые меняются).
        private TextMeshProUGUI AddParagraphRef(Transform parent, string text, Color color)
        {
            var go = NewUi("P", parent);
            go.AddComponent<LayoutElement>().minHeight = 32;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = 22; t.color = color;
            t.alignment = TextAlignmentOptions.TopLeft; t.enableWordWrapping = true;
            ApplyFont(t);
            return t;
        }

        // Поле ввода (TMP) для модалок (код лобби и т.п.).
        private TMP_InputField AddInputField(Transform parent, string placeholder)
        {
            var row = NewUi("InputRow", parent);
            row.AddComponent<LayoutElement>().preferredHeight = 60;
            row.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var input = row.AddComponent<TMP_InputField>();
            input.lineType = TMP_InputField.LineType.SingleLine;

            var area = NewUi("Text Area", row.transform);
            Stretch(area, Vector2.zero, Vector2.one, new Vector2(16, 6), new Vector2(-16, -6));
            area.AddComponent<RectMask2D>();

            var ph = NewUi("Placeholder", area.transform);
            Stretch(ph, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var phT = ph.AddComponent<TextMeshProUGUI>();
            phT.text = placeholder; phT.fontSize = 24; phT.fontStyle = FontStyles.Italic;
            phT.color = new Color(1f, 1f, 1f, 0.35f); phT.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyFont(phT);

            var txt = NewUi("Text", area.transform);
            Stretch(txt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var txtT = txt.AddComponent<TextMeshProUGUI>();
            txtT.fontSize = 24; txtT.color = ColGold; txtT.alignment = TextAlignmentOptions.MidlineLeft;
            ApplyFont(txtT);

            input.textViewport = area.GetComponent<RectTransform>();
            input.textComponent = txtT;
            input.placeholder = phT;
            input.pointSize = 24;
            input.characterValidation = TMP_InputField.CharacterValidation.None;
            if (TMP_Settings.defaultFontAsset != null) input.fontAsset = TMP_Settings.defaultFontAsset;
            return input;
        }

        private Slider AddSlider(Transform parent, string label, int value)
        {
            var row = NewUi("Field_" + label, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 64;

            var lblGo = NewUi("Lbl", row.transform);
            Anchored(lblGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(0, 30));
            var lt = lblGo.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 24; lt.color = ColGold;
            lt.alignment = TextAlignmentOptions.Left; ApplyFont(lt);

            var sliderGo = NewUi("Slider", row.transform);
            Anchored(sliderGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 6), new Vector2(0, 24));
            var slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0; slider.maxValue = 100; slider.wholeNumbers = true;

            var bgGo = NewUi("BG", sliderGo.transform);
            Stretch(bgGo, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -4), new Vector2(0, 4));
            bgGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            var fillArea = NewUi("Fill Area", sliderGo.transform);
            Stretch(fillArea, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, -4), new Vector2(0, 4));
            var fillGo = NewUi("Fill", fillArea.transform);
            Stretch(fillGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            fillGo.AddComponent<Image>().color = ColEmber;
            slider.fillRect = fillGo.GetComponent<RectTransform>();

            var handleArea = NewUi("Handle Slide Area", sliderGo.transform);
            Stretch(handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handleGo = NewUi("Handle", handleArea.transform);
            var hrt = handleGo.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(20, 28);
            handleGo.AddComponent<Image>().color = ColGold;
            slider.handleRect = hrt;
            slider.targetGraphic = handleGo.GetComponent<Image>();

            slider.value = value;
            return slider;
        }

        private TMP_Dropdown AddDropdown(Transform parent, string label, string[] options, int selected)
        {
            var row = NewUi("Field_" + label, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 90;

            var lblGo = NewUi("Lbl", row.transform);
            Anchored(lblGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1),
                new Vector2(0, 0), new Vector2(0, 28));
            var lt = lblGo.AddComponent<TextMeshProUGUI>();
            lt.text = label; lt.fontSize = 24; lt.color = ColGold;
            lt.alignment = TextAlignmentOptions.Left; ApplyFont(lt);

            var ddGo = NewUi("Dropdown", row.transform);
            Anchored(ddGo, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, 6), new Vector2(0, 48));
            ddGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            var dd = ddGo.AddComponent<TMP_Dropdown>();

            var labelGo = NewUi("Label", ddGo.transform);
            Stretch(labelGo, Vector2.zero, Vector2.one, new Vector2(14, 4), new Vector2(-34, -4));
            var labelT = labelGo.AddComponent<TextMeshProUGUI>();
            labelT.fontSize = 22; labelT.color = ColGold;
            labelT.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(labelT);
            dd.captionText = labelT;

            // шаблон выпадающего списка
            var template = NewUi("Template", ddGo.transform);
            Anchored(template, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1),
                new Vector2(0, 2), new Vector2(0, 200));
            template.AddComponent<Image>().color = ColCard;
            var tScroll = template.AddComponent<ScrollRect>();
            template.SetActive(false);

            var viewport = NewUi("Viewport", template.transform);
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0.001f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = NewUi("Content", viewport.transform);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 40);

            var item = NewUi("Item", content.transform);
            var irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(1, 0.5f);
            irt.sizeDelta = new Vector2(0, 36);
            var itemToggle = item.AddComponent<Toggle>();
            var itemBg = NewUi("Item Background", item.transform);
            Stretch(itemBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            itemBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            itemToggle.targetGraphic = itemBg.GetComponent<Image>();
            var itemLabelGo = NewUi("Item Label", item.transform);
            Stretch(itemLabelGo, Vector2.zero, Vector2.one, new Vector2(14, 1), new Vector2(-10, -1));
            var itemLabelT = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemLabelT.fontSize = 22; itemLabelT.color = ColGold;
            itemLabelT.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(itemLabelT);

            tScroll.content = crt; tScroll.viewport = viewport.GetComponent<RectTransform>();
            tScroll.horizontal = false;
            dd.template = template.GetComponent<RectTransform>();
            dd.itemText = itemLabelT;

            dd.options = new List<TMP_Dropdown.OptionData>();
            foreach (var o in options) dd.options.Add(new TMP_Dropdown.OptionData(o));
            dd.value = Mathf.Clamp(selected, 0, options.Length - 1);
            dd.RefreshShownValue();
            return dd;
        }

        private Toggle AddToggle(Transform parent, string label, bool on)
        {
            var row = NewUi("Toggle_" + label, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 48;

            var toggle = row.AddComponent<Toggle>();
            toggle.isOn = on;

            var box = NewUi("Box", row.transform);
            Anchored(box, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(0, 0), new Vector2(36, 36));
            box.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var check = NewUi("Check", box.transform);
            Stretch(check, Vector2.zero, Vector2.one, new Vector2(6, 6), new Vector2(-6, -6));
            var checkImg = check.AddComponent<Image>();
            checkImg.color = ColEmber;
            toggle.graphic = checkImg;
            toggle.targetGraphic = box.GetComponent<Image>();

            var lblGo = NewUi("Lbl", row.transform);
            Anchored(lblGo, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(50, 0), new Vector2(-50, 0));
            var t = lblGo.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 24; t.color = ColGold;
            t.alignment = TextAlignmentOptions.MidlineLeft; t.enableWordWrapping = true;
            ApplyFont(t);
            return toggle;
        }

        private void AddPrimaryButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = NewUi("Btn_" + label, parent);
            go.AddComponent<LayoutElement>().preferredHeight = 60;
            go.AddComponent<Image>().color = new Color(1f, 0.54f, 0.17f, 0.16f);
            go.AddComponent<Button>().onClick.AddListener(onClick);
            var lblGo = NewUi("Lbl", go.transform);
            Stretch(lblGo, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var t = lblGo.AddComponent<TextMeshProUGUI>();
            t.text = label; t.fontSize = 28; t.color = ColGold;
            t.alignment = TextAlignmentOptions.Center; ApplyFont(t);
        }

        // ---------- мини-утилиты uGUI ----------

        private static void ApplyFont(TMP_Text t)
        {
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        }

        // Горизонтальный градиент: насыщенный слева → прозрачный справа. Нужен для
        // мягкого затемнения под текстом меню (без жёсткой панели).
        private static Sprite MakeHorizontalFade(Color left, int w = 256)
        {
            var tex = new Texture2D(w, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < w; x++)
            {
                float a = 1f - x / (float)(w - 1); // 1 слева → 0 справа
                a *= a;                            // мягче спад
                tex.SetPixel(x, 0, new Color(left.r, left.g, left.b, left.a * a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, 1), new Vector2(0.5f, 0.5f), 100f);
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

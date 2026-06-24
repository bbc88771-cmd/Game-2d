using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Экран выбора персонажа (фаза 6 порта): три колонки — характеристики (навыки
    /// на клавишах, секретная 5-я с замком по прогрессу, плюсы/минусы), центр
    /// (портрет, имя, роль, переключатель героев, кнопка выбора) и предыстория.
    /// Данные — в <see cref="GameHeroes"/>; прогресс секретки — в <see cref="GameProgress"/>.
    /// UI строится из кода. Сборку сцены см. Sunset → Build Hero Select Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetHeroSelect : MonoBehaviour
    {
        [Tooltip("Выбранный по умолчанию герой (id).")]
        public string heroId = "tessi";

        /// <summary>Вызывается, когда игрок подтвердил выбор героя (id).</summary>
        public event Action<string> HeroPicked;

        private static readonly Color ColBg = new Color(0.05f, 0.04f, 0.03f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.54f, 0.17f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColMuted = new Color(0.95f, 0.91f, 0.81f, 0.55f);
        private static readonly Color ColPanel = new Color(0.04f, 0.03f, 0.02f, 0.55f);
        private static readonly Color ColPros = new Color(0.55f, 0.85f, 0.55f, 1f);
        private static readonly Color ColCons = new Color(1f, 0.5f, 0.45f, 1f);
        private static readonly Color ColKey = new Color(1f, 0.54f, 0.17f, 0.9f);

        private RectTransform _root;
        private RectTransform _leftContent, _midPanel, _rightContent;
        private int _clearedCount;
        private bool _secret;

        private void Awake()
        {
            var cleared = ProgressSave.Load();
            _clearedCount = GameProgress.ClearedCount(cleared);
            _secret = GameProgress.SecretUnlocked(cleared);
            BuildShell();
            RenderHero(heroId);
        }

        // ---------- рендер выбранного героя ----------

        private void RenderHero(string id)
        {
            heroId = id;
            var h = GameHeroes.ById(id);

            // ----- левая колонка: характеристики -----
            ClearChildren(_leftContent);
            AddHeader(_leftContent, "Характеристики", 30);
            AddHeader(_leftContent, "Навыки (на клавишах)", 22);
            for (int i = 0; i < h.skills.Length; i++)
                AddSkill(_leftContent, i < GameHeroes.SkillKeys.Length ? GameHeroes.SkillKeys[i] : "",
                    h.skills[i].name, h.skills[i].desc, false);
            // секретка
            if (_secret)
                AddSkill(_leftContent, "5", h.secret.name, h.secret.desc, true);
            else
                AddSkill(_leftContent, "5", "Секретная способность 🔒",
                    $"Откроется после прохождения игры на всех сложностях (лёгкая, обычная, сложная, хард). Пройдено: {_clearedCount}/4.", true);

            AddParagraph(_leftContent, "Способности усиливаются через «Кольцо навыков»: например, поле " +
                "обнаружения опасности расширяет радиус и начинает подсвечивать руду и деревни.", ColMuted, 18);

            AddHeader(_leftContent, "Плюсы", 22, ColPros);
            foreach (var p in h.pros) AddBullet(_leftContent, p, ColGold);
            AddHeader(_leftContent, "Минусы", 22, ColCons);
            foreach (var c in h.cons) AddBullet(_leftContent, c, ColGold);

            // ----- центр: портрет, имя, роль, переключатель, кнопка -----
            ClearChildren(_midPanel);
            var portrait = NewUi("Portrait", _midPanel);
            Anchored(portrait, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -10), new Vector2(280, 280));
            var pImg = portrait.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("Sunset/" + h.portrait);
            if (sprite != null) { pImg.sprite = sprite; pImg.color = Color.white; pImg.preserveAspect = true; }
            else pImg.color = new Color(0.12f, 0.1f, 0.09f, 1f);

            var nameGo = NewUi("Name", _midPanel);
            Anchored(nameGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -300), new Vector2(0, 50));
            var nameT = nameGo.AddComponent<TextMeshProUGUI>();
            nameT.text = h.name; nameT.fontSize = 40; nameT.color = ColGold;
            nameT.alignment = TextAlignmentOptions.Center; ApplyFont(nameT);

            var roleGo = NewUi("Role", _midPanel);
            Anchored(roleGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, -350), new Vector2(0, 34));
            var roleT = roleGo.AddComponent<TextMeshProUGUI>();
            roleT.text = h.role; roleT.fontSize = 22; roleT.color = ColEmber;
            roleT.alignment = TextAlignmentOptions.Center; ApplyFont(roleT);

            // переключатель героев
            var sw = NewUi("Switch", _midPanel);
            Anchored(sw, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -400), new Vector2(GameHeroes.Count * 78, 70));
            var hl = sw.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 8; hl.childAlignment = TextAnchor.MiddleCenter;
            hl.childControlWidth = true; hl.childControlHeight = true;
            hl.childForceExpandWidth = true; hl.childForceExpandHeight = true;
            foreach (var hd in GameHeroes.All)
            {
                var b = NewUi("Sw_" + hd.id, sw.transform);
                b.AddComponent<Image>().color = hd.id == id ? new Color(1f, 0.54f, 0.17f, 0.3f) : ColPanel;
                string hid = hd.id;
                b.AddComponent<Button>().onClick.AddListener(() => RenderHero(hid));
                var bl = NewUi("Lbl", b.transform);
                Stretch(bl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var bt = bl.AddComponent<TextMeshProUGUI>();
                bt.text = h.name.Length > 0 && hd.id == id ? Initial(hd.name) : Initial(hd.name);
                bt.fontSize = 26; bt.color = hd.id == id ? ColGold : ColMuted;
                bt.alignment = TextAlignmentOptions.Center; ApplyFont(bt);
            }

            var pick = NewUi("Pick", _midPanel);
            Anchored(pick, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -490), new Vector2(420, 70));
            pick.AddComponent<Image>().color = new Color(1f, 0.54f, 0.17f, 0.22f);
            pick.AddComponent<Button>().onClick.AddListener(() => OnPick(h.id));
            var pl = NewUi("Lbl", pick.transform);
            Stretch(pl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var plt = pl.AddComponent<TextMeshProUGUI>();
            plt.text = "Выбрать: " + h.name; plt.fontSize = 28; plt.color = ColGold;
            plt.alignment = TextAlignmentOptions.Center; ApplyFont(plt);

            // ----- правая колонка: предыстория -----
            ClearChildren(_rightContent);
            AddHeader(_rightContent, "Предыстория", 30);
            foreach (var para in h.story.Split(new[] { "\n\n" }, StringSplitOptions.None))
                AddParagraph(_rightContent, para, ColGold, 20);
            AddHeader(_rightContent, "Как попал на остров", 22, ColEmber);
            AddParagraph(_rightContent, h.arrival, ColGold, 20);
        }

        private void OnPick(string id)
        {
            Debug.Log($"[Sunset] Выбран герой: {GameHeroes.ById(id).name} ({id}).");
            // запоминаем героя и черновик сейва, копим прогресс сложностей (порт startWorldStub)
            PlayerPrefs.SetString("sunset_hero", id);
            PlayerPrefs.SetString("sunset_save", "{\"hero\":\"" + id + "\"}");
            PlayerPrefs.Save();
            var settings = SettingsSave.Load();
            ProgressSave.MarkCleared(settings.difficulty);
            HeroPicked?.Invoke(id);
            SceneFlow.Go(SceneFlow.World);
        }

        // ---------- оболочка экрана ----------

        private void BuildShell()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("HeroCanvas");
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

            // шапка
            var header = NewUi("Header", _root);
            Anchored(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -16), new Vector2(600, 60));
            var ht = header.AddComponent<TextMeshProUGUI>();
            ht.text = "Выбор персонажа"; ht.fontSize = 40; ht.color = ColGold;
            ht.alignment = TextAlignmentOptions.Center; ApplyFont(ht);

            var back = NewUi("Back", _root);
            Anchored(back, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -20), new Vector2(180, 50));
            back.AddComponent<Image>().color = ColPanel;
            back.AddComponent<Button>().onClick.AddListener(() => SceneFlow.Go(SceneFlow.MainMenu));
            var bl = NewUi("Lbl", back.transform);
            Stretch(bl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bt = bl.AddComponent<TextMeshProUGUI>();
            bt.text = "← Назад"; bt.fontSize = 22; bt.color = ColGold;
            bt.alignment = TextAlignmentOptions.Center; ApplyFont(bt);

            // три колонки
            _leftContent = MakeScrollColumn("Left", new Vector2(40, 40), new Vector2(600, -110));
            _rightContent = MakeScrollColumn("Right", new Vector2(1320, 40), new Vector2(1880, -110));

            var mid = NewUi("Mid", _root);
            var mrt = mid.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(0, 0); mrt.anchorMax = new Vector2(0, 1);
            mrt.pivot = new Vector2(0.5f, 0.5f);
            mrt.offsetMin = new Vector2(620, 40); mrt.offsetMax = new Vector2(0, -110);
            mrt.sizeDelta = new Vector2(680, mrt.sizeDelta.y);
            // фиксированная ширина центра
            mrt.anchorMin = new Vector2(0.5f, 0); mrt.anchorMax = new Vector2(0.5f, 1);
            mrt.anchoredPosition = new Vector2(0, -35); mrt.sizeDelta = new Vector2(680, -110);
            _midPanel = mrt;
        }

        /// <summary>Колонка с вертикальной прокруткой (для длинных списков/текста).</summary>
        private RectTransform MakeScrollColumn(string name, Vector2 offMin, Vector2 offMax)
        {
            var scrollGo = NewUi(name, _root);
            var srt = scrollGo.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = offMin; srt.offsetMax = offMax;
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 28f;

            var viewport = NewUi("Viewport", scrollGo.transform);
            Stretch(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var vpImg = viewport.AddComponent<Image>(); vpImg.color = new Color(0, 0, 0, 0.001f);
            viewport.AddComponent<RectMask2D>();

            var content = NewUi("Content", viewport.transform);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.anchoredPosition = Vector2.zero;
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.childControlWidth = true; v.childControlHeight = false;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = crt;
            return crt;
        }

        // ---------- виджеты ----------

        private void AddHeader(RectTransform parent, string text, int size, Color? color = null)
        {
            var go = NewUi("H", parent);
            go.AddComponent<LayoutElement>().preferredHeight = size + 14;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color ?? ColEmber;
            t.alignment = TextAlignmentOptions.TopLeft; ApplyFont(t);
        }

        private void AddSkill(RectTransform parent, string key, string name, string desc, bool secret)
        {
            var row = NewUi("Skill", parent);
            row.AddComponent<LayoutElement>().preferredHeight = 64;
            row.AddComponent<Image>().color = secret ? new Color(1f, 0.54f, 0.17f, 0.1f) : ColPanel;

            var keyGo = NewUi("Key", row.transform);
            Anchored(keyGo, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(10, 0), new Vector2(40, 40));
            keyGo.AddComponent<Image>().color = new Color(0.16f, 0.12f, 0.07f, 1f);
            var keyLbl = NewUi("Lbl", keyGo.transform);
            Stretch(keyLbl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var keyT = keyLbl.AddComponent<TextMeshProUGUI>();
            keyT.text = key; keyT.fontSize = 20; keyT.color = ColKey;
            keyT.alignment = TextAlignmentOptions.Center; ApplyFont(keyT);

            var txt = NewUi("Txt", row.transform);
            Anchored(txt, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0.5f),
                new Vector2(62, 4), new Vector2(-72, -8));
            var t = txt.AddComponent<TextMeshProUGUI>();
            t.text = $"<b>{name}</b>\n<size=16><color=#a89b8a>{desc}</color></size>";
            t.fontSize = 20; t.color = ColGold; t.richText = true; t.enableWordWrapping = true;
            t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);
        }

        private void AddBullet(RectTransform parent, string text, Color color)
            => AddParagraph(parent, "•  " + text, color, 19);

        private void AddParagraph(RectTransform parent, string text, Color color, int size)
        {
            var go = NewUi("P", parent);
            go.AddComponent<LayoutElement>().minHeight = size + 8;
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

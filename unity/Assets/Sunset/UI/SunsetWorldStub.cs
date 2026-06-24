using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Экран входа в мир — «Стартовый остров» (порт startWorldStub из веб-версии).
    /// Показывается после выбора героя: сводка (имя/герой/сложность) и прогресс
    /// секретной способности. Это место будущего геймплейного прототипа (движение,
    /// сбор, стройка, бой) — пока заглушка с кнопкой «В меню».
    ///
    /// Читает выбор из сохранений: имя — из <see cref="NekoState"/>, герой — из
    /// PlayerPrefs (sunset_hero), сложность — из настроек, прогресс — из
    /// <see cref="GameProgress"/>. Сцена: Sunset → Build World Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetWorldStub : MonoBehaviour
    {
        private static readonly Color ColBg = new Color(0.05f, 0.04f, 0.03f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.54f, 0.17f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColMuted = new Color(0.95f, 0.91f, 0.81f, 0.6f);
        private static readonly Color ColPanel = new Color(0.04f, 0.03f, 0.02f, 0.6f);

        private RectTransform _root;
        private NekoPresence _neko;
        private static readonly string[] DemoKeys = { "night", "wounded", "resource_low", "discovery", "boss", "build" };

        private void Awake()
        {
            BuildUi();
            // «Некий» здесь, в мире — главный канал общения: редкие реплики сами по
            // себе и по триггерам события. (В прототипе триггеры на клавишах 1–6.)
            _neko = gameObject.AddComponent<NekoPresence>();
        }

        private void Update()
        {
            if (_neko == null) return;
            for (int i = 0; i < DemoKeys.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) _neko.Trigger(DemoKeys[i]);
        }

        private void BuildUi()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("WorldCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = canvasGo.GetComponent<RectTransform>();

            // фон — стартовая локация, если есть; иначе тёмная заливка
            var bg = NewUi("Background", _root);
            Stretch(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bgImg = bg.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("Sunset/loc_start");
            if (sprite != null) { bgImg.sprite = sprite; bgImg.color = new Color(0.5f, 0.5f, 0.5f, 1f); bgImg.preserveAspect = false; }
            else bgImg.color = ColBg;
            bgImg.raycastTarget = false;

            var shade = NewUi("Shade", _root);
            Stretch(shade, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var shadeImg = shade.AddComponent<Image>();
            shadeImg.color = new Color(0f, 0f, 0f, 0.62f); shadeImg.raycastTarget = false;

            // карточка по центру
            var card = NewUi("Card", _root);
            Anchored(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000, 560));
            card.AddComponent<Image>().color = ColPanel;
            var v = card.AddComponent<VerticalLayoutGroup>();
            v.spacing = 18; v.padding = new RectOffset(50, 50, 40, 40);
            v.childControlWidth = true; v.childControlHeight = false;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            v.childAlignment = TextAnchor.UpperCenter;

            // данные из сохранений
            var neko = SunsetSave.Load();
            var settings = SettingsSave.Load();
            string name = neko != null ? neko.knownName : null;
            string heroId = PlayerPrefs.GetString("sunset_hero", "tessi");
            var hero = GameHeroes.ById(heroId);
            int done = ProgressSave.ClearedCount();
            bool secret = ProgressSave.SecretUnlocked();

            AddText(card.transform, "Стартовый остров", 44, ColEmber, TextAlignmentOptions.Center, 60);

            string who = string.IsNullOrEmpty(name) ? "Ты" : $"«{name}», т";
            AddText(card.transform,
                $"{who}ы очнулся в разорванном мире после катаклизма.",
                26, ColGold, TextAlignmentOptions.Center, 44);

            AddText(card.transform,
                $"Герой: <b><color=#f3e7cf>{hero.name}</color></b>   ·   Сложность: <b><color=#f3e7cf>{GameDifficulties.NameOf(settings.difficulty)}</color></b>",
                24, ColGold, TextAlignmentOptions.Center, 40);

            AddText(card.transform,
                "Здесь начинается мир «Sunset of the World». Геймплейный прототип " +
                "(движение, сбор ресурсов, стройка, бой) — следующий шаг разработки.",
                22, ColMuted, TextAlignmentOptions.Center, 80);

            string note = secret
                ? "★ Все сложности пройдены — секретная 5-я способность открыта!"
                : $"Прогресс секретной способности: пройдено {done}/4 сложностей.";
            AddText(card.transform, note, 22, secret ? ColEmber : ColMuted, TextAlignmentOptions.Center, 40);

            // кнопка «В меню»
            var btn = NewUi("ToMenu", card.transform);
            btn.AddComponent<LayoutElement>().preferredHeight = 66;
            btn.AddComponent<Image>().color = new Color(1f, 0.54f, 0.17f, 0.22f);
            btn.AddComponent<Button>().onClick.AddListener(() => SceneFlow.Go(SceneFlow.MainMenu));
            AddText(btn.transform, "В меню", 28, ColGold, TextAlignmentOptions.Center, 0, stretch: true);

            // подсказка про присутствие «Некого» в мире (демо-триггеры)
            var hint = NewUi("Hint", _root);
            Anchored(hint, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 36), new Vector2(1200, 30));
            var hintT = hint.AddComponent<TextMeshProUGUI>();
            hintT.text = "«Некий» иногда заговорит сам. Клавиши 1–6 — демо-триггеры событий (ночь/рана/нехватка/находка/босс/стройка).";
            hintT.fontSize = 17; hintT.color = new Color(0.95f, 0.91f, 0.81f, 0.4f);
            hintT.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null) hintT.font = TMP_Settings.defaultFontAsset;
        }

        // ---------- утилиты ----------

        private void AddText(Transform parent, string text, int size, Color color,
            TextAlignmentOptions align, float height, bool stretch = false)
        {
            var go = NewUi("T", parent);
            if (stretch) Stretch(go, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            else go.AddComponent<LayoutElement>().preferredHeight = height;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.richText = true;
            t.enableWordWrapping = true; t.alignment = align;
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

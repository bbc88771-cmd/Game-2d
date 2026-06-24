using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.UI
{
    /// <summary>
    /// Экран выбора сложности новой игры (порт chooseDifficulty из веб-версии).
    /// Идёт между интро «Некого» и катсценой: выбор сохраняется в настройки и
    /// дальше используется в лобби/мире. Список из <see cref="GameDifficulties"/>.
    /// UI строится из кода. Сцена: Sunset → Build Difficulty Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetDifficultySelect : MonoBehaviour
    {
        [Tooltip("Сцена после кнопки «Дальше →». По умолчанию — катсцена.")]
        public string nextScene = SceneFlow.Cutscene;

        private static readonly Color ColBg = new Color(0.05f, 0.04f, 0.03f, 1f);
        private static readonly Color ColEmber = new Color(1f, 0.54f, 0.17f, 1f);
        private static readonly Color ColGold = new Color(0.95f, 0.91f, 0.81f, 1f);
        private static readonly Color ColMuted = new Color(0.95f, 0.91f, 0.81f, 0.55f);
        private static readonly Color ColPanel = new Color(0.04f, 0.03f, 0.02f, 0.55f);
        private static readonly Color ColSelected = new Color(1f, 0.54f, 0.17f, 0.22f);

        private GameSettings _settings;
        private string _picked;
        private RectTransform _list;
        private RectTransform _root;

        private void Awake()
        {
            _settings = SettingsSave.Load();
            _picked = GameDifficulties.IsValid(_settings.difficulty) ? _settings.difficulty : "normal";
            BuildUi();
            RenderList();
        }

        private void RenderList()
        {
            ClearChildren(_list);
            foreach (var d in GameDifficulties.All)
            {
                bool sel = d.id == _picked;
                var row = NewUi("Opt_" + d.id, _list);
                row.AddComponent<LayoutElement>().preferredHeight = 78;
                var img = row.AddComponent<Image>();
                img.color = sel ? ColSelected : ColPanel;
                var btn = row.AddComponent<Button>();
                string id = d.id;
                btn.onClick.AddListener(() => { _picked = id; RenderList(); });

                var txt = NewUi("Txt", row.transform);
                Stretch(txt, Vector2.zero, Vector2.one, new Vector2(22, 6), new Vector2(-22, -6));
                var t = txt.AddComponent<TextMeshProUGUI>();
                t.text = $"<b>{d.name}</b>{(sel ? "   <color=#ff8a2b>◆</color>" : "")}\n<size=17><color=#a89b8a>{d.sub}</color></size>";
                t.fontSize = 26; t.color = ColGold; t.richText = true; t.enableWordWrapping = true;
                t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);
            }
        }

        private void OnNext()
        {
            _settings.difficulty = _picked;
            SettingsSave.Save(_settings);
            Debug.Log($"[Sunset] Выбрана сложность: {GameDifficulties.NameOf(_picked)}.");
            if (!string.IsNullOrEmpty(nextScene)) SceneFlow.Go(nextScene);
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

            var canvasGo = new GameObject("DifficultyCanvas");
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

            // заголовок + подзаголовок
            var header = NewUi("Header", _root);
            Anchored(header, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -60), new Vector2(900, 60));
            var ht = header.AddComponent<TextMeshProUGUI>();
            ht.text = "Новая игра — сложность"; ht.fontSize = 44; ht.color = ColGold;
            ht.alignment = TextAlignmentOptions.Center; ApplyFont(ht);

            var sub = NewUi("Sub", _root);
            Anchored(sub, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -120), new Vector2(1000, 40));
            var st = sub.AddComponent<TextMeshProUGUI>();
            st.text = "Выберите сложность. На высоких — больше боссов, квестов и предметов.";
            st.fontSize = 22; st.color = ColMuted; st.alignment = TextAlignmentOptions.Center; ApplyFont(st);

            // список опций (по центру)
            var listGo = NewUi("List", _root);
            Anchored(listGo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 20), new Vector2(900, 480));
            var v = listGo.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12; v.childControlWidth = true; v.childControlHeight = false;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            _list = listGo.GetComponent<RectTransform>();

            // кнопка «Дальше →»
            var next = NewUi("Next", _root);
            Anchored(next, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 70), new Vector2(360, 70));
            next.AddComponent<Image>().color = ColSelected;
            next.AddComponent<Button>().onClick.AddListener(OnNext);
            var nl = NewUi("Lbl", next.transform);
            Stretch(nl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var nt = nl.AddComponent<TextMeshProUGUI>();
            nt.text = "Дальше →"; nt.fontSize = 30; nt.color = ColGold;
            nt.alignment = TextAlignmentOptions.Center; ApplyFont(nt);

            // назад в меню
            var back = NewUi("Back", _root);
            Anchored(back, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(40, -40), new Vector2(180, 50));
            back.AddComponent<Image>().color = ColPanel;
            back.AddComponent<Button>().onClick.AddListener(() => SceneFlow.Go(SceneFlow.MainMenu));
            var bl = NewUi("Lbl", back.transform);
            Stretch(bl, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bt = bl.AddComponent<TextMeshProUGUI>();
            bt.text = "← В меню"; bt.fontSize = 22; bt.color = ColGold;
            bt.alignment = TextAlignmentOptions.Center; ApplyFont(bt);
        }

        // ---------- утилиты uGUI ----------

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

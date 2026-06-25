using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.Game
{
    /// <summary>
    /// Строительство (по ТЗ): меню крафтов с доступными постройками и их стоимостью →
    /// выбор → установка призраком в удобное место → стройплощадка с табличкой
    /// ресурсов → внесение ресурсов → анимация постройки. Управление: C — меню,
    /// ЛКМ — поставить, ПКМ — отмена, E — внести ресурсы у площадки.
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildController : MonoBehaviour
    {
        public float depositRange = 3.0f;

        private Inventory _inv;
        private Camera _cam;
        private Func<Vector2, bool> _isOnLand;
        private Transform _player;

        private Canvas _ui;
        private GameObject _menu;
        private TextMeshProUGUI _resHud;
        private BuildingGhost _ghost;
        private BuildingDef _placing;
        private readonly List<ConstructionSiteView> _sites = new List<ConstructionSiteView>();

        public bool IsPlacing => _ghost != null;

        public void Init(Camera cam, Inventory inv, Func<Vector2, bool> isOnLand, Transform player)
        {
            _cam = cam; _inv = inv; _isOnLand = isOnLand; _player = player;
            EnsureEventSystem();
            BuildUi();
        }

        private void Update()
        {
            RefreshResHud();

            if (Input.GetKeyDown(KeyCode.C) && !IsPlacing) ToggleMenu();

            if (IsPlacing)
            {
                // ЛКМ по миру (не по UI) — поставить, если место подходит
                if (Input.GetMouseButtonDown(0) && !PointerOverUi() && _ghost.ValidHere)
                    PlaceHere();
                if (Input.GetMouseButtonDown(1)) CancelPlacement();
            }

            // внести ресурсы в ближайшую недостроенную площадку
            if (Input.GetKeyDown(KeyCode.E) && _player != null)
            {
                var site = NearestSite(_player.position, depositRange);
                if (site != null) site.Deposit(_inv);
            }
        }

        // ---------- меню/установка ----------

        private void ToggleMenu() { if (_menu != null) _menu.SetActive(!_menu.activeSelf); }

        public void StartPlacement(BuildingDef def)
        {
            if (def == null) return;
            CancelPlacement();
            if (_menu != null) _menu.SetActive(false);
            _placing = def;
            var go = new GameObject("BuildGhost");
            _ghost = go.AddComponent<BuildingGhost>();
            _ghost.Init(Resources.Load<Sprite>("Sunset/" + def.sprite), _isOnLand);
        }

        public void CancelPlacement()
        {
            if (_ghost != null) Destroy(_ghost.gameObject);
            _ghost = null; _placing = null;
        }

        private void PlaceHere()
        {
            var pos = _ghost.WorldPos;
            var def = _placing;
            CancelPlacement();

            var go = new GameObject("Site_" + def.id);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var view = go.AddComponent<ConstructionSiteView>();
            view.Init(def, Resources.Load<Sprite>("Sunset/" + def.sprite));
            _sites.Add(view);
        }

        private ConstructionSiteView NearestSite(Vector2 from, float range)
        {
            ConstructionSiteView best = null;
            float bestD = range * range;
            foreach (var s in _sites)
            {
                if (s == null || s.IsComplete) continue;
                float d = ((Vector2)s.Position - from).sqrMagnitude;
                if (d <= bestD) { bestD = d; best = s; }
            }
            return best;
        }

        // ---------- UI ----------

        private void BuildUi()
        {
            var canvasGo = new GameObject("BuildUI");
            canvasGo.transform.SetParent(transform, false);
            _ui = canvasGo.AddComponent<Canvas>();
            _ui.renderMode = RenderMode.ScreenSpaceOverlay;
            _ui.sortingOrder = 40;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.GetComponent<RectTransform>();

            // HUD ресурсов (сверху справа)
            var hudGo = NewUi("ResHud", root);
            Anchored(hudGo, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-30, -24), new Vector2(560, 40));
            _resHud = hudGo.AddComponent<TextMeshProUGUI>();
            _resHud.fontSize = 26; _resHud.color = new Color(0.95f, 0.91f, 0.81f);
            _resHud.alignment = TextAlignmentOptions.Right; ApplyFont(_resHud);

            // меню крафтов (слева, скрыто по умолчанию)
            _menu = NewUi("CraftMenu", root);
            Anchored(_menu, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(30, 0), new Vector2(560, 640));
            _menu.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 0.92f);
            var v = _menu.AddComponent<VerticalLayoutGroup>();
            v.spacing = 10; v.padding = new RectOffset(20, 20, 20, 20);
            v.childControlWidth = true; v.childControlHeight = false;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            AddLabel(_menu.transform, "Строительство", 32, new Color(1f, 0.7f, 0.4f), 48);
            AddLabel(_menu.transform, "Выбери постройку и поставь в удобное место.", 18,
                new Color(0.95f, 0.91f, 0.81f, 0.6f), 30);

            foreach (var def in BuildRecipes.All)
                AddRecipeButton(_menu.transform, def);

            _menu.SetActive(false);
        }

        private void AddRecipeButton(Transform parent, BuildingDef def)
        {
            var go = NewUi("Recipe_" + def.id, parent);
            go.AddComponent<LayoutElement>().preferredHeight = 88;
            go.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => StartPlacement(def));

            var lblGo = NewUi("Lbl", go.transform);
            Stretch(lblGo, Vector2.zero, Vector2.one, new Vector2(16, 6), new Vector2(-16, -6));
            var t = lblGo.AddComponent<TextMeshProUGUI>();
            t.text = $"<b>{def.name}</b>\n<size=17><color=#a89b8a>{CostText(def)}</color></size>";
            t.fontSize = 24; t.color = new Color(0.95f, 0.91f, 0.81f); t.richText = true;
            t.alignment = TextAlignmentOptions.MidlineLeft; ApplyFont(t);
        }

        private static string CostText(BuildingDef def)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < def.cost.Length; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(def.cost[i].amount).Append(' ').Append(ResourceId.Name(def.cost[i].res).ToLowerInvariant());
            }
            return sb.ToString();
        }

        private void RefreshResHud()
        {
            if (_resHud == null) return;
            var sb = new StringBuilder();
            for (int i = 0; i < ResourceId.All.Length; i++)
            {
                if (i > 0) sb.Append("    ");
                string id = ResourceId.All[i];
                sb.Append(ResourceId.Name(id)).Append(": ").Append(_inv != null ? _inv.Get(id) : 0);
            }
            _resHud.text = sb.ToString();
        }

        private void AddLabel(Transform parent, string text, int size, Color color, float height)
        {
            var go = NewUi("L", parent);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.enableWordWrapping = true;
            t.alignment = TextAlignmentOptions.Left; ApplyFont(t);
        }

        // ---------- утилиты ----------

        private static bool PointerOverUi()
            => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
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

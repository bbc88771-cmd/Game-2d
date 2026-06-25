using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Sunset.Core;

namespace Sunset.Game
{
    /// <summary>
    /// Стройплощадка в мире: полупрозрачный «недострой» постройки + табличка с
    /// требуемыми ресурсами (ячейки-полоски заполняются по мере внесения). Когда все
    /// ячейки заполнены — проигрывается простая анимация постройки. По ТЗ.
    /// Логика учёта ресурсов — в <see cref="Core.ConstructionSite"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class ConstructionSiteView : MonoBehaviour
    {
        private ConstructionSite _site;
        private SpriteRenderer _sr;
        private Canvas _board;
        private readonly Dictionary<string, Image> _fills = new Dictionary<string, Image>();
        private readonly Dictionary<string, TextMeshProUGUI> _labels = new Dictionary<string, TextMeshProUGUI>();
        private bool _built;

        public bool IsComplete => _site != null && _site.IsComplete;
        public Vector2 Position => transform.position;

        public void Init(BuildingDef def, Sprite buildingSprite)
        {
            _site = new ConstructionSite(def);

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = buildingSprite != null ? buildingSprite : PlaceholderSprites.Solid(new Color(0.5f, 0.4f, 0.3f), 32);
            _sr.color = new Color(1f, 1f, 1f, 0.4f); // недострой — полупрозрачный
            _sr.sortingOrder = 7;

            float topY = _sr.sprite != null ? _sr.sprite.bounds.size.y * 0.5f : 1.5f;
            BuildBoard(def, new Vector3(0, topY + 0.4f, 0));
            Refresh();
        }

        /// <summary>Внести доступные ресурсы из инвентаря (постепенно или сразу).</summary>
        public bool Deposit(Inventory inv)
        {
            if (_site == null || _built || inv == null) return false;
            bool any = false;
            foreach (var c in _site.def.cost)
            {
                int need = _site.Remaining(c.res);
                if (need <= 0) continue;
                int have = inv.Get(c.res);
                int move = Mathf.Min(need, have);
                if (move > 0)
                {
                    inv.Take(c.res, move);
                    _site.Deposit(c.res, move);
                    any = true;
                }
            }
            if (any) Refresh();
            if (_site.IsComplete && !_built) StartCoroutine(BuildAnim());
            return any;
        }

        private void Refresh()
        {
            foreach (var c in _site.def.cost)
            {
                int dep = _site.Deposited(c.res), req = c.amount;
                if (_fills.TryGetValue(c.res, out var fill))
                    fill.rectTransform.anchorMax = new Vector2(req > 0 ? (float)dep / req : 1f, 1f);
                if (_labels.TryGetValue(c.res, out var lbl))
                {
                    bool full = dep >= req;
                    lbl.text = $"{ResourceId.Name(c.res)}  {dep} / {req}";
                    lbl.color = full ? new Color(0.55f, 0.9f, 0.55f) : new Color(0.95f, 0.91f, 0.81f);
                }
            }
        }

        private IEnumerator BuildAnim()
        {
            _built = true;
            if (_board != null) Destroy(_board.gameObject);

            // простая анимация постройки: «вырастает» снизу + проявляется + лёгкий поскок
            float t = 0f, dur = 0.9f;
            Vector3 baseScale = transform.localScale;
            while (t < dur)
            {
                float k = t / dur;
                float pop = 1f + Mathf.Sin(k * Mathf.PI) * 0.08f;        // лёгкий поскок
                transform.localScale = new Vector3(baseScale.x * pop, baseScale.y * Mathf.Lerp(0.6f, 1f, k) * pop, 1f);
                _sr.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.4f, 1f, k));
                t += Time.deltaTime;
                yield return null;
            }
            transform.localScale = baseScale;
            _sr.color = Color.white;
            // готовый дом получает коллайдер-основание
            float w = _sr.sprite.bounds.size.x, h = _sr.sprite.bounds.size.y;
            var col = gameObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w * 0.7f, h * 0.28f);
            col.offset = new Vector2(0f, -h * 0.5f + h * 0.16f);
        }

        // ---------- табличка (world-space canvas) ----------

        private void BuildBoard(BuildingDef def, Vector3 localPos)
        {
            var canvasGo = new GameObject("Board");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = localPos;
            canvasGo.transform.localScale = Vector3.one * 0.01f; // 100 px = 1 юнит
            _board = canvasGo.AddComponent<Canvas>();
            _board.renderMode = RenderMode.WorldSpace;
            _board.sortingOrder = 25;
            var rt = canvasGo.GetComponent<RectTransform>();
            int rows = def.cost.Length;
            rt.sizeDelta = new Vector2(360, 70 + rows * 52);

            var panel = NewUi("Panel", rt);
            Stretch(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            panel.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.92f);

            var title = NewUi("Title", rt);
            Anchored(title, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -14), new Vector2(340, 44));
            var tt = title.AddComponent<TextMeshProUGUI>();
            tt.text = def.name; tt.fontSize = 30; tt.color = new Color(1f, 0.7f, 0.4f);
            tt.alignment = TextAlignmentOptions.Center; ApplyFont(tt);

            for (int i = 0; i < def.cost.Length; i++)
            {
                var c = def.cost[i];
                float y = -64 - i * 52;

                // подпись «Имя dep/req»
                var lblGo = NewUi("Lbl_" + c.res, rt);
                Anchored(lblGo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                    new Vector2(0, y), new Vector2(-40, 26));
                var lbl = lblGo.AddComponent<TextMeshProUGUI>();
                lbl.fontSize = 22; lbl.alignment = TextAlignmentOptions.Left; ApplyFont(lbl);
                _labels[c.res] = lbl;

                // полоска-ячейка (фон + заполнение)
                var barBg = NewUi("Bar_" + c.res, rt);
                Anchored(barBg, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                    new Vector2(0, y - 28), new Vector2(-40, 16));
                barBg.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

                var fillGo = NewUi("Fill", barBg.transform);
                var frt = fillGo.GetComponent<RectTransform>();
                frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(0, 1);
                frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
                var fillImg = fillGo.AddComponent<Image>();
                fillImg.color = PlaceholderSprites.ResourceColor(c.res);
                _fills[c.res] = fillImg;
            }
        }

        // ---------- утилиты uGUI ----------

        private static void ApplyFont(TMP_Text t)
        {
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
        }

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

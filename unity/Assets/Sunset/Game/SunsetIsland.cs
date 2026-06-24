using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Sunset.Core;
using Sunset.UI;

namespace Sunset.Game
{
    /// <summary>
    /// Прототип стартового острова (вид сверху). Строит игровую сцену из кода:
    /// фон-остров (спрайт loc_start), границы, игрок с физикой, несколько предметов,
    /// враг-преследователь, камера-следование, HUD и присутствие «Некого». Временная
    /// графика генерируется из кода — арт/карту/персонажей можно заменить позже,
    /// не трогая логику. Управление: WASD/стрелки, Esc — в меню.
    ///
    /// Сцена: Sunset → Build Island Scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SunsetIsland : MonoBehaviour
    {
        [Tooltip("Во сколько раз увеличить фон-остров (размер игровой зоны).")]
        public float islandScale = 3f;

        [Tooltip("Сколько предметов разбросать по острову.")]
        public int pickupCount = 6;

        private NekoPresence _neko;
        private Transform _player;
        private Transform _enemy;
        private int _collected;
        private int _total;
        private TextMeshProUGUI _hud;
        private Vector2 _boundsMin, _boundsMax;
        private readonly System.Random _rng = new System.Random();
        private bool _wounded;

        private void Awake()
        {
            BuildCamera(out Camera cam);
            BuildGround();
            BuildBounds(cam);
            _player = BuildPlayer();
            BuildPickups();
            _enemy = BuildEnemy(_player);
            _neko = gameObject.AddComponent<NekoPresence>();
            BuildHud();

            // камера следует за игроком в рамках острова
            var follow = cam.gameObject.AddComponent<CameraFollow2D>();
            follow.target = _player;
            follow.useBounds = true;
            follow.boundsMin = _boundsMin;
            follow.boundsMax = _boundsMax;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SceneFlow.Go(SceneFlow.MainMenu);

            if (_player != null && _enemy != null)
            {
                float d = Vector2.Distance(_player.position, _enemy.position);
                if (d < 5.5f) _neko.Trigger("boss");      // близко большое — присутствие само не частит
                if (d < 1.1f && !_wounded) { _wounded = true; _neko.Trigger("wounded"); }
                if (d > 2.5f) _wounded = false;
            }
        }

        // ---------- сборка мира ----------

        private void BuildCamera(out Camera cam)
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            cam.transform.position = new Vector3(0, 0, -10f);
        }

        private void BuildGround()
        {
            var go = new GameObject("Ground");
            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("Sunset/loc_start");
            if (sprite != null) { sr.sprite = sprite; sr.color = Color.white; }
            else { sr.sprite = MakeSprite(new Color(0.3f, 0.5f, 0.3f), 32, false); }
            sr.sortingOrder = 0;
            go.transform.localScale = new Vector3(islandScale, islandScale, 1f);
            go.transform.position = Vector3.zero;
        }

        private void BuildBounds(Camera cam)
        {
            // размер фона в мире (по спрайту loc_start ~9.6×6 при ppu 100) × масштаб
            float halfW = 9.6f * 0.5f * islandScale - 0.5f;
            float halfH = 6f * 0.5f * islandScale - 0.5f;
            _boundsMin = new Vector2(-halfW, -halfH);
            _boundsMax = new Vector2(halfW, halfH);

            AddWall(new Vector2(0, halfH + 0.5f), new Vector2(halfW * 2 + 2, 1));   // верх
            AddWall(new Vector2(0, -halfH - 0.5f), new Vector2(halfW * 2 + 2, 1));  // низ
            AddWall(new Vector2(-halfW - 0.5f, 0), new Vector2(1, halfH * 2 + 2));  // лево
            AddWall(new Vector2(halfW + 0.5f, 0), new Vector2(1, halfH * 2 + 2));   // право
        }

        private void AddWall(Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private Transform BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.position = Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSprite(new Color(0.45f, 0.75f, 1f), 28, true);
            sr.sortingOrder = 10;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;

            var pc = go.AddComponent<PlayerController2D>();
            pc.speed = 5.5f;
            return go.transform;
        }

        private void BuildPickups()
        {
            _total = Mathf.Max(0, pickupCount);
            for (int i = 0; i < _total; i++)
            {
                var go = new GameObject("Pickup");
                go.transform.position = RandomInBounds(1.5f);
                go.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = MakeSprite(new Color(1f, 0.82f, 0.3f), 24, false);
                sr.sortingOrder = 5;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true; col.radius = 0.7f;
                var pickup = go.AddComponent<Pickup>();
                pickup.Collected = OnCollected;
            }
        }

        private Transform BuildEnemy(Transform target)
        {
            var go = new GameObject("Enemy");
            go.transform.position = RandomInBounds(4f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeSprite(new Color(0.85f, 0.3f, 0.3f), 30, false);
            sr.sortingOrder = 10;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f; rb.freezeRotation = true;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.85f, 0.85f);
            var e = go.AddComponent<EnemyStub>();
            e.target = target; e.speed = 2.0f; e.aggroRange = 7f;
            return go.transform;
        }

        private void OnCollected(Pickup p)
        {
            _collected++;
            UpdateHud();
            _neko.Trigger("discovery");
        }

        // ---------- HUD ----------

        private void BuildHud()
        {
            var canvasGo = new GameObject("IslandHUD");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            var root = canvasGo.GetComponent<RectTransform>();

            var countGo = new GameObject("Count", typeof(RectTransform));
            countGo.transform.SetParent(root, false);
            var crt = countGo.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(0, 1);
            crt.pivot = new Vector2(0, 1); crt.anchoredPosition = new Vector2(30, -24);
            crt.sizeDelta = new Vector2(600, 50);
            _hud = countGo.AddComponent<TextMeshProUGUI>();
            _hud.fontSize = 30; _hud.color = new Color(0.95f, 0.91f, 0.81f);
            _hud.alignment = TextAlignmentOptions.TopLeft;
            if (TMP_Settings.defaultFontAsset != null) _hud.font = TMP_Settings.defaultFontAsset;
            UpdateHud();

            var hintGo = new GameObject("Hint", typeof(RectTransform));
            hintGo.transform.SetParent(root, false);
            var hrt = hintGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0); hrt.anchorMax = new Vector2(0.5f, 0);
            hrt.pivot = new Vector2(0.5f, 0); hrt.anchoredPosition = new Vector2(0, 24);
            hrt.sizeDelta = new Vector2(1200, 30);
            var hint = hintGo.AddComponent<TextMeshProUGUI>();
            hint.text = "WASD / стрелки — движение · собирай золотое · красный преследует · Esc — в меню";
            hint.fontSize = 18; hint.color = new Color(0.95f, 0.91f, 0.81f, 0.45f);
            hint.alignment = TextAlignmentOptions.Center;
            if (TMP_Settings.defaultFontAsset != null) hint.font = TMP_Settings.defaultFontAsset;
        }

        private void UpdateHud()
        {
            if (_hud != null) _hud.text = $"Собрано: {_collected} / {_total}";
        }

        // ---------- утилиты ----------

        private Vector3 RandomInBounds(float margin)
        {
            float x = Mathf.Lerp(_boundsMin.x + margin, _boundsMax.x - margin, (float)_rng.NextDouble());
            float y = Mathf.Lerp(_boundsMin.y + margin, _boundsMax.y - margin, (float)_rng.NextDouble());
            return new Vector3(x, y, 0f);
        }

        /// <summary>Сплошной спрайт-заглушка (квадрат или круг), 1×1 мир-единица.</summary>
        private static Sprite MakeSprite(Color color, int size, bool circle)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            Color32 c = color, clear = new Color32(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inside = !circle || (new Vector2(x + 0.5f - r, y + 0.5f - r).sqrMagnitude <= (r - 0.5f) * (r - 0.5f));
                    px[y * size + x] = inside ? c : clear;
                }
            tex.SetPixels32(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}

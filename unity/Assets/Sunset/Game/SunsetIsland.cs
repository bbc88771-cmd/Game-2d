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
        [Tooltip("Во сколько раз увеличить остров (размер игровой зоны). Карта большая.")]
        public float islandScale = 3.5f;

        [Tooltip("Сколько предметов разбросать по острову.")]
        public int pickupCount = 8;

        private NekoPresence _neko;
        private Transform _player;
        private Transform _enemy;
        private int _collected;
        private int _total;
        private TextMeshProUGUI _hud;
        private Vector2 _boundsMin, _boundsMax;
        private float _spriteW, _spriteH;   // размер острова в мире при масштабе 1
        private readonly System.Random _rng = new System.Random();
        private bool _wounded;

        private void Awake()
        {
            BuildCamera(out Camera cam);
            BuildGround();
            BuildBoundary();
            BuildHouses();
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
            cam.orthographicSize = 8f; // карта большая — показываем больше
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.09f, 1f);
            cam.transform.position = new Vector3(0, 0, -10f);
        }

        private void BuildGround()
        {
            var go = new GameObject("Ground");
            var sr = go.AddComponent<SpriteRenderer>();
            var sprite = Resources.Load<Sprite>("Sunset/island_start");
            if (sprite != null)
            {
                sr.sprite = sprite; sr.color = Color.white;
                _spriteW = sprite.bounds.size.x;
                _spriteH = sprite.bounds.size.y;
            }
            else
            {
                sr.sprite = MakeSprite(new Color(0.3f, 0.5f, 0.3f), 32, false);
                _spriteW = _spriteH = 1f;
            }
            sr.sortingOrder = 0;
            go.transform.localScale = new Vector3(islandScale, islandScale, 1f);
            go.transform.position = Vector3.zero;

            // рамка для камеры — габариты острова в мире
            float halfW = _spriteW * 0.5f * islandScale;
            float halfH = _spriteH * 0.5f * islandScale;
            _boundsMin = new Vector2(-halfW, -halfH);
            _boundsMax = new Vector2(halfW, halfH);
        }

        /// <summary>Граница по силуэту острова: дальше — пустота, туда не пройти.</summary>
        private void BuildBoundary()
        {
            int n = IslandStartShape.Count;
            if (n < 3) return;
            var go = new GameObject("IslandBoundary");
            var pts = new Vector2[n + 1];
            for (int i = 0; i < n; i++)
            {
                float u = IslandStartShape.Points[i * 2];
                float v = IslandStartShape.Points[i * 2 + 1];
                pts[i] = NormToWorld(u, v);
            }
            pts[n] = pts[0]; // замкнуть петлю
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = pts;
        }

        // нормализованные (u,v) острова → мировые координаты (с учётом масштаба)
        private Vector2 NormToWorld(float u, float v)
        {
            float x = (u - 0.5f) * _spriteW * islandScale;
            float y = (0.5f - v) * _spriteH * islandScale; // v идёт вниз, мир — вверх
            return new Vector2(x, y);
        }

        // дома-ассеты: имя + позиция в нормализованных координатах острова (на суше)
        private static readonly (string name, float u, float v)[] HouseLayout =
        {
            ("house_tower",  0.42f, 0.22f),
            ("house_market", 0.55f, 0.33f),
            ("house_barn",   0.30f, 0.30f),
            ("house_smithy", 0.30f, 0.66f),
            ("house_porch",  0.52f, 0.74f),
            ("house_wood",   0.66f, 0.62f),
        };

        private void BuildHouses()
        {
            foreach (var h in HouseLayout)
            {
                if (!IslandStartShape.Contains(h.u, h.v)) continue; // только на суше
                var sprite = Resources.Load<Sprite>("Sunset/" + h.name);
                if (sprite == null) continue;

                var go = new GameObject(h.name);
                go.transform.position = (Vector3)NormToWorld(h.u, h.v);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 6; // под игроком (10): игрок проходит перед домом

                // коллайдер-«фундамент» у основания, чтобы нельзя было пройти сквозь дом
                float w = sprite.bounds.size.x, hgt = sprite.bounds.size.y;
                var col = go.AddComponent<BoxCollider2D>();
                col.size = new Vector2(w * 0.7f, hgt * 0.28f);
                col.offset = new Vector2(0f, -hgt * 0.5f + hgt * 0.16f);
            }
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
                go.transform.position = RandomOnLand();
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
            go.transform.position = RandomOnLand();
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

        // случайная точка НА СУШЕ острова (через силуэт IslandStartShape)
        private Vector3 RandomOnLand()
        {
            for (int i = 0; i < 64; i++)
            {
                float u = 0.1f + 0.8f * (float)_rng.NextDouble();
                float v = 0.1f + 0.8f * (float)_rng.NextDouble();
                if (IslandStartShape.Contains(u, v))
                    return (Vector3)NormToWorld(u, v);
            }
            return Vector3.zero; // запас: центр острова
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

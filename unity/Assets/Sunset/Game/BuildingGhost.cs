using System;
using UnityEngine;

namespace Sunset.Game
{
    /// <summary>
    /// Полупрозрачный «призрак» постройки при выборе места: следует за курсором,
    /// подсвечивается зелёным (можно ставить) или красным (нельзя — вне суши/занято).
    /// По ТЗ: «поставить в удобное место с полупрозрачной видимостью».
    /// </summary>
    [DisallowMultipleComponent]
    public class BuildingGhost : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Camera _cam;
        private Func<Vector2, bool> _isValid;

        public Vector2 WorldPos { get; private set; }
        public bool ValidHere { get; private set; }

        public void Init(Sprite sprite, Func<Vector2, bool> isValid)
        {
            _cam = Camera.main;
            _isValid = isValid;
            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = sprite;
            _sr.sortingOrder = 20; // поверх мира
        }

        private void Update()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 m = Input.mousePosition;
            m.z = -_cam.transform.position.z;
            Vector3 w = _cam.ScreenToWorldPoint(m);
            WorldPos = new Vector2(w.x, w.y);
            transform.position = new Vector3(WorldPos.x, WorldPos.y, 0f);

            ValidHere = _isValid == null || _isValid(WorldPos);
            // полупрозрачно: зелёный — можно, красный — нельзя
            _sr.color = ValidHere ? new Color(0.5f, 1f, 0.5f, 0.55f) : new Color(1f, 0.45f, 0.45f, 0.55f);
        }
    }
}

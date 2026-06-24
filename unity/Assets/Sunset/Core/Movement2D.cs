using System;

namespace Sunset.Core
{
    /// <summary>
    /// Чистая математика движения «вид сверху»: из ввода (оси X/Y) и скорости даёт
    /// вектор скорости с нормализацией по диагонали (чтобы наискосок не было быстрее).
    /// Без зависимостей от движка — чтобы покрыть тестами. Сам контроллер
    /// (Rigidbody2D, ввод, поворот спрайта) — в Game/PlayerController2D.
    /// </summary>
    public static class Movement2D
    {
        /// <summary>
        /// Желаемая скорость по вводу. <paramref name="ix"/>/<paramref name="iy"/> — оси
        /// (обычно −1..1). Если длина ввода больше 1 (диагональ), он нормализуется,
        /// поэтому модуль скорости не превышает <paramref name="speed"/>.
        /// </summary>
        public static void Desired(float ix, float iy, float speed, out float vx, out float vy)
        {
            double len = Math.Sqrt((double)ix * ix + (double)iy * iy);
            if (len > 1.0) { ix = (float)(ix / len); iy = (float)(iy / len); }
            vx = ix * speed;
            vy = iy * speed;
        }

        /// <summary>Модуль вектора (для тестов/проверок).</summary>
        public static float Magnitude(float x, float y) => (float)Math.Sqrt((double)x * x + (double)y * y);
    }
}

using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты математики движения «вид сверху»: скорость и нормализация диагонали.</summary>
    public class Movement2DTests
    {
        [Test]
        public void SingleAxis_FullSpeed()
        {
            Movement2D.Desired(1, 0, 5f, out float vx, out float vy);
            Assert.AreEqual(5f, vx, 1e-4f);
            Assert.AreEqual(0f, vy, 1e-4f);
        }

        [Test]
        public void Diagonal_NotFasterThanSpeed()
        {
            Movement2D.Desired(1, 1, 5f, out float vx, out float vy);
            float mag = Movement2D.Magnitude(vx, vy);
            Assert.AreEqual(5f, mag, 1e-3f, "диагональ не должна быть быстрее");
            Assert.AreEqual(vx, vy, 1e-4f, "по диагонали оси равны");
        }

        [Test]
        public void Zero_Stops()
        {
            Movement2D.Desired(0, 0, 5f, out float vx, out float vy);
            Assert.AreEqual(0f, vx);
            Assert.AreEqual(0f, vy);
        }

        [Test]
        public void Analog_BelowOne_NotScaledUp()
        {
            // частичный ввод (геймпад) не должен ускоряться нормализацией
            Movement2D.Desired(0.5f, 0f, 10f, out float vx, out float vy);
            Assert.AreEqual(5f, vx, 1e-4f);
            Assert.AreEqual(0f, vy, 1e-4f);
        }

        [Test]
        public void Negative_DirectionPreserved()
        {
            Movement2D.Desired(-1, 0, 4f, out float vx, out float vy);
            Assert.AreEqual(-4f, vx, 1e-4f);
        }
    }
}

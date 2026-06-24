using System;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты силуэта стартового острова: контур и проверка «точка на земле».</summary>
    public class IslandStartShapeTests
    {
        [Test]
        public void HasContourPoints()
        {
            Assert.GreaterOrEqual(IslandStartShape.Count, 8, "контур должен быть осмысленным");
            Assert.AreEqual(IslandStartShape.Count * 2, IslandStartShape.Points.Length);
            foreach (var p in IslandStartShape.Points)
                Assert.IsTrue(p >= 0f && p <= 1f, "координаты нормализованы в [0,1]");
        }

        [Test]
        public void CenterIsLand_CornersAreVoid()
        {
            Assert.IsTrue(IslandStartShape.Contains(0.5f, 0.5f), "центр — суша");
            Assert.IsFalse(IslandStartShape.Contains(0.02f, 0.02f), "угол — пустота");
            Assert.IsFalse(IslandStartShape.Contains(0.98f, 0.02f), "угол — пустота");
            Assert.IsFalse(IslandStartShape.Contains(0.02f, 0.98f), "угол — пустота");
            Assert.IsFalse(IslandStartShape.Contains(0.98f, 0.98f), "угол — пустота");
        }

        [Test]
        public void LandFraction_IsReasonable()
        {
            var rng = new Random(1);
            int land = 0, total = 20000;
            for (int i = 0; i < total; i++)
                if (IslandStartShape.Contains((float)rng.NextDouble(), (float)rng.NextDouble())) land++;
            double frac = (double)land / total;
            Assert.IsTrue(frac > 0.3 && frac < 0.85, $"доля суши {frac:P0} вне ожидаемого");
        }
    }
}

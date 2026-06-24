using System;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>
    /// Тесты состояния лобби и кодов. Логика гейта проверяется в LobbyGateTests;
    /// здесь — модель лобби (игроки, карусель) и генерация/валидация кодов.
    /// </summary>
    public class LobbyStateTests
    {
        private static LobbyState Host(int unlocked = 1)
            => new LobbyState(LobbyMode.Host, "ABC123", new LobbyPlayer("Хост", true, unlocked));

        // ---------- коды ----------

        [Test]
        public void Code_GeneratesRequestedLengthFromAlphabet()
        {
            var rng = new Random(1);
            for (int n = 0; n < 50; n++)
            {
                string c = LobbyCode.Generate(rng);
                Assert.AreEqual(LobbyCode.DefaultLength, c.Length);
                foreach (char ch in c)
                    Assert.IsTrue(LobbyCode.Alphabet.IndexOf(ch) >= 0, $"символ '{ch}' вне алфавита");
            }
        }

        [Test]
        public void Code_Deterministic_WithSameSeed()
        {
            Assert.AreEqual(LobbyCode.Generate(new Random(42)), LobbyCode.Generate(new Random(42)));
        }

        [Test]
        public void Code_Validation()
        {
            Assert.IsTrue(LobbyCode.IsValid("abcd"));
            Assert.IsTrue(LobbyCode.IsValid("  abc123 "));
            Assert.IsFalse(LobbyCode.IsValid("ab"));
            Assert.IsFalse(LobbyCode.IsValid(null));
            Assert.AreEqual("ABC123", LobbyCode.Normalize("  abc123 "));
        }

        // ---------- игроки ----------

        [Test]
        public void HostIsFirstAndFlagged()
        {
            var l = Host(3);
            Assert.AreEqual(1, l.Count);
            Assert.IsTrue(l.Host.host);
            Assert.AreEqual(3, l.HostUnlocked);
        }

        [Test]
        public void AddGuest_RespectsMaxPlayers()
        {
            var l = Host();
            for (int i = 0; i < LobbyState.MaxPlayers - 1; i++)
                Assert.IsTrue(l.AddGuest(new LobbyPlayer("G" + i, false, 1)));
            Assert.IsTrue(l.IsFull);
            Assert.IsFalse(l.AddGuest(new LobbyPlayer("overflow", false, 1)));
            Assert.AreEqual(LobbyState.MaxPlayers, l.Count);
        }

        [Test]
        public void AddGuest_ForcesGuestFlag()
        {
            var l = Host();
            var g = new LobbyPlayer("Гость", true, 1); // ошибочно помечен хостом
            l.AddGuest(g);
            Assert.IsFalse(g.host);
            Assert.AreSame(l.Host, l.players[0]); // хост по-прежнему один
        }

        // ---------- карусель ----------

        [Test]
        public void Carousel_ClampsToBounds()
        {
            var l = Host();
            l.StepLocation(-5);
            Assert.AreEqual(0, l.locIndex);
            l.StepLocation(99);
            Assert.AreEqual(GameLocations.Count - 1, l.locIndex);
        }

        [Test]
        public void HostHasLocation_TracksUnlocked()
        {
            var l = Host(2); // открыты локации 0 и 1
            Assert.IsTrue(l.HostHasLocation(0));
            Assert.IsTrue(l.HostHasLocation(1));
            Assert.IsFalse(l.HostHasLocation(2));
        }

        // ---------- гейт старта ----------

        [Test]
        public void Gate_BlocksWhenHostHasNotUnlocked()
        {
            var l = Host(1);
            l.locIndex = 2;
            Assert.IsFalse(l.Gate().ok);
        }

        [Test]
        public void Gate_BlocksWhenGuestMissingLocation()
        {
            var l = Host(5);
            l.AddGuest(new LobbyPlayer("Отстающий", false, 1));
            l.locIndex = 3;
            var g = l.Gate();
            Assert.IsFalse(g.ok);
            StringAssert.Contains("Отстающий", g.message);
        }

        [Test]
        public void Gate_OkWhenEveryoneUnlocked()
        {
            var l = Host(5);
            l.AddGuest(new LobbyPlayer("Готовый", false, 5));
            l.locIndex = 4;
            Assert.IsTrue(l.Gate().ok);
        }
    }
}

using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты данных чата лобби и подстановки имён в шёпоты «Некого».</summary>
    public class LobbyChatTests
    {
        [Test]
        public void Pools_AreNonEmpty()
        {
            Assert.Greater(LobbyChat.NekoLobby.Length, 0);
            Assert.Greater(LobbyChat.NekoWhispers.Length, 0);
            Assert.Greater(LobbyChat.PlayerGreets.Length, 0);
            Assert.Greater(LobbyChat.PlayerIdles.Length, 0);
            foreach (var pair in LobbyChat.PlayerIdles)
                Assert.Greater(pair.Length, 0, "у болтовни нет вариантов");
        }

        [Test]
        public void Whispers_AllHavePlaceholders()
        {
            foreach (var w in LobbyChat.NekoWhispers)
            {
                StringAssert.Contains("{a}", w);
                StringAssert.Contains("{b}", w);
            }
        }

        [Test]
        public void Whisper_SubstitutesNames()
        {
            string r = LobbyChat.Whisper("{a}, берегись {b}.", "Тесси", "Вард");
            Assert.AreEqual("Тесси, берегись Вард.", r);
            Assert.IsFalse(r.Contains("{a}"));
            Assert.IsFalse(r.Contains("{b}"));
        }

        [Test]
        public void Whisper_HandlesNullsGracefully()
        {
            Assert.AreEqual("", LobbyChat.Whisper(null, "a", "b"));
            Assert.AreEqual(", ", LobbyChat.Whisper("{a}, {b}", null, null));
        }
    }
}

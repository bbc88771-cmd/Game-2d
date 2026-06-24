using System.Collections.Generic;
using NUnit.Framework;
using Sunset.Core;

namespace Sunset.Tests
{
    /// <summary>Тесты разблокировки локаций в лобби (порт lobbyGate).</summary>
    public class LobbyGateTests
    {
        private static List<LobbyPlayer> Party(int hostUnlocked, params int[] friends)
        {
            var list = new List<LobbyPlayer> { new LobbyPlayer("Вы", true, hostUnlocked) };
            for (int i = 0; i < friends.Length; i++)
                list.Add(new LobbyPlayer("Друг" + (i + 1), false, friends[i]));
            return list;
        }

        [Test]
        public void HostHasNotUnlocked_Blocks()
        {
            var res = LobbyGate.Evaluate(Party(2), 2); // выбран индекс 2, у хоста открыто 2 (0..1)
            Assert.IsFalse(res.ok);
            StringAssert.Contains("ещё не открыл", res.message);
        }

        [Test]
        public void FriendMissingLocation_BlocksAndNamesThem()
        {
            // хост открыл 3 (индексы 0..2), друг — только 2 (0..1), выбран индекс 2
            var res = LobbyGate.Evaluate(Party(3, 2, 4), 2);
            Assert.IsFalse(res.ok);
            StringAssert.Contains("Друг1", res.message);
            StringAssert.Contains("не разблокирована", res.message);
        }

        [Test]
        public void AllUnlocked_Ok()
        {
            var res = LobbyGate.Evaluate(Party(3, 3, 5), 2);
            Assert.IsTrue(res.ok);
            StringAssert.Contains("Можно начинать", res.message);
        }

        [Test]
        public void FirstLocation_AlwaysOkForEveryone()
        {
            var res = LobbyGate.Evaluate(Party(1, 1, 1, 1), 0);
            Assert.IsTrue(res.ok);
        }

        [Test]
        public void IsUnlocked_CountMeansIndices()
        {
            var p = new LobbyPlayer("X", false, 3); // открыл 3 локации → индексы 0,1,2
            Assert.IsTrue(LobbyGate.IsUnlocked(p, 0));
            Assert.IsTrue(LobbyGate.IsUnlocked(p, 2));
            Assert.IsFalse(LobbyGate.IsUnlocked(p, 3));
        }
    }
}

using System;

namespace Sunset.Core
{
    /// <summary>Описание уровня сложности (порт DIFFICULTIES из веб-версии).</summary>
    [Serializable]
    public class DifficultyDef
    {
        public string id;
        public string name;
        public string sub;

        public DifficultyDef(string id, string name, string sub)
        {
            this.id = id; this.name = name; this.sub = sub;
        }
    }

    public static class GameDifficulties
    {
        public static readonly DifficultyDef[] All =
        {
            new DifficultyDef("creative", "Творческий", "Без угроз — стройка и исследование"),
            new DifficultyDef("easy", "Лёгкий", "Мягкие враги, без доп-боссов (~30 ч)"),
            new DifficultyDef("normal", "Обычный", "Базовый баланс игры"),
            new DifficultyDef("hard", "Сложный", "Доп. мини-боссы, квесты, предметы"),
            new DifficultyDef("nightmare", "Хард", "Максимум: редкая 5-я способность, секреты"),
        };

        public static int Count => All.Length;

        /// <summary>Человекочитаемое имя сложности по id (или «—», если неизвестна).</summary>
        public static string NameOf(string id)
        {
            foreach (var d in All) if (d.id == id) return d.name;
            return "—";
        }

        public static bool IsValid(string id)
        {
            foreach (var d in All) if (d.id == id) return true;
            return false;
        }
    }
}

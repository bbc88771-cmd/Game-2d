using System;

namespace Sunset.Core
{
    /// <summary>Описание мода в лобби (порт modsState из веб-версии).</summary>
    [Serializable]
    public class ModDef
    {
        public string name;
        public string sub;
        public bool on;
        public bool friend; // мод включён у друга в общем лобби
        public bool custom; // добавлен игроком вручную

        public ModDef(string name, string sub, bool on, bool friend = false, bool custom = false)
        {
            this.name = name; this.sub = sub; this.on = on; this.friend = friend; this.custom = custom;
        }
    }

    public static class GameMods
    {
        /// <summary>Стартовый набор модов лобби (новая копия на каждое лобби).</summary>
        public static System.Collections.Generic.List<ModDef> Defaults()
        {
            return new System.Collections.Generic.List<ModDef>
            {
                new ModDef("Расширенный бестиарий", "Доп. мобы и дроп-таблицы", true),
                new ModDef("Больше построек", "+12 зданий и декор", false),
                new ModDef("Хардкор-выживание", "Голод, жажда, температура", false, friend: true),
                new ModDef("Уютные ночи", "Светлячки и костры у лагеря", false, friend: true),
            };
        }
    }
}

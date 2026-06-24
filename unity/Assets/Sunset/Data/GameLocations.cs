using System;

namespace Sunset.Core
{
    /// <summary>Описание локации-острова (порт LOCATIONS из веб-версии).</summary>
    [Serializable]
    public class LocationDef
    {
        public string id;
        public string name;
        public string who;
        public string desc;
        public string image; // имя спрайта в Resources (опционально для UI)

        public LocationDef(string id, string name, string who, string desc, string image)
        {
            this.id = id; this.name = name; this.who = who; this.desc = desc; this.image = image;
        }
    }

    public static class GameLocations
    {
        public static readonly LocationDef[] All =
        {
            new LocationDef("start", "Стартовый остров", "Жители: люди",
                "Большой остров: леса, реки, мало воды. 3–4 враждующие деревни. Боссы: 3 главных и мини-боссы.", "loc_start"),
            new LocationDef("snow", "Снежный", "Жители: люди и полу-люди",
                "Бури и снег заметают следы. Медузы, светлячки. Босс: охотники с волками.", "loc_snow"),
            new LocationDef("dead", "Мёртвый (пустыня)", "Жители: полумёртвые люди",
                "Засуха, черви, драугры. Боссы: драугры и черви.", "loc_dead"),
            new LocationDef("swamp", "Болотный", "Жители: рептилии (двуногие)",
                "Тина, черепахи, болотники. Босс: болотники.", "loc_swamp"),
            new LocationDef("magic", "Волшебный", "Жители: гномики",
                "Магия и источник: единороги, леприкон, пикси. Боссы: рогатая жаба, воробей в броне.", "loc_magic"),
        };

        public static int Count => All.Length;
    }
}

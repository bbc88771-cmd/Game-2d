namespace Sunset.Core
{
    /// <summary>Стоимость: ресурс и количество.</summary>
    public struct ResCost
    {
        public string res;
        public int amount;
        public ResCost(string res, int amount) { this.res = res; this.amount = amount; }
    }

    /// <summary>
    /// Описание постройки для крафта: id, имя, спрайт (Resources/Sunset/&lt;sprite&gt;)
    /// и стоимость в ресурсах. Спрайты переиспользуют дома-ассеты.
    /// </summary>
    public class BuildingDef
    {
        public readonly string id;
        public readonly string name;
        public readonly string sprite;
        public readonly ResCost[] cost;

        public BuildingDef(string id, string name, string sprite, ResCost[] cost)
        {
            this.id = id; this.name = name; this.sprite = sprite; this.cost = cost ?? new ResCost[0];
        }

        public int Cost(string res)
        {
            foreach (var c in cost) if (c.res == res) return c.amount;
            return 0;
        }

        public int TotalCost()
        {
            int s = 0;
            foreach (var c in cost) s += c.amount;
            return s;
        }
    }

    /// <summary>
    /// Доступные постройки (меню крафтов). Дубовый дом по ТЗ:
    /// 500 древесины, 250 камня, 150 веток, 100 глины.
    /// </summary>
    public static class BuildRecipes
    {
        public static readonly BuildingDef[] All =
        {
            new BuildingDef("oak_house", "Дубовый дом", "house_wood", new[]
            {
                new ResCost(ResourceId.Wood, 500),
                new ResCost(ResourceId.Stone, 250),
                new ResCost(ResourceId.Branch, 150),
                new ResCost(ResourceId.Clay, 100),
            }),
            new BuildingDef("hut", "Изба с верандой", "house_porch", new[]
            {
                new ResCost(ResourceId.Wood, 300),
                new ResCost(ResourceId.Branch, 120),
                new ResCost(ResourceId.Clay, 60),
            }),
            new BuildingDef("market", "Торговая лавка", "house_market", new[]
            {
                new ResCost(ResourceId.Wood, 350),
                new ResCost(ResourceId.Stone, 150),
                new ResCost(ResourceId.Clay, 80),
            }),
            new BuildingDef("smithy", "Кузница", "house_smithy", new[]
            {
                new ResCost(ResourceId.Stone, 400),
                new ResCost(ResourceId.Wood, 200),
                new ResCost(ResourceId.Clay, 120),
            }),
            new BuildingDef("barn", "Амбар", "house_barn", new[]
            {
                new ResCost(ResourceId.Wood, 450),
                new ResCost(ResourceId.Branch, 200),
            }),
        };

        public static BuildingDef ById(string id)
        {
            foreach (var b in All) if (b.id == id) return b;
            return null;
        }
    }
}

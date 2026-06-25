namespace Sunset.Core
{
    /// <summary>Идентификаторы ресурсов строительства и их человекочитаемые имена.</summary>
    public static class ResourceId
    {
        public const string Wood = "wood";
        public const string Stone = "stone";
        public const string Branch = "branch";
        public const string Clay = "clay";

        public static readonly string[] All = { Wood, Stone, Branch, Clay };

        public static string Name(string id)
        {
            switch (id)
            {
                case Wood: return "Древесина";
                case Stone: return "Камень";
                case Branch: return "Ветки";
                case Clay: return "Глина";
                default: return id;
            }
        }
    }
}

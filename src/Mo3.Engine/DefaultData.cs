namespace Mo3.Engine;

public static class DefaultData
{
    public static IReadOnlyList<Faction> Factions { get; } =
    [
        new() { Id = "aelthuun", Name = "Ael’thuun" },
        new() { Id = "aurumbrae", Name = "Aurumbræ" },
        new() { Id = "dracogallus", Name = "Dracogallus" },
        new() { Id = "elyndar", Name = "Elyndar" },
        new() { Id = "kame-no-kaizoku", Name = "Kame no Kaizoku" },
        new() { Id = "malvethar", Name = "Malvethar" },
        new() { Id = "mar-rhazun", Name = "Mar’Rhazûn" },
        new() { Id = "morr-ghuun", Name = "Morr’ghuun" },
        new() { Id = "ooruun", Name = "Ooruun" },
        new() { Id = "ordo-solis", Name = "Ordo Solis" },
        new() { Id = "qal-asar", Name = "Qal Asar" },
        new() { Id = "ruda-flotila", Name = "Rudá flotila" },
        new() { Id = "sos", Name = "S.O.S." },
        new() { Id = "shoal", Name = "Shoal" },
        new() { Id = "taznar", Name = "Taznar" }
    ];

    public static IReadOnlyDictionary<string, Dictionary<ResourceType, int>> StartingResources { get; } =
        new Dictionary<string, Dictionary<ResourceType, int>>
        {
            ["aelthuun"] = CreateResources(0, 0, 0, 1, 1, 3, 2, 0, -1, 0, 0, -1),
            ["aurumbrae"] = CreateResources(0, 1, 0, 1, 0, 0, 2, 0, 0, 0, 6, 0),
            ["dracogallus"] = CreateResources(0, 0, 0, 0, 0, 3, 2, 1, 0, 0, 4, 0),
            ["elyndar"] = CreateResources(0, 0, 0, 1, 0, 3, 3, 0, 0, 0, 3, 0),
            ["kame-no-kaizoku"] = CreateResources(0, 1, 0, 0, 0, 3, 3, 0, 0, 3, 0, 0),
            ["malvethar"] = CreateResources(1, 1, 0, 0, 0, 0, 2, 0, 0, 3, 3, 0),
            ["mar-rhazun"] = CreateResources(0, 1, 0, 0, 0, 3, 0, 0, 0, 0, 6, 0),
            ["morr-ghuun"] = CreateResources(0, 0, 1, 0, 0, 3, 0, 0, 0, 0, 6, 0),
            ["ooruun"] = CreateResources(0, 1, 1, 1, 0, 3, 0, 0, 0, 0, 4, 0),
            ["ordo-solis"] = CreateResources(0, 1, 1, 0, 0, 3, 0, 0, 0, 0, 5, 0),
            ["qal-asar"] = CreateResources(0, 1, 1, 0, 0, 3, 0, 0, 0, 2, 2, 1),
            ["ruda-flotila"] = CreateResources(1, 2, 0, 0, 0, 0, 2, 0, 0, 5, 0, 0),
            ["sos"] = CreateResources(0, 0, 0, 0, 0, 3, 3, 0, 0, 4, 0, 0),
            ["shoal"] = CreateResources(0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 7, 0),
            ["taznar"] = CreateResources(0, 0, 0, 1, 0, 3, 3, 0, 0, 0, 3, 0)
        };

    private static Dictionary<ResourceType, int> CreateResources(
        int food,
        int wood,
        int iron,
        int gold,
        int magicalLiquid,
        int workforce,
        int tools,
        int luxuryGoods,
        int magicItems,
        int ships,
        int conscripts,
        int scholars)
    {
        return new Dictionary<ResourceType, int>
        {
            [ResourceType.Food] = food,
            [ResourceType.Wood] = wood,
            [ResourceType.Iron] = iron,
            [ResourceType.Gold] = gold,
            [ResourceType.MagicalLiquid] = magicalLiquid,
            [ResourceType.Workforce] = workforce,
            [ResourceType.Tools] = tools,
            [ResourceType.LuxuryGoods] = luxuryGoods,
            [ResourceType.MagicItems] = magicItems,
            [ResourceType.Ships] = ships,
            [ResourceType.Conscripts] = conscripts,
            [ResourceType.Scholars] = scholars
        };
    }
}

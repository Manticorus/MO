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

    public static IReadOnlyDictionary<string, IReadOnlyList<ResourceType>> StrongResourcesByFaction { get; } =
        new Dictionary<string, IReadOnlyList<ResourceType>>
        {
            ["shoal"] = [ResourceType.Workforce, ResourceType.MagicalLiquid, ResourceType.Food],
            ["dracogallus"] = [ResourceType.MagicalLiquid, ResourceType.MagicItems, ResourceType.Scholars],
            ["mar-rhazun"] = [ResourceType.Iron, ResourceType.Tools, ResourceType.Gold],
            ["ooruun"] = [ResourceType.Food, ResourceType.Conscripts, ResourceType.Tools],
            ["aelthuun"] = [ResourceType.MagicItems, ResourceType.Scholars, ResourceType.LuxuryGoods],
            ["elyndar"] = [ResourceType.Food, ResourceType.Conscripts, ResourceType.Wood],
            ["taznar"] = [ResourceType.MagicalLiquid, ResourceType.Wood, ResourceType.Conscripts],
            ["malvethar"] = [ResourceType.Gold, ResourceType.LuxuryGoods, ResourceType.Workforce],
            ["aurumbrae"] = [ResourceType.Food, ResourceType.Workforce, ResourceType.LuxuryGoods],
            ["qal-asar"] = [ResourceType.MagicalLiquid, ResourceType.MagicItems, ResourceType.Tools],
            ["ordo-solis"] = [ResourceType.Conscripts, ResourceType.Food, ResourceType.Tools],
            ["morr-ghuun"] = [ResourceType.Food, ResourceType.Wood, ResourceType.Tools],
            ["kame-no-kaizoku"] = [ResourceType.Ships, ResourceType.Food, ResourceType.Iron],
            ["sos"] = [ResourceType.Ships, ResourceType.Wood, ResourceType.MagicalLiquid],
            ["ruda-flotila"] = [ResourceType.Ships, ResourceType.Workforce, ResourceType.Gold]
        };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> StartingEdictNamesByFaction { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["aelthuun"] = ["Hromotepec", "Aetherová Laboratoř", "Nebeská Knihovna"],
            ["aurumbrae"] = ["Chmelařství", "Měšťanské Domy", "Ležákový Pivovar"],
            ["dracogallus"] = ["Prachové Varny", "Dračí Výhně", "Psionická Lóže"],
            ["elyndar"] = ["Selské Hospodářství", "Panská Pila", "Rytířský Hrádek"],
            ["kame-no-kaizoku"] = ["Rýžové Terasy", "Kanna-ba", "Nipponské Loděnice"],
            ["malvethar"] = ["Pirátské Doupě", "Slumy Otroků", "Drogová Laboratoř"],
            ["mar-rhazun"] = ["Krvavá Štola", "Posedlý Důl", "Démonické Hutě"],
            ["morr-ghuun"] = ["Mušlový Háj", "Chaluhový Lis", "Perleťář"],
            ["ooruun"] = ["Jačí Stádo", "Kamenotepec", "Ordu Válečníků"],
            ["ordo-solis"] = ["Řádové Polnosti", "Kovárna Řádu", "Hrádek Rytířů Slunce"],
            ["qal-asar"] = ["Magická Destilerka", "Elfí Kovárna", "Quelský Alchymista"],
            ["ruda-flotila"] = ["Kasíno", "Lóže Nekromancerů", "Upíří Loděnice"],
            ["sos"] = ["Elfí Háj", "Elfí Zahrady", "Elfí Loděnice"],
            ["shoal"] = ["Řasové Plantáže", "Perlová Farma", "Korálové Vily"],
            ["taznar"] = ["Otrocká Pila", "Obětní Oltáře", "Chrám Krve"]
        };

    public static IReadOnlyDictionary<string, string> SpecialAbilityNotesByFaction { get; } =
        new Dictionary<string, string>
        {
            ["shoal"] = "No ships required for movement over sea; cannot permanently take inland cities.",
            ["dracogallus"] = "Spy edict once per order window.",
            ["mar-rhazun"] = "Strong-resource edicts produce 7 instead of 6.",
            ["ooruun"] = "No ships, cannot take ports, stronger but slower-recovering soldiers.",
            ["aelthuun"] = "No army/fleet usage; capital only mages can attack.",
            ["elyndar"] = "When they defeat an army, enemy losses are 100%.",
            ["taznar"] = "Each Magical Spring edict grants +1 conscript.",
            ["malvethar"] = "Each port city gives +5 workforce and +5 gold.",
            ["aurumbrae"] = "Every 5 exported resources grant +1 gold.",
            ["qal-asar"] = "Magic items production edict is stronger (8 total).",
            ["ordo-solis"] = "Each controlled city automatically produces +10 gold.",
            ["morr-ghuun"] = "No city takeover; can occupy ports and raise limits via special unification edicts.",
            ["kame-no-kaizoku"] = "Global trade access and trade-spy edict.",
            ["sos"] = "Global trade access and trade-spy edict.",
            ["ruda-flotila"] = "Global trade access and trade-spy edict."
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

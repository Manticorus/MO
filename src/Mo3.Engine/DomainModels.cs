namespace Mo3.Engine;

public enum ResourceType
{
    Food,
    Wood,
    Iron,
    Gold,
    MagicalLiquid,
    Workforce,
    Tools,
    LuxuryGoods,
    MagicItems,
    Ships,
    Conscripts,
    Scholars
}

public enum UnitType
{
    Army,
    Fleet,
    Mages
}

public sealed class City
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public List<ResourceType> ResourceFocuses { get; init; } = [];

    public int BaseGarrisonStrength { get; init; }
}

public sealed class Faction
{
    public required string Id { get; init; }

    public required string Name { get; init; }
}

public sealed class WorldDefinition
{
    public List<City> Cities { get; init; } = [];

    public List<Faction> Factions { get; init; } = [];
}

public sealed class FactionState
{
    public required string FactionId { get; init; }

    public string? OverlordFactionId { get; set; }

    public Dictionary<ResourceType, int> Resources { get; init; } = [];

    public Dictionary<UnitType, int> Units { get; init; } = [];

    public bool IsInEconomicCollapse { get; set; }
}

public sealed class CityState
{
    public required string CityId { get; init; }

    public required string OwnerFactionId { get; set; }

    public string? OccupyingFactionId { get; set; }

    public bool IsOccupied => OccupyingFactionId is not null;

    public int GarrisonStrength { get; set; }
}

public sealed class DefensivePact
{
    public required string FactionAId { get; init; }

    public required string FactionBId { get; init; }
}

public sealed class TradeTransfer
{
    public required string FromFactionId { get; init; }

    public required string ToFactionId { get; init; }

    public required ResourceType Resource { get; init; }

    public int Amount { get; init; }
}

public sealed class TradePact
{
    public required string FactionAId { get; init; }

    public required string FactionBId { get; init; }

    public List<TradeTransfer> Transfers { get; init; } = [];
}

public sealed class GameState
{
    public int SchemaVersion { get; init; } = 1;

    public int TurnNumber { get; set; }

    public int Seed { get; set; }

    public List<FactionState> Factions { get; init; } = [];

    public List<CityState> Cities { get; init; } = [];

    public List<DefensivePact> DefensivePacts { get; init; } = [];

    public List<TradePact> TradePacts { get; init; } = [];
}

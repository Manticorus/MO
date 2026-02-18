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

public enum EdictSection
{
    I,
    II,
    III,
    Military
}

public abstract record EdictBase
{
    public required string IssuingFactionId { get; init; }

    public required EdictSection Section { get; init; }

    public bool IsCancellation { get; init; }
}

public sealed record ResourceAmount
{
    public required ResourceType Resource { get; init; }

    public int Amount { get; init; }
}

public enum EdictResourceUsage
{
    Consumed,
    RequiredAvailable
}

public sealed record EdictResourceRequirement
{
    public required ResourceType Resource { get; init; }

    public int Amount { get; init; }

    public EdictResourceUsage Usage { get; init; }
}

public sealed record InternalProductionEdict : EdictBase
{
    public required string EdictName { get; init; }

    public List<EdictResourceRequirement> InputRequirements { get; init; } = [];

    public List<ResourceAmount> Outputs { get; init; } = [];

    public int ExecutionCount { get; init; } = 1;
}

public enum ExternalEdictType
{
    TradeContract,
    DefensivePact,
    Spy
}

public sealed record ExternalEdict : EdictBase
{
    public required ExternalEdictType Type { get; init; }

    public required string TargetFactionId { get; init; }

    public ResourceType? Resource { get; init; }

    public int Amount { get; init; }
}

public enum MilitaryEdictType
{
    Attack,
    SupportAttack,
    EndOccupation,
    Takeover,
    Liberation
}

public sealed record MilitaryEdict : EdictBase
{
    public required MilitaryEdictType Type { get; init; }

    public string? SourceCityId { get; init; }

    public string? TargetCityId { get; init; }

    public string? SupportedFactionId { get; init; }
}

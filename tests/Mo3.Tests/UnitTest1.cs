using Mo3.Engine;

namespace Mo3.Tests;

public class GameStateJsonTests
{
    [Fact]
    public void GameStateJson_Roundtrip_PreservesData()
    {
        var original = new GameState
        {
            SchemaVersion = 1,
            TurnNumber = 7,
            Seed = 12345,
            Factions =
            [
                new FactionState
                {
                    FactionId = "aelthuun",
                    OverlordFactionId = "ordo-solis",
                    Resources =
                    {
                        [ResourceType.Gold] = 12,
                        [ResourceType.Food] = 30
                    },
                    Units =
                    {
                        [UnitType.Army] = 5,
                        [UnitType.Fleet] = 2
                    },
                    IsInEconomicCollapse = false
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "aelthuun",
                    OccupyingFactionId = "malvethar",
                    GarrisonStrength = 9
                }
            ],
            DefensivePacts =
            [
                new DefensivePact
                {
                    FactionAId = "aelthuun",
                    FactionBId = "aurumbrae"
                }
            ],
            TradePacts =
            [
                new TradePact
                {
                    FactionAId = "aelthuun",
                    FactionBId = "elyndar",
                    Transfers =
                    [
                        new TradeTransfer
                        {
                            FromFactionId = "aelthuun",
                            ToFactionId = "elyndar",
                            Resource = ResourceType.Tools,
                            Amount = 2
                        }
                    ]
                }
            ]
        };

        var json = GameStateJson.Serialize(original);
        var restored = GameStateJson.Deserialize(json);

        Assert.Equal(1, restored.SchemaVersion);
        Assert.Equal(7, restored.TurnNumber);
        Assert.Equal(12345, restored.Seed);

        var faction = Assert.Single(restored.Factions);
        Assert.Equal("aelthuun", faction.FactionId);
        Assert.Equal("ordo-solis", faction.OverlordFactionId);
        Assert.Equal(12, faction.Resources[ResourceType.Gold]);
        Assert.Equal(2, faction.Units[UnitType.Fleet]);

        var city = Assert.Single(restored.Cities);
        Assert.True(city.IsOccupied);
        Assert.Equal("malvethar", city.OccupyingFactionId);

        var transfer = Assert.Single(Assert.Single(restored.TradePacts).Transfers);
        Assert.Equal(ResourceType.Tools, transfer.Resource);
        Assert.Equal(2, transfer.Amount);
    }
}

public class M2CalculationTests
{
    [Fact]
    public void EdictLimitCalculator_AppliesCityBonusesByFactionSize()
    {
        var cities =
            new[]
            {
                new City
                {
                    Id = "c1",
                    Name = "First",
                    ResourceFocuses = [ResourceType.Food, ResourceType.Wood, ResourceType.Iron]
                },
                new City
                {
                    Id = "c2",
                    Name = "Second",
                    ResourceFocuses = [ResourceType.Food, ResourceType.Gold, ResourceType.Tools]
                }
            };

        var limits = EdictLimitCalculator.CalculateLimits(cities);

        Assert.Equal(41, limits[ResourceType.Food]);
        Assert.Equal(22, limits[ResourceType.Wood]);
        Assert.Equal(22, limits[ResourceType.Iron]);
        Assert.Equal(21, limits[ResourceType.Gold]);
        Assert.Equal(21, limits[ResourceType.Tools]);
        Assert.Equal(2, limits[ResourceType.LuxuryGoods]);
    }

    [Fact]
    public void ProductionScaling_RespectsDoubleLimitAndHalfOutputAboveLimit()
    {
        var result = ProductionScaling.Calculate(baseOutputPerEdict: 4, requestedEdicts: 7, edictLimit: 3);

        Assert.Equal(7, result.RequestedEdicts);
        Assert.Equal(6, result.ExecutedEdicts);
        Assert.Equal(3, result.FullOutputEdicts);
        Assert.Equal(3, result.ReducedOutputEdicts);
        Assert.Equal(18m, result.TotalOutput);
    }
}


public class DefaultDataTests
{
    [Fact]
    public void DefaultData_ContainsProvidedFactionsAndStartingResources()
    {
        Assert.Equal(15, DefaultData.Factions.Count);
        Assert.Contains(DefaultData.Factions, f => f.Name == "Ael’thuun");
        Assert.Contains(DefaultData.Factions, f => f.Name == "Taznar");

        var aelthuun = DefaultData.StartingResources["aelthuun"];
        Assert.Equal(1, aelthuun[ResourceType.Gold]);
        Assert.Equal(-1, aelthuun[ResourceType.MagicItems]);

        var rudaFlotila = DefaultData.StartingResources["ruda-flotila"];
        Assert.Equal(5, rudaFlotila[ResourceType.Ships]);

        Assert.Equal(3, DefaultData.StrongResourcesByFaction["aelthuun"].Count);
        Assert.Equal(3, DefaultData.StartingEdictNamesByFaction["aelthuun"].Count);
        Assert.Contains(ResourceType.MagicItems, DefaultData.StrongResourcesByFaction["aelthuun"]);

        Assert.Equal(DefaultData.Factions.Count, DefaultData.SpecialAbilityNotesByFaction.Count);
        Assert.Contains("100%", DefaultData.SpecialAbilityNotesByFaction["elyndar"]);
        Assert.Contains("Global trade", DefaultData.SpecialAbilityNotesByFaction["sos"]);
    }
}

public class M3TypedEdictModelTests
{
    [Fact]
    public void TypedEdicts_CanBeCreated_ForInternalExternalAndMilitarySections()
    {
        EdictBase internalEdict = new InternalProductionEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.I,
            EdictName = "Ritual Forge",
            InputRequirements =
            [
                new EdictResourceRequirement
                {
                    Resource = ResourceType.Iron,
                    Amount = 1,
                    Usage = EdictResourceUsage.Consumed
                },
                new EdictResourceRequirement
                {
                    Resource = ResourceType.Tools,
                    Amount = 1,
                    Usage = EdictResourceUsage.RequiredAvailable
                }
            ],
            Outputs =
            [
                new ResourceAmount
                {
                    Resource = ResourceType.MagicItems,
                    Amount = 4
                }
            ],
            ExecutionCount = 3
        };

        EdictBase externalEdict = new ExternalEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.II,
            Type = ExternalEdictType.TradeContract,
            TargetFactionId = "elyndar",
            Resource = ResourceType.Tools,
            Amount = 2
        };

        EdictBase militaryEdict = new MilitaryEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.Military,
            Type = MilitaryEdictType.Attack,
            SourceCityId = "c1",
            TargetCityId = "c2"
        };

        var typedInternal = Assert.IsType<InternalProductionEdict>(internalEdict);
        Assert.Equal("Ritual Forge", typedInternal.EdictName);
        Assert.Equal(3, typedInternal.ExecutionCount);
        Assert.Collection(
            typedInternal.InputRequirements,
            consumed =>
            {
                Assert.Equal(ResourceType.Iron, consumed.Resource);
                Assert.Equal(EdictResourceUsage.Consumed, consumed.Usage);
            },
            requiredOnly =>
            {
                Assert.Equal(ResourceType.Tools, requiredOnly.Resource);
                Assert.Equal(EdictResourceUsage.RequiredAvailable, requiredOnly.Usage);
            });
        var output = Assert.Single(typedInternal.Outputs);
        Assert.Equal(ResourceType.MagicItems, output.Resource);
        Assert.Equal(4, output.Amount);

        var typedExternal = Assert.IsType<ExternalEdict>(externalEdict);
        Assert.Equal(ExternalEdictType.TradeContract, typedExternal.Type);
        Assert.Equal("elyndar", typedExternal.TargetFactionId);
        Assert.Equal(2, typedExternal.Amount);

        var typedMilitary = Assert.IsType<MilitaryEdict>(militaryEdict);
        Assert.Equal(MilitaryEdictType.Attack, typedMilitary.Type);
        Assert.Equal("c1", typedMilitary.SourceCityId);
        Assert.Equal("c2", typedMilitary.TargetCityId);
    }
}

public class M3EdictValidatorTests
{
    [Fact]
    public void Validate_Collapse_AllowsOnlyTradeOrCancellation()
    {
        var collapseFaction = new FactionState
        {
            FactionId = "aelthuun",
            IsInEconomicCollapse = true
        };

        var militaryEdict = new MilitaryEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.Military,
            Type = MilitaryEdictType.Attack,
            SourceCityId = "c1",
            TargetCityId = "c2"
        };

        var blocked = EdictValidator.Validate(militaryEdict, collapseFaction);
        Assert.Contains(blocked, e => e.Message.Contains("economic collapse", StringComparison.OrdinalIgnoreCase));

        var noSourceCityErrors = EdictValidator.Validate(
            militaryEdict with { SourceCityId = null },
            new FactionState { FactionId = "aelthuun", IsInEconomicCollapse = false });
        Assert.DoesNotContain(noSourceCityErrors, e => e.Message.Contains("source city", StringComparison.OrdinalIgnoreCase));

        var tradeEdict = new ExternalEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.II,
            Type = ExternalEdictType.TradeContract,
            TargetFactionId = "elyndar",
            Resource = ResourceType.Food,
            Amount = 1
        };

        Assert.Empty(EdictValidator.Validate(tradeEdict, collapseFaction));

        var cancellationEdict = new InternalProductionEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.I,
            IsCancellation = true,
            EdictName = "Cancel smithy",
            InputRequirements =
            [
                new EdictResourceRequirement
                {
                    Resource = ResourceType.Iron,
                    Amount = 1,
                    Usage = EdictResourceUsage.Consumed
                }
            ],
            Outputs =
            [
                new ResourceAmount
                {
                    Resource = ResourceType.Tools,
                    Amount = 1
                }
            ],
            ExecutionCount = 1
        };

        Assert.DoesNotContain(
            EdictValidator.Validate(cancellationEdict, collapseFaction),
            e => e.Message.Contains("economic collapse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ReportsParameterIssues()
    {
        var faction = new FactionState
        {
            FactionId = "aelthuun",
            IsInEconomicCollapse = false
        };

        var invalidInternal = new InternalProductionEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.I,
            EdictName = "",
            ExecutionCount = 0,
            InputRequirements =
            [
                new EdictResourceRequirement
                {
                    Resource = ResourceType.Wood,
                    Amount = 0,
                    Usage = EdictResourceUsage.Consumed
                }
            ],
            Outputs =
            [
                new ResourceAmount
                {
                    Resource = ResourceType.Gold,
                    Amount = 0
                }
            ]
        };

        var invalidExternal = new ExternalEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.II,
            Type = ExternalEdictType.TradeContract,
            TargetFactionId = "",
            Amount = 0
        };

        var internalErrors = EdictValidator.Validate(invalidInternal, faction);
        Assert.Contains(internalErrors, e => e.Message.Contains("name", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(internalErrors, e => e.Message.Contains("execution", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(internalErrors, e => e.Message.Contains("input amount", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(internalErrors, e => e.Message.Contains("output amount", StringComparison.OrdinalIgnoreCase));

        var externalErrors = EdictValidator.Validate(invalidExternal, faction);
        Assert.Contains(externalErrors, e => e.Message.Contains("target faction", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(externalErrors, e => e.Message.Contains("requires a resource", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(externalErrors, e => e.Message.Contains("amount must be positive", StringComparison.OrdinalIgnoreCase));
    }
}


public class EdictOutputCalculatorTests
{
    [Fact]
    public void GetInternalEdictOutputPerExecution_AppliesStrongResourceAndFactionBonuses()
    {
        Assert.Equal(4, EdictOutputCalculator.GetInternalEdictOutputPerExecution("aelthuun", ResourceType.Food));
        Assert.Equal(6, EdictOutputCalculator.GetInternalEdictOutputPerExecution("aelthuun", ResourceType.MagicItems));
        Assert.Equal(7, EdictOutputCalculator.GetInternalEdictOutputPerExecution("mar-rhazun", ResourceType.Iron));
        Assert.Equal(8, EdictOutputCalculator.GetInternalEdictOutputPerExecution("qal-asar", ResourceType.MagicItems));
    }
}

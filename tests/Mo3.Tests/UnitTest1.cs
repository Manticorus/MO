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
            TargetCityId = "c2",
            RequestedArmy = 1,
            RequestedFleet = 0,
            RequestedMages = 0
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
            TargetCityId = "c2",
            RequestedArmy = 1,
            RequestedFleet = 0,
            RequestedMages = 0
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


    [Fact]
    public void Validate_MilitaryRejectsNegativeRequestedUnits()
    {
        var faction = new FactionState { FactionId = "aelthuun" };

        var invalid = new MilitaryEdict
        {
            IssuingFactionId = "aelthuun",
            Section = EdictSection.Military,
            Type = MilitaryEdictType.Attack,
            TargetCityId = "c2",
            RequestedArmy = -1,
            RequestedFleet = 0,
            RequestedMages = 0
        };

        var errors = EdictValidator.Validate(invalid, faction);
        Assert.Contains(errors, e => e.Message.Contains("cannot be negative", StringComparison.OrdinalIgnoreCase));
    }
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

public class M4TurnResolverTests
{
    [Fact]
    public void ResolveTurn_UsesFixedSeedForDeterministicOrderAndLogs()
    {
        var state = new GameState
        {
            TurnNumber = 4,
            Factions =
            [
                new FactionState { FactionId = "aelthuun" },
                new FactionState { FactionId = "elyndar" }
            ]
        };

        var ordersByFaction = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["aelthuun"] =
            [
                new InternalProductionEdict
                {
                    IssuingFactionId = "aelthuun",
                    Section = EdictSection.I,
                    EdictName = "Foundry",
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
                }
            ],
            ["elyndar"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "elyndar",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    SourceCityId = "c2",
                    TargetCityId = "c1",
                    RequestedArmy = 10,
                    RequestedFleet = 2,
                    RequestedMages = 1
                }
            ]
        };

        var first = TurnResolver.ResolveTurn(state, ordersByFaction, seed: 1337);
        var second = TurnResolver.ResolveTurn(state, ordersByFaction, seed: 1337);

        Assert.Equal(first.ShuffledFactionOrder, second.ShuffledFactionOrder);
        Assert.Equal(first.LogEntries, second.LogEntries);
        Assert.Equal(1, first.MilitaryIntents.Count);
        Assert.Equal("Resolve turn 4 with seed 1337.", first.LogEntries[0]);
        Assert.Equal(1337, first.UpdatedState.Seed);

        var factionStartIndices = first.ShuffledFactionOrder.ToDictionary(
            factionId => factionId,
            factionId => first.LogEntries.IndexOf($"Faction {factionId}: begin resolution."));

        foreach (var factionId in first.ShuffledFactionOrder)
        {
            var startIndex = factionStartIndices[factionId];
            Assert.True(startIndex >= 0);

            Assert.True(first.LogEntries.IndexOf($"Faction {factionId}: section I snapshot start.") > startIndex);
            Assert.True(first.LogEntries.IndexOf($"Faction {factionId}: section II snapshot start.") > startIndex);
            Assert.True(first.LogEntries.IndexOf($"Faction {factionId}: section III snapshot start.") > startIndex);
        }

        var firstFaction = first.ShuffledFactionOrder[0];
        var secondFaction = first.ShuffledFactionOrder[1];
        var firstFactionSectionIII = first.LogEntries.IndexOf($"Faction {firstFaction}: section III snapshot start.");
        var secondFactionSectionI = first.LogEntries.IndexOf($"Faction {secondFaction}: section I snapshot start.");
        Assert.True(firstFactionSectionIII < secondFactionSectionI);
    }
}

public class M4TurnResolverStateApplicationTests
{
    [Fact]
    public void ResolveTurn_AppliesInternalExternalAndSpecialEdicts_AndPreparesBattleStats()
    {
        var state = new GameState
        {
            TurnNumber = 8,
            Factions =
            [
                new FactionState
                {
                    FactionId = "aurumbrae",
                    Resources =
                    {
                        [ResourceType.Workforce] = 4,
                        [ResourceType.Tools] = 0,
                        [ResourceType.Gold] = 0
                    },
                    Units =
                    {
                        [UnitType.Army] = 3,
                        [UnitType.Fleet] = 1,
                        [UnitType.Mages] = 0
                    }
                },
                new FactionState
                {
                    FactionId = "elyndar",
                    Resources =
                    {
                        [ResourceType.Workforce] = 1,
                        [ResourceType.Tools] = 0,
                        [ResourceType.Gold] = 0
                    },
                    Units =
                    {
                        [UnitType.Army] = 2,
                        [UnitType.Fleet] = 0,
                        [UnitType.Mages] = 1
                    }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "elyndar",
                    GarrisonStrength = 5
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["aurumbrae"] =
            [
                new InternalProductionEdict
                {
                    IssuingFactionId = "aurumbrae",
                    Section = EdictSection.I,
                    EdictName = "Workshop",
                    InputRequirements =
                    [
                        new EdictResourceRequirement
                        {
                            Resource = ResourceType.Workforce,
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
                },
                new ExternalEdict
                {
                    IssuingFactionId = "aurumbrae",
                    Section = EdictSection.II,
                    Type = ExternalEdictType.TradeContract,
                    TargetFactionId = "elyndar",
                    Resource = ResourceType.Workforce,
                    Amount = 5
                },
                new MilitaryEdict
                {
                    IssuingFactionId = "aurumbrae",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedArmy = 10,
                    RequestedFleet = 2,
                    RequestedMages = 1
                }
            ]
        };

        var result = TurnResolver.ResolveTurn(state, orders, seed: 99);

        var aurumbrae = Assert.Single(result.UpdatedState.Factions.Where(f => f.FactionId == "aurumbrae"));
        var elyndar = Assert.Single(result.UpdatedState.Factions.Where(f => f.FactionId == "elyndar"));

        Assert.Equal(-2, aurumbrae.Resources[ResourceType.Workforce]); // 4 - 1 (internal) - 5 (trade)
        Assert.Equal(4, aurumbrae.Resources[ResourceType.Tools]);
        Assert.Equal(1, aurumbrae.Resources[ResourceType.Gold]); // aurumbrae export bonus (5 / 5)
        Assert.Equal(6, elyndar.Resources[ResourceType.Workforce]);
        Assert.True(aurumbrae.IsInEconomicCollapse);

        var preparedBattle = Assert.Single(result.PreparedBattles);
        Assert.Equal("c1", preparedBattle.TargetCityId);
        Assert.Equal("elyndar", preparedBattle.DefenderFactionId);
        Assert.Equal(5, preparedBattle.DefenderGarrisonStrength);

        var attack = Assert.Single(preparedBattle.Attacks);
        Assert.Equal("aurumbrae", attack.AttackerFactionId);
        Assert.Equal(3, attack.AvailableArmy);
        Assert.Equal(1, attack.AvailableFleet);
        Assert.Equal(0, attack.AvailableMages);
        Assert.Equal(3, attack.CommittedArmy);
        Assert.Equal(1, attack.CommittedFleet);
        Assert.Equal(0, attack.CommittedMages);
    }
}


public class M4TurnResolverRuleFixTests
{
    [Fact]
    public void ResolveTurn_UsesDirectionalTradeAndAurumbræExportBonusOnlyWhenExporting()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState { FactionId = "aurumbrae", Resources = { [ResourceType.Wood] = 0, [ResourceType.Gold] = 0 } },
                new FactionState { FactionId = "ordo-solis", Resources = { [ResourceType.Wood] = 10, [ResourceType.Gold] = 0 } }
            ]
        };

        var inboundOrders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["aurumbrae"] =
            [
                new ExternalEdict
                {
                    IssuingFactionId = "aurumbrae",
                    Section = EdictSection.II,
                    Type = ExternalEdictType.TradeContract,
                    TargetFactionId = "ordo-solis",
                    Resource = ResourceType.Wood,
                    Amount = 5,
                    IsInboundToIssuer = true
                }
            ]
        };

        var result = TurnResolver.ResolveTurn(state, inboundOrders, 7);
        var aur = Assert.Single(result.UpdatedState.Factions.Where(x => x.FactionId == "aurumbrae"));
        var ord = Assert.Single(result.UpdatedState.Factions.Where(x => x.FactionId == "ordo-solis"));

        Assert.Equal(5, aur.Resources[ResourceType.Wood]);
        Assert.Equal(5, ord.Resources[ResourceType.Wood]);
        Assert.Equal(0, aur.Resources[ResourceType.Gold]);

        var outboundOrders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["aurumbrae"] =
            [
                new ExternalEdict
                {
                    IssuingFactionId = "aurumbrae",
                    Section = EdictSection.II,
                    Type = ExternalEdictType.TradeContract,
                    TargetFactionId = "ordo-solis",
                    Resource = ResourceType.Wood,
                    Amount = 5
                }
            ]
        };

        TurnResolver.ResolveTurn(state, outboundOrders, 8);
        Assert.Equal(1, aur.Resources[ResourceType.Gold]);
    }

    [Fact]
    public void ResolveTurn_AppliesCityOwnershipBonusesAsPersistentAdjustments()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState { FactionId = "ordo-solis", Resources = { [ResourceType.Gold] = 0 } },
                new FactionState { FactionId = "malvethar", Resources = { [ResourceType.Gold] = 0, [ResourceType.Workforce] = 0 } }
            ],
            Cities =
            [
                new CityState { CityId = "a", OwnerFactionId = "ordo-solis", GarrisonStrength = 1 },
                new CityState { CityId = "b", OwnerFactionId = "malvethar", GarrisonStrength = 1 }
            ]
        };

        TurnResolver.ResolveTurn(state, new Dictionary<string, IReadOnlyList<EdictBase>>(), 1);
        var ordo = state.Factions.Single(x => x.FactionId == "ordo-solis");
        var mal = state.Factions.Single(x => x.FactionId == "malvethar");
        Assert.Equal(10, ordo.Resources[ResourceType.Gold]);
        Assert.Equal(5, mal.Resources[ResourceType.Gold]);
        Assert.Equal(5, mal.Resources[ResourceType.Workforce]);

        state.Cities[0].OwnerFactionId = "malvethar";
        TurnResolver.ResolveTurn(state, new Dictionary<string, IReadOnlyList<EdictBase>>(), 2);
        Assert.Equal(0, ordo.Resources[ResourceType.Gold]);
        Assert.Equal(10, mal.Resources[ResourceType.Gold]);
        Assert.Equal(10, mal.Resources[ResourceType.Workforce]);
    }

    [Fact]
    public void ResolveTurn_TaznarBonusAppliesOnlyForMagickeZridlo()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "taznar",
                    Resources = { [ResourceType.MagicalLiquid] = 2, [ResourceType.Conscripts] = 0 }
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["taznar"] =
            [
                new InternalProductionEdict
                {
                    IssuingFactionId = "taznar",
                    Section = EdictSection.I,
                    EdictName = "Magické zřídlo",
                    InputRequirements = [ new EdictResourceRequirement { Resource = ResourceType.MagicalLiquid, Amount = 1, Usage = EdictResourceUsage.Consumed } ],
                    Outputs = [ new ResourceAmount { Resource = ResourceType.Wood, Amount = 1 } ]
                },
                new InternalProductionEdict
                {
                    IssuingFactionId = "taznar",
                    Section = EdictSection.I,
                    EdictName = "Some other spring",
                    InputRequirements = [ new EdictResourceRequirement { Resource = ResourceType.MagicalLiquid, Amount = 1, Usage = EdictResourceUsage.Consumed } ],
                    Outputs = [ new ResourceAmount { Resource = ResourceType.Wood, Amount = 1 } ]
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 11);
        var taznar = Assert.Single(state.Factions);
        Assert.Equal(1, taznar.Resources[ResourceType.Conscripts]);
    }
}

public class M5BattleResolutionTests
{
    [Fact]
    public void ResolveTurn_BattleTie_DefenderWinsAndAppliesCasualtyRounding()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "att",
                    Units = { [UnitType.Army] = 4 }
                },
                new FactionState
                {
                    FactionId = "def",
                    Units = { [UnitType.Army] = 4 }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "def",
                    GarrisonStrength = 0
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["att"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "att",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedArmy = 4
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 5);

        var city = Assert.Single(state.Cities);
        Assert.Equal("def", city.OwnerFactionId);
        Assert.Null(city.OccupyingFactionId);

        var attacker = state.Factions.Single(f => f.FactionId == "att");
        var defender = state.Factions.Single(f => f.FactionId == "def");
        Assert.Equal(2, attacker.Units[UnitType.Army]);
        Assert.Equal(3, defender.Units[UnitType.Army]);
    }

    [Fact]
    public void ResolveTurn_DefensivePactAllies_AreAutoCommittedToDefense()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "att",
                    Units = { [UnitType.Army] = 3 }
                },
                new FactionState
                {
                    FactionId = "def",
                    Units = { [UnitType.Army] = 0 }
                },
                new FactionState
                {
                    FactionId = "ally",
                    Units = { [UnitType.Army] = 4 }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "def",
                    GarrisonStrength = 0
                }
            ],
            DefensivePacts =
            [
                new DefensivePact
                {
                    FactionAId = "def",
                    FactionBId = "ally"
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["att"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "att",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedArmy = 3
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 17);

        var city = Assert.Single(state.Cities);
        Assert.Equal("def", city.OwnerFactionId);
        Assert.Null(city.OccupyingFactionId);

        var attacker = state.Factions.Single(f => f.FactionId == "att");
        var defender = state.Factions.Single(f => f.FactionId == "def");
        var ally = state.Factions.Single(f => f.FactionId == "ally");

        Assert.Equal(1, attacker.Units[UnitType.Army]);
        Assert.Equal(0, defender.Units[UnitType.Army]);
        Assert.Equal(3, ally.Units[UnitType.Army]);
    }

    [Fact]
    public void ResolveTurn_AttackerWin_OccupiesByDefaultAndRoundsWinnerCasualtiesUp()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "att",
                    Units = { [UnitType.Army] = 5 }
                },
                new FactionState
                {
                    FactionId = "def",
                    Units = { [UnitType.Army] = 1 }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "def",
                    GarrisonStrength = 0
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["att"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "att",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedArmy = 5
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 22);

        var city = Assert.Single(state.Cities);
        Assert.Equal("def", city.OwnerFactionId);
        Assert.Equal("att", city.OccupyingFactionId);

        var attacker = state.Factions.Single(f => f.FactionId == "att");
        var defender = state.Factions.Single(f => f.FactionId == "def");

        Assert.Equal(3, attacker.Units[UnitType.Army]);
        Assert.Equal(0, defender.Units[UnitType.Army]);
    }

    [Fact]
    public void ResolveTurn_MagesCountAsFivePowerInBattleResolution()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "att",
                    Units = { [UnitType.Mages] = 1 }
                },
                new FactionState
                {
                    FactionId = "def",
                    Units = { [UnitType.Army] = 4 }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "def",
                    GarrisonStrength = 0
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["att"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "att",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedMages = 1
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 30);

        var city = Assert.Single(state.Cities);
        Assert.Equal("def", city.OwnerFactionId);
        Assert.Null(city.OccupyingFactionId);

        var attacker = state.Factions.Single(f => f.FactionId == "att");
        var defender = state.Factions.Single(f => f.FactionId == "def");

        Assert.Equal(0, attacker.Units[UnitType.Mages]);
        Assert.Equal(3, defender.Units[UnitType.Army]);
    }

    [Fact]
    public void ResolveTurn_ElyndarWinner_InflictsFullLossesOnDefender()
    {
        var state = new GameState
        {
            Factions =
            [
                new FactionState
                {
                    FactionId = "elyndar",
                    Units = { [UnitType.Army] = 6 }
                },
                new FactionState
                {
                    FactionId = "def",
                    Units = { [UnitType.Army] = 4 }
                }
            ],
            Cities =
            [
                new CityState
                {
                    CityId = "c1",
                    OwnerFactionId = "def",
                    GarrisonStrength = 0
                }
            ]
        };

        var orders = new Dictionary<string, IReadOnlyList<EdictBase>>
        {
            ["elyndar"] =
            [
                new MilitaryEdict
                {
                    IssuingFactionId = "elyndar",
                    Section = EdictSection.Military,
                    Type = MilitaryEdictType.Attack,
                    TargetCityId = "c1",
                    RequestedArmy = 6
                }
            ]
        };

        TurnResolver.ResolveTurn(state, orders, 31);

        var city = Assert.Single(state.Cities);
        Assert.Equal("elyndar", city.OccupyingFactionId);

        var attacker = state.Factions.Single(f => f.FactionId == "elyndar");
        var defender = state.Factions.Single(f => f.FactionId == "def");

        Assert.Equal(4, attacker.Units[UnitType.Army]);
        Assert.Equal(0, defender.Units[UnitType.Army]);
    }
}

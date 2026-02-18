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
    }
}

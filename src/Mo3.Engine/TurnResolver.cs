namespace Mo3.Engine;

public sealed record PreparedBattle
{
    public required string TargetCityId { get; init; }

    public required string DefenderFactionId { get; init; }

    public int DefenderGarrisonStrength { get; init; }

    public List<PreparedBattleAttack> Attacks { get; init; } = [];
}

public sealed record PreparedBattleAttack
{
    public required string AttackerFactionId { get; init; }

    public required MilitaryEdictType OperationType { get; init; }

    public int AvailableArmy { get; init; }

    public int AvailableFleet { get; init; }

    public int AvailableMages { get; init; }

    public int CommittedArmy { get; init; }

    public int CommittedFleet { get; init; }

    public int CommittedMages { get; init; }
}

public sealed record TurnResolutionResult
{
    public required GameState UpdatedState { get; init; }

    public List<string> LogEntries { get; init; } = [];

    public List<MilitaryEdict> MilitaryIntents { get; init; } = [];

    public List<string> ShuffledFactionOrder { get; init; } = [];

    public List<PreparedBattle> PreparedBattles { get; init; } = [];
}

public static class TurnResolver
{
    private static readonly EdictSection[] OrderedEconomicSections = [EdictSection.I, EdictSection.II, EdictSection.III];

    public static TurnResolutionResult ResolveTurn(
        GameState state,
        IReadOnlyDictionary<string, IReadOnlyList<EdictBase>> ordersByFaction,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ordersByFaction);

        var shuffledFactionOrder = ShuffleFactions(ordersByFaction.Keys, seed);
        var logs = new List<string>
        {
            $"Resolve turn {state.TurnNumber} with seed {seed}.",
            $"Faction order: {string.Join(", ", shuffledFactionOrder)}"
        };

        RecalculatePersistentCityBonuses(state, logs);

        var factionById = state.Factions.ToDictionary(f => f.FactionId);

        foreach (var factionId in shuffledFactionOrder)
        {
            if (!ordersByFaction.TryGetValue(factionId, out var edicts) ||
                !factionById.TryGetValue(factionId, out var factionState))
            {
                continue;
            }

            logs.Add($"Faction {factionId}: begin resolution.");

            foreach (var section in OrderedEconomicSections)
            {
                ResolveSectionForFaction(factionState, state, edicts, section, logs);
            }
        }

        var militaryIntents = shuffledFactionOrder
            .Where(ordersByFaction.ContainsKey)
            .SelectMany(factionId => ordersByFaction[factionId].OfType<MilitaryEdict>())
            .ToList();

        CommitMilitaryTroops(state, militaryIntents, logs);

        var preparedBattles = PrepareBattles(state, militaryIntents, logs);
        logs.Add($"Military intents collected: {militaryIntents.Count}.");
        logs.Add($"Prepared battles: {preparedBattles.Count}.");

        state.Seed = seed;

        foreach (var factionState in state.Factions)
        {
            factionState.IsInEconomicCollapse = factionState.Resources.Values.Any(value => value < 0);
        }

        return new TurnResolutionResult
        {
            UpdatedState = state,
            LogEntries = logs,
            MilitaryIntents = militaryIntents,
            ShuffledFactionOrder = shuffledFactionOrder,
            PreparedBattles = preparedBattles
        };
    }

    private static void RecalculatePersistentCityBonuses(GameState state, List<string> logs)
    {
        foreach (var faction in state.Factions)
        {
            var controlledCities = state.Cities.Count(c => c.OwnerFactionId == faction.FactionId);

            if (faction.FactionId == "ordo-solis")
            {
                var newBonus = controlledCities * 10;
                var delta = newBonus - faction.AppliedCityGoldBonus;
                if (delta != 0)
                {
                    faction.Resources[ResourceType.Gold] = faction.Resources.GetValueOrDefault(ResourceType.Gold) + delta;
                    faction.AppliedCityGoldBonus = newBonus;
                    logs.Add($"Faction ordo-solis city bonus adjusted by {delta:+#;-#;0} Gold (controlled cities: {controlledCities}).");
                }
            }

            if (faction.FactionId == "malvethar")
            {
                var newBonus = controlledCities * 5;
                var goldDelta = newBonus - faction.AppliedCityGoldBonus;
                var workforceDelta = newBonus - faction.AppliedCityWorkforceBonus;

                if (goldDelta != 0)
                {
                    faction.Resources[ResourceType.Gold] = faction.Resources.GetValueOrDefault(ResourceType.Gold) + goldDelta;
                    faction.AppliedCityGoldBonus = newBonus;
                }

                if (workforceDelta != 0)
                {
                    faction.Resources[ResourceType.Workforce] = faction.Resources.GetValueOrDefault(ResourceType.Workforce) + workforceDelta;
                    faction.AppliedCityWorkforceBonus = newBonus;
                }

                if (goldDelta != 0 || workforceDelta != 0)
                {
                    logs.Add($"Faction malvethar city bonus adjusted by Gold {goldDelta:+#;-#;0}, Workforce {workforceDelta:+#;-#;0} (controlled cities: {controlledCities}).");
                }
            }
        }
    }

    private static void ResolveSectionForFaction(
        FactionState factionState,
        GameState state,
        IReadOnlyList<EdictBase> allFactionEdicts,
        EdictSection section,
        List<string> logs)
    {
        logs.Add($"Faction {factionState.FactionId}: section {section} snapshot start.");
        var sectionEdicts = allFactionEdicts.Where(edict => edict.Section == section).ToList();
        if (sectionEdicts.Count == 0)
        {
            return;
        }

        logs.Add($"Faction {factionState.FactionId}: section {section} has {sectionEdicts.Count} edict(s).");

        var sectionSnapshot = new Dictionary<ResourceType, int>(factionState.Resources);
        var pendingResourceChanges = new Dictionary<ResourceType, int>();

        var successfulInternalEdicts = new List<InternalProductionEdict>();
        var successfulExternalEdicts = new List<ExternalEdict>();

        foreach (var edict in sectionEdicts)
        {
            var errors = EdictValidator.Validate(edict, factionState);
            if (errors.Count > 0)
            {
                logs.Add($"Faction {factionState.FactionId}: skipped edict due to validation errors ({string.Join("; ", errors.Select(e => e.Message))}).");
                continue;
            }

            switch (edict)
            {
                case InternalProductionEdict internalEdict:
                    if (TryResolveInternalEdict(factionState, sectionSnapshot, internalEdict, pendingResourceChanges, logs))
                    {
                        successfulInternalEdicts.Add(internalEdict);
                    }
                    break;
                case ExternalEdict externalEdict:
                    successfulExternalEdicts.Add(externalEdict);
                    break;
            }
        }

        ApplyResourceChanges(factionState.Resources, pendingResourceChanges);
        ResolveExternalEdicts(factionState, state, successfulExternalEdicts, logs);
        ApplyFactionSpecialEdictBonuses(factionState, successfulInternalEdicts, logs);
    }

    private static bool TryResolveInternalEdict(
        FactionState factionState,
        IReadOnlyDictionary<ResourceType, int> sectionSnapshot,
        InternalProductionEdict edict,
        Dictionary<ResourceType, int> pendingResourceChanges,
        List<string> logs)
    {
        foreach (var requirement in edict.InputRequirements)
        {
            var availableAtSnapshot = sectionSnapshot.GetValueOrDefault(requirement.Resource);
            if (availableAtSnapshot < requirement.Amount)
            {
                logs.Add($"Faction {factionState.FactionId}: internal edict '{edict.EdictName}' skipped (snapshot has {availableAtSnapshot} {requirement.Resource}, needs {requirement.Amount}).");
                return false;
            }
        }

        foreach (var requirement in edict.InputRequirements.Where(input => input.Usage == EdictResourceUsage.Consumed))
        {
            pendingResourceChanges[requirement.Resource] = pendingResourceChanges.GetValueOrDefault(requirement.Resource) - requirement.Amount;
        }

        foreach (var output in edict.Outputs)
        {
            var baseOutputPerExecution = EdictOutputCalculator.GetInternalEdictOutputPerExecution(
                factionState.FactionId,
                output.Resource);

            var scaling = ProductionScaling.Calculate(
                baseOutputPerExecution,
                edict.ExecutionCount,
                edictLimit: 2);

            var producedAmount = (int)Math.Floor(scaling.TotalOutput);
            pendingResourceChanges[output.Resource] = pendingResourceChanges.GetValueOrDefault(output.Resource) + producedAmount;

            logs.Add($"Faction {factionState.FactionId}: internal edict '{edict.EdictName}' produced {producedAmount} {output.Resource}.");
        }

        return true;
    }

    private static void ResolveExternalEdicts(
        FactionState issuerFaction,
        GameState state,
        IReadOnlyList<ExternalEdict> externalEdicts,
        List<string> logs)
    {
        foreach (var edict in externalEdicts)
        {
            switch (edict.Type)
            {
                case ExternalEdictType.TradeContract:
                    ResolveTradeContract(issuerFaction, state, edict, logs);
                    break;
                case ExternalEdictType.DefensivePact:
                    ResolveDefensivePact(state, issuerFaction.FactionId, edict.TargetFactionId, logs);
                    break;
                case ExternalEdictType.Spy:
                    logs.Add($"Faction {issuerFaction.FactionId}: spy edict registered against {edict.TargetFactionId}.");
                    break;
            }
        }
    }

    private static void ResolveTradeContract(FactionState issuerFaction, GameState state, ExternalEdict edict, List<string> logs)
    {
        if (edict.Resource is null || edict.Amount <= 0)
        {
            return;
        }

        var targetFaction = state.Factions.FirstOrDefault(f => f.FactionId == edict.TargetFactionId);
        if (targetFaction is null)
        {
            logs.Add($"Faction {issuerFaction.FactionId}: trade contract target {edict.TargetFactionId} not found.");
            return;
        }

        var exporter = edict.IsInboundToIssuer ? targetFaction : issuerFaction;
        var importer = edict.IsInboundToIssuer ? issuerFaction : targetFaction;

        exporter.Resources[edict.Resource.Value] = exporter.Resources.GetValueOrDefault(edict.Resource.Value) - edict.Amount;
        importer.Resources[edict.Resource.Value] = importer.Resources.GetValueOrDefault(edict.Resource.Value) + edict.Amount;

        logs.Add($"Trade: {exporter.FactionId} -> {importer.FactionId}: {edict.Amount} {edict.Resource.Value}.");

        if (exporter.FactionId == "aurumbrae")
        {
            var bonusGold = edict.Amount / 5;
            if (bonusGold > 0)
            {
                exporter.Resources[ResourceType.Gold] = exporter.Resources.GetValueOrDefault(ResourceType.Gold) + bonusGold;
                logs.Add($"Faction aurumbrae special: +{bonusGold} Gold from trade exports.");
            }
        }
    }

    private static void ResolveDefensivePact(GameState state, string factionA, string factionB, List<string> logs)
    {
        var alreadyExists = state.DefensivePacts.Any(p =>
            (p.FactionAId == factionA && p.FactionBId == factionB) ||
            (p.FactionAId == factionB && p.FactionBId == factionA));

        if (alreadyExists)
        {
            return;
        }

        state.DefensivePacts.Add(new DefensivePact
        {
            FactionAId = factionA,
            FactionBId = factionB
        });

        logs.Add($"Defensive pact established: {factionA} <-> {factionB}.");
    }

    private static void ApplyFactionSpecialEdictBonuses(
        FactionState factionState,
        IReadOnlyList<InternalProductionEdict> successfulInternalEdicts,
        List<string> logs)
    {
        if (factionState.FactionId != "taznar")
        {
            return;
        }

        var magicalSpringCount = successfulInternalEdicts.Count(edict =>
            string.Equals(edict.EdictName, "Magické zřídlo", StringComparison.OrdinalIgnoreCase));

        if (magicalSpringCount <= 0)
        {
            return;
        }

        factionState.Resources[ResourceType.Conscripts] =
            factionState.Resources.GetValueOrDefault(ResourceType.Conscripts) + magicalSpringCount;
        logs.Add($"Faction taznar special: +{magicalSpringCount} Conscripts from Magické zřídlo.");
    }

    private static void CommitMilitaryTroops(GameState state, IReadOnlyList<MilitaryEdict> militaryIntents, List<string> logs)
    {
        var availableByFaction = state.Factions.ToDictionary(
            f => f.FactionId,
            f => new Dictionary<UnitType, int>
            {
                [UnitType.Army] = f.Units.GetValueOrDefault(UnitType.Army),
                [UnitType.Fleet] = f.Units.GetValueOrDefault(UnitType.Fleet),
                [UnitType.Mages] = f.Units.GetValueOrDefault(UnitType.Mages)
            });

        foreach (var edict in militaryIntents)
        {
            if (!availableByFaction.TryGetValue(edict.IssuingFactionId, out var available))
            {
                continue;
            }

            edict.CommittedArmy = Math.Min(Math.Max(edict.RequestedArmy, 0), available[UnitType.Army]);
            available[UnitType.Army] -= edict.CommittedArmy;

            edict.CommittedFleet = Math.Min(Math.Max(edict.RequestedFleet, 0), available[UnitType.Fleet]);
            available[UnitType.Fleet] -= edict.CommittedFleet;

            edict.CommittedMages = Math.Min(Math.Max(edict.RequestedMages, 0), available[UnitType.Mages]);
            available[UnitType.Mages] -= edict.CommittedMages;

            logs.Add($"Military commit: {edict.IssuingFactionId} committed A:{edict.CommittedArmy} F:{edict.CommittedFleet} M:{edict.CommittedMages} for {edict.Type}.");
        }
    }

    private static void ApplyResourceChanges(Dictionary<ResourceType, int> resources, Dictionary<ResourceType, int> changes)
    {
        foreach (var (resource, delta) in changes)
        {
            resources[resource] = resources.GetValueOrDefault(resource) + delta;
        }
    }

    private static List<PreparedBattle> PrepareBattles(GameState state, IReadOnlyList<MilitaryEdict> militaryIntents, List<string> logs)
    {
        var battlesByCity = new Dictionary<string, PreparedBattle>();

        foreach (var militaryEdict in militaryIntents)
        {
            if (string.IsNullOrWhiteSpace(militaryEdict.TargetCityId))
            {
                continue;
            }

            var city = state.Cities.FirstOrDefault(c => c.CityId == militaryEdict.TargetCityId);
            if (city is null)
            {
                logs.Add($"Military intent skipped: target city '{militaryEdict.TargetCityId}' does not exist.");
                continue;
            }

            if (!battlesByCity.TryGetValue(city.CityId, out var preparedBattle))
            {
                preparedBattle = new PreparedBattle
                {
                    TargetCityId = city.CityId,
                    DefenderFactionId = city.OwnerFactionId,
                    DefenderGarrisonStrength = city.GarrisonStrength
                };
                battlesByCity[city.CityId] = preparedBattle;
            }

            var attacker = state.Factions.FirstOrDefault(f => f.FactionId == militaryEdict.IssuingFactionId);
            if (attacker is null)
            {
                continue;
            }

            preparedBattle.Attacks.Add(new PreparedBattleAttack
            {
                AttackerFactionId = attacker.FactionId,
                OperationType = militaryEdict.Type,
                AvailableArmy = attacker.Units.GetValueOrDefault(UnitType.Army),
                AvailableFleet = attacker.Units.GetValueOrDefault(UnitType.Fleet),
                AvailableMages = attacker.Units.GetValueOrDefault(UnitType.Mages),
                CommittedArmy = militaryEdict.CommittedArmy,
                CommittedFleet = militaryEdict.CommittedFleet,
                CommittedMages = militaryEdict.CommittedMages
            });
        }

        return battlesByCity.Values.ToList();
    }

    private static List<string> ShuffleFactions(IEnumerable<string> factionIds, int seed)
    {
        var shuffled = factionIds.ToList();
        var random = new Random(seed);

        for (var i = shuffled.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        return shuffled;
    }
}

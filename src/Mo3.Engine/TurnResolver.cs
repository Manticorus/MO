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
        ResolveBattles(state, militaryIntents, preparedBattles, logs);
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

    private static void ResolveBattles(
        GameState state,
        IReadOnlyList<MilitaryEdict> militaryIntents,
        IReadOnlyList<PreparedBattle> preparedBattles,
        List<string> logs)
    {
        if (preparedBattles.Count == 0)
        {
            return;
        }

        var factionById = state.Factions.ToDictionary(f => f.FactionId);
        var intentsByCity = militaryIntents
            .Where(i => !string.IsNullOrWhiteSpace(i.TargetCityId))
            .GroupBy(i => i.TargetCityId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var remainingByFaction = state.Factions.ToDictionary(
            faction => faction.FactionId,
            faction => new Dictionary<UnitType, int>
            {
                [UnitType.Army] = Math.Max(0, faction.Units.GetValueOrDefault(UnitType.Army) - militaryIntents.Where(e => e.IssuingFactionId == faction.FactionId).Sum(e => e.CommittedArmy)),
                [UnitType.Fleet] = Math.Max(0, faction.Units.GetValueOrDefault(UnitType.Fleet) - militaryIntents.Where(e => e.IssuingFactionId == faction.FactionId).Sum(e => e.CommittedFleet)),
                [UnitType.Mages] = Math.Max(0, faction.Units.GetValueOrDefault(UnitType.Mages) - militaryIntents.Where(e => e.IssuingFactionId == faction.FactionId).Sum(e => e.CommittedMages))
            });

        foreach (var battle in preparedBattles.OrderBy(b => b.TargetCityId, StringComparer.Ordinal))
        {
            var city = state.Cities.First(c => c.CityId == battle.TargetCityId);
            var cityIntents = intentsByCity.GetValueOrDefault(battle.TargetCityId) ?? [];
            var primaryAttacks = battle.Attacks
                .Where(a => a.OperationType is MilitaryEdictType.Attack or MilitaryEdictType.Takeover or MilitaryEdictType.Liberation)
                .ToList();

            if (primaryAttacks.Count == 0)
            {
                logs.Add($"Battle {battle.TargetCityId}: no primary attacks submitted.");
                continue;
            }

            var supportForFaction = cityIntents
                .Where(i => i.Type == MilitaryEdictType.SupportAttack && !string.IsNullOrWhiteSpace(i.SupportedFactionId))
                .GroupBy(i => i.SupportedFactionId!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            var attackCandidates = primaryAttacks.Select(primary =>
            {
                var contributors = new List<BattleContributor>
                {
                    new(primary.AttackerFactionId, primary.CommittedArmy, primary.CommittedFleet, primary.CommittedMages)
                };

                if (supportForFaction.TryGetValue(primary.AttackerFactionId, out var supports))
                {
                    contributors.AddRange(supports.Select(s => new BattleContributor(
                        s.IssuingFactionId,
                        s.CommittedArmy,
                        s.CommittedFleet,
                        s.CommittedMages)));
                }

                return new AttackCandidate(primary, contributors);
            }).ToList();

            var bestAttackPower = attackCandidates.Max(a => ComputeTotalPower(a.Contributors));
            var bestAttackers = attackCandidates.Where(a => ComputeTotalPower(a.Contributors) == bestAttackPower).ToList();
            if (bestAttackers.Count > 1)
            {
                logs.Add($"Battle {battle.TargetCityId}: multiple strongest attackers tied; defender holds.");
                continue;
            }

            var bestAttack = bestAttackers[0];
            var attackerPower = ComputeTotalPower(bestAttack.Contributors);
            var attackerUnits = ComputeTotalUnits(bestAttack.Contributors);

            var defenderContributors = MobilizeDefenders(
                state,
                battle.DefenderFactionId,
                attackerPower,
                remainingByFaction,
                logs,
                battle.TargetCityId);

            var defenderMilitaryPower = ComputeTotalPower(defenderContributors);
            var defenderMilitaryUnits = ComputeTotalUnits(defenderContributors);
            var defenderTotalPower = battle.DefenderGarrisonStrength + defenderMilitaryPower;
            var defenderWins = defenderTotalPower >= attackerPower;

            if (defenderWins)
            {
                var attackerCasualtyCount = (int)Math.Ceiling(attackerUnits * 0.5m);
                ApplyCasualtiesToContributors(factionById, bestAttack.Contributors, attackerCasualtyCount, logs);

                var defenderCasualtyCount = (int)Math.Ceiling(defenderMilitaryUnits * 0.25m);
                ApplyCasualtiesToContributors(factionById, defenderContributors, defenderCasualtyCount, logs);

                logs.Add($"Battle {battle.TargetCityId}: defender {battle.DefenderFactionId} wins (def {defenderTotalPower} vs atk {attackerPower}).");
                continue;
            }

            var attackerWinnerCasualtyCount = (int)Math.Ceiling(attackerUnits * 0.25m);
            ApplyCasualtiesToContributors(factionById, bestAttack.Contributors, attackerWinnerCasualtyCount, logs);

            var losingDefenderRate = HasElyndar(bestAttack.Contributors) ? 1m : 0.5m;
            var defenderLoserCasualtyCount = (int)Math.Ceiling(defenderMilitaryUnits * losingDefenderRate);
            ApplyCasualtiesToContributors(factionById, defenderContributors, defenderLoserCasualtyCount, logs);

            if (bestAttack.PrimaryAttack.OperationType == MilitaryEdictType.Takeover)
            {
                city.OwnerFactionId = bestAttack.PrimaryAttack.AttackerFactionId;
                city.OccupyingFactionId = null;
                logs.Add($"Battle {battle.TargetCityId}: takeover by {bestAttack.PrimaryAttack.AttackerFactionId}.");
            }
            else if (bestAttack.PrimaryAttack.OperationType == MilitaryEdictType.Liberation)
            {
                city.OccupyingFactionId = null;
                logs.Add($"Battle {battle.TargetCityId}: liberation by {bestAttack.PrimaryAttack.AttackerFactionId}.");
            }
            else
            {
                city.OccupyingFactionId = bestAttack.PrimaryAttack.AttackerFactionId;
                logs.Add($"Battle {battle.TargetCityId}: occupation by {bestAttack.PrimaryAttack.AttackerFactionId}.");
            }
        }
    }

    private sealed record BattleContributor(string FactionId, int Army, int Fleet, int Mages)
    {
        public int TotalUnits => Army + Fleet + Mages;

        public int CombatPower => Army + Fleet + (Mages * 5);
    }

    private sealed record AttackCandidate(PreparedBattleAttack PrimaryAttack, List<BattleContributor> Contributors);

    private static int ComputeTotalUnits(IEnumerable<BattleContributor> contributors)
    {
        return contributors.Sum(c => c.TotalUnits);
    }

    private static int ComputeTotalPower(IEnumerable<BattleContributor> contributors)
    {
        return contributors.Sum(c => c.CombatPower);
    }

    private static bool HasElyndar(IEnumerable<BattleContributor> contributors)
    {
        return contributors.Any(c => c.FactionId == "elyndar");
    }

    private static List<BattleContributor> MobilizeDefenders(
        GameState state,
        string defenderFactionId,
        int targetPower,
        Dictionary<string, Dictionary<UnitType, int>> remainingByFaction,
        List<string> logs,
        string targetCityId)
    {
        var contributors = new List<BattleContributor>();
        var powerSoFar = 0;

        foreach (var factionId in new[] { defenderFactionId }.Concat(GetDefensiveAllies(state, defenderFactionId)))
        {
            if (powerSoFar >= targetPower)
            {
                break;
            }

            if (!remainingByFaction.TryGetValue(factionId, out var available))
            {
                continue;
            }

            var mobilizedArmy = 0;
            var mobilizedFleet = 0;
            var mobilizedMages = 0;

            while (powerSoFar < targetPower && available[UnitType.Army] > 0)
            {
                available[UnitType.Army]--;
                mobilizedArmy++;
                powerSoFar++;
            }

            while (powerSoFar < targetPower && available[UnitType.Fleet] > 0)
            {
                available[UnitType.Fleet]--;
                mobilizedFleet++;
                powerSoFar++;
            }

            while (powerSoFar < targetPower && available[UnitType.Mages] > 0)
            {
                available[UnitType.Mages]--;
                mobilizedMages++;
                powerSoFar += 5;
            }

            if (mobilizedArmy + mobilizedFleet + mobilizedMages <= 0)
            {
                continue;
            }

            contributors.Add(new BattleContributor(factionId, mobilizedArmy, mobilizedFleet, mobilizedMages));
            logs.Add($"Battle {targetCityId}: {factionId} auto-defends with A:{mobilizedArmy} F:{mobilizedFleet} M:{mobilizedMages}.");
        }

        return contributors;
    }

    private static void ApplyCasualtiesToContributors(
        IReadOnlyDictionary<string, FactionState> factionById,
        IReadOnlyList<BattleContributor> contributors,
        int casualtyCount,
        List<string> logs)
    {
        var casualtiesLeft = casualtyCount;

        foreach (var contributor in contributors)
        {
            if (casualtiesLeft <= 0)
            {
                break;
            }

            if (!factionById.TryGetValue(contributor.FactionId, out var faction))
            {
                continue;
            }

            var contributorUnitsLeft = contributor.TotalUnits;
            if (contributorUnitsLeft <= 0)
            {
                continue;
            }

            var contributorCasualties = Math.Min(contributorUnitsLeft, casualtiesLeft);
            casualtiesLeft -= contributorCasualties;
            ApplyTotalCasualties(faction, contributorCasualties, logs);
        }
    }

    private static IEnumerable<string> GetDefensiveAllies(GameState state, string defenderFactionId)
    {
        return state.DefensivePacts
            .SelectMany(pact =>
            {
                if (pact.FactionAId == defenderFactionId)
                {
                    return new[] { pact.FactionBId };
                }

                if (pact.FactionBId == defenderFactionId)
                {
                    return new[] { pact.FactionAId };
                }

                return Array.Empty<string>();
            })
            .Distinct(StringComparer.Ordinal);
    }

    private static void ApplyTotalCasualties(FactionState faction, int casualties, List<string> logs)
    {
        if (casualties <= 0)
        {
            return;
        }

        var remaining = casualties;
        foreach (var unitType in new[] { UnitType.Army, UnitType.Fleet, UnitType.Mages })
        {
            if (remaining <= 0)
            {
                break;
            }

            var available = faction.Units.GetValueOrDefault(unitType);
            if (available <= 0)
            {
                continue;
            }

            var loss = Math.Min(available, remaining);
            faction.Units[unitType] = available - loss;
            remaining -= loss;
        }

        logs.Add($"Casualties: {faction.FactionId} loses {casualties - remaining} unit(s).");
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

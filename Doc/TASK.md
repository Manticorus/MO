# Tasks (MO3 Resolver MVP)

Principles:
- Keep repo buildable after every task.
- Each resolver logic task includes at least 1 unit test.
- Geography is stubbed: all cities reachable; do NOT add adjacency/map systems.

## M0 — Solution & scaffolding
- [ ] Create solution structure:
      src/Mo3.Engine (classlib)
      src/Mo3.App (WPF)
      tests/Mo3.Tests (xUnit)
- [ ] Add MVVM helpers to App: ObservableObject + RelayCommand.
- [ ] Update README with build/test commands.

## M1 — Core domain model + JSON
- [ ] Define basic models/enums:
      ResourceType, UnitType
      City, Faction, WorldDefinition
      FactionState (resources, units, collapse flag)
      CityState (owner, occupation/vassal flags, garrison strength)
      GameState (turn number, seed, factions, cities, pacts)
- [ ] JSON schema version field + roundtrip serialization.
- [ ] Unit test: GameState JSON roundtrip.

## M2 — Edict limit + production math
- [ ] Implement EdictLimitCalculator per rules.
- [ ] Implement ProductionScaling:
      allow up to 2*limit
      above limit => half output
- [ ] Unit tests with a small sample setup.

## M3 — Typed edicts + validation layer
- [ ] Create typed edict models:
      Internal (production)
      External (trade contract, defensive pact, spy, etc.)
      Military (attack, support attack, end occupation, takeover, liberation, etc.)
- [ ] Implement EdictValidator:
      - economic collapse restriction (only trade allowed)
      - parameter validation (missing target, negative amounts, etc.)
      - NOTE: no adjacency validation (MVP stub!)
- [ ] Unit tests for validator behavior.

## M4 — Turn resolution pipeline (sections + logs)
- [ ] Implement TurnResolver.ResolveTurn(state, ordersByFaction, seed):
      - shuffle faction order by seed
      - resolve section I (snapshot)
      - resolve section II (snapshot)
      - resolve section III (snapshot)
      - collect military intents (do not fight yet)
      - generate log entries
- [ ] Unit test: deterministic output with fixed seed.

## M5 — Military resolution + battles (NO geography restrictions)
- [ ] Implement battle resolution:
      - collect attacks & supports
      - defender auto-mobilization
      - tie -> defender wins
      - casualties:
          winner loses ceil(deployed/4)
          loser loses ceil(deployed/2)
      - outcomes: occupation (default), takeover, liberation
- [ ] Implement ally support on defense (MVP rule):
      - if defender has defensive pact allies, auto-commit ally forces (subject to availability)
- [ ] Unit tests for: tie behavior, casualty rounding, ally support included.

## M6 — Recruitment step
- [ ] Implement “no military edicts this turn” check.
- [ ] Implement recruitment + caps (per rules).
- [ ] Unit tests for caps and condition.

## M7 — Persistence + sample data
- [ ] AppData storage service:
      load/save world.json + state.json
      create sample data if missing
      write logs/turn_N.md
- [ ] Smoke test steps documented in README.

## M8 — WPF UI MVP
- [ ] Dashboard: load/save, resolve, faction overview, open editors
- [ ] Orders editor: structured per-section edict entry
- [ ] Log viewer: open last log
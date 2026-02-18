# Mlžné Ostrovy – Resolver Engine (MO3)

## Goal
Replace the current turn-resolving workflow with a deterministic engine + WPF UI that:
1) stores game state (factions/cities/resources/units/pacts),
2) ingests one “Rozkaz” per faction per turn,
3) resolves per rules,
4) outputs a clear turn log + updated state.

## MVP Geography Assumption (IMPORTANT)
For MVP, the resolver does NOT model geography or adjacency.
- Every city is considered adjacent/reachable from every other city.
- Therefore: all attacks are legal; all trade/pacts that require “neighboring” are legal.
- There are NO enclaves (everything is connected), and any rules involving dominion/enclaves are treated as “not applicable” in MVP.
- Any sea/road restrictions are ignored in MVP (no “port network” logic, no “army <= fleet across water”, etc.).

This is a deliberate stub. In a later version, geography can be added as a pluggable rule module.

## MVP Ally Support Assumption (IMPORTANT)
For MVP, if the defender has allies via defensive pact:
- Allies are assumed to respond to every defense automatically.
- The system will automatically commit ally forces to defend, using whatever is available after their own commitments.
- There is no reachability check for allied support.

(If an ally lacks available units or required resources, their contribution is reduced accordingly; we still don’t create units from nothing.)

## Turn structure
- Each faction submits at most 1 order sheet per turn.
- Orders are grouped into sections: I, II, III, Military.
- Within a section, edicts resolve “simultaneously” from a snapshot at section start.
- Sections resolve sequentially: I -> II -> III -> Military.
- Battles resolve at end-of-turn.
- Recruitment happens at end-of-turn with the “no military edicts this turn” condition.
- Order sheets are evaluated in random order; engine supports a “turn seed” for reproducibility.

## Data & storage
Store as JSON in `%AppData%/MO3Resolver/`
- world.json (cities, factions, static definitions)
- state.json (current ownership, resources, units, pacts, occupations)
- logs/turn_N.md (resolution output)
- optional snapshots per turn

## Architecture
- `Mo3.Engine` (pure domain + resolver; no WPF refs)
- `Mo3.App` (WPF UI, MVVM)
- `Mo3.Tests` (unit tests for resolver rules)
- Deterministic output for same input+seed.
- Every step produces log entries.

## UI (MVP)
### Dashboard
- Load/save state
- Turn number, seed, Resolve button
- List factions with resource/unit overview + collapse warning
- Open Orders editor per faction
- Open latest log

### Orders editor
- Structured edict editor (dropdown type + parameter fields), per section
- Validation summary (show clear reasons why an edict is invalid)

### Log viewer
- View generated markdown/text log, with filters (optional)

## Acceptance criteria (MVP)
- `dotnet build` succeeds
- App can: load sample state -> input orders for 2+ factions -> resolve -> produce log -> save updated state
- Unit tests cover:
  - economic collapse gating
  - edict limit calculation
  - battle resolution + casualty rounding
  - recruitment rule
  - deterministic seed behavior

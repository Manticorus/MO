\# Mlžné Ostrovy – Resolver Engine (MO3)



\## Goal

Replace the existing “turn resolver” for Mlžné Ostrovy with a deterministic engine + WPF organizer UI that:

1\) stores game state (factions/cities/resources/edicts/war),

2\) ingests one “Rozkaz” per faction per turn,

3\) resolves it according to the rules,

4\) produces a clear resolution log + updated state.



\## Source of truth

Rules are defined in:

\- `MO3 - Herní pravidla a mechaniky.txt`

\- `MO3 - Příběh.txt`

If engine behavior and UI copy differ from those files, the files win.



\## MVP scope (v1)

\### Included in MVP

\- World model:

&nbsp; - Resources (Suroviny)

&nbsp; - Factions (Frakce)

&nbsp; - Cities (Města), adjacency graph (roads + ports)

&nbsp; - Capital, dominion, enclaves

&nbsp; - Army/Fleet/Mages units, garrison defense per city

\- Edicts (Edikty) and their resolution:

&nbsp; - Internal production edicts (Farma, Pila, Důl, Řemeslník, …)

&nbsp; - External edicts: Spy (Špionáž), Trade contract (Obchodní kontrakt), Defensive pact (Obranný pakt)

&nbsp; - Military edicts: Attack (Vojenský manévr), Support attack (Podpora útoku), End occupation, Trade takeover, Vassalization \& rebellion (as rules specify)

\- Turn structure:

&nbsp; - Each faction submits at most 1 order sheet per window.

&nbsp; - Edicts are grouped into sections: I, II, III, Military.

&nbsp; - Within a section, edicts resolve “simultaneously” using resources snapshot at section start.

&nbsp; - Sections resolve sequentially: I -> II -> III -> Military.

&nbsp; - Battles resolve after all orders are processed (end-of-turn).

&nbsp; - Recruitment happens at end-of-turn with the “no military edicts this turn” condition.

\- Output:

&nbsp; - Human-readable log (per faction + global summary).

&nbsp; - Updated game state saved to disk.



\### Explicitly NOT in MVP (v1)

\- Local roleplay abilities, death/ghost rules, etc.

\- Questbook UI for expeditions (we can track committed resources, but story resolution stays manual).

\- Map rendering / fancy visuals (simple list/table view only).

\- Authentication/multiuser networking.



\## Core rules to implement (summary)

This is what the engine must do (in code), matching the docs.



\### Resources (Suroviny)

Resource types:

\- Jídlo, Dřevo, Železo, Zlato, Magická tekutina, Pracovní Síla, Nástroje,

&nbsp; Luxusní zboží, Magické předměty, Lodě, Branci, Učenci.



Economic collapse:

\- If any resource becomes negative => faction enters “Ekonomický kolaps”.

\- While in collapse: faction cannot issue any edicts except trade contracts.

\- Collapse ends immediately when all resources are >= 0.



\### Cities / adjacency / dominion / enclaves

\- Cities are adjacent only if connected by roads.

\- Port cities are all adjacent to all other port cities via sea.

\- Dominion = cities connected to the faction capital by valid connections.

\- “Neighbors for trade/war/pacts” are determined by adjacency to dominion.

\- Enclave cities:

&nbsp; - do not contribute to edict limits,

&nbsp; - still contribute to faction size,

&nbsp; - automatic defense in enclave can mobilize only Mages (because others cannot reach).



\### Edict limit

\- Base edict limit starts at 2 for each resource type and never drops below 2.

\- Each city increases faction size by +1.

\- Each city increases edict limit in its 3 listed resource types by (20 - factionSize).

\- Internal production edicts may be declared up to max 2 \* edictLimit.

\- Any production edict above edictLimit produces only half of its normal output.



\### Military units

\- Army unit strength = 1

\- Fleet unit strength = 1

\- Mage unit strength = 5

\- Max Army and Fleet = (free Branci or Lodě) \* 3

\- Max Mages = (free Učenci) \* 1

\- Fleet exists only if faction has at least one port; losing last port loses entire fleet.



Recruitment:

\- End of turn, if faction executed NO military edicts:

&nbsp; - Army increases by free Branci (up to max)

&nbsp; - Fleet increases by free Lodě (up to max)

&nbsp; - Mages increase by floor(free Učenci / 4) (up to max)



\### War resolution

Attacks:

\- Attack target must neighbor attacker dominion, except:

&nbsp; - Mage-only attacks can target anywhere.

\- If attacking across water: Army committed cannot exceed Fleet committed.

\- In inland regions: Fleet cannot participate.



Defense:

\- If attacked, defender automatically mobilizes enough available forces to match attacker strength (as much as possible),

&nbsp; prioritizing Army, then Fleet (if port), then Mages.

\- Each city has a garrison that always defends; garrison is not part of faction units and cannot be moved.

\- Battles resolve after all edicts are evaluated.



Battle outcome:

\- Compare attackerStrength vs defenderStrength:

&nbsp; - higher wins; ties => defender wins.

\- Casualties:

&nbsp; - winner loses ceil(deployed / 4)

&nbsp; - loser loses ceil(deployed / 2)

\- On attacker victory vs a city: outcome is one of:

&nbsp; - Occupation (default if unspecified)

&nbsp; - Takeover

&nbsp; - Liberation (city becomes neutral)



Support attack:

\- A faction may support another faction’s attack (rules apply: reachability, water constraints, etc).

\- Additional food cost for Army support: 1x Food per each 3 Army committed (per rules text).



Occupation ending:

\- Separate edict can end occupation with choices: Takeover, Liberation, Withdrawal (default if unspecified).



Vassalization \& rebellion:

\- Implement the edicts described in rules (diplomatic vassalization, changing taxes, rebellion battle rules, etc.)

\- If rule text is ambiguous in implementation, log it clearly and follow the most literal reading.



\### Random order of resolving orders

\- Order sheets are evaluated in random order.

\- Engine must support setting a “turn seed” so results can be reproduced.

&nbsp; - UI: seed input (optional); if blank, generate and store.



\## Data \& storage

\### Save files

\- Store everything as JSON in `%AppData%/MO3Resolver/`

&nbsp; - `world.json` (cities, adjacency, base faction definitions)

&nbsp; - `state.json` (current ownership, resources, units, ongoing edicts, etc.)

&nbsp; - `turn\_YYYYMMDD\_HHMM\_seedXXXX.json` (optional snapshots)

&nbsp; - `logs/turn\_N.md` (resolution logs)



\### Editable config

\- Cities, factions, and starting state should be editable without recompiling:

&nbsp; - JSON schema versioned (`schemaVersion` field)

&nbsp; - Engine validates and reports errors



\## Architecture \& quality

\- Solution split:

&nbsp; - `Mo3.Engine` (pure domain + resolution; no WPF references)

&nbsp; - `Mo3.App` (WPF UI, MVVM)

&nbsp; - `Mo3.Tests` (unit tests for resolver rules)

\- Deterministic resolution given same inputs + seed.

\- Never block UI thread: file I/O async.

\- Every resolution step must generate a log entry.



\## UI (MVP)

\### Screen 1: Dashboard

\- Load/save state

\- Turn number, seed, “Resolve turn” button

\- List of factions with:

&nbsp; - resources overview

&nbsp; - units overview

&nbsp; - economic collapse indicator

\- Button to open “Orders input” for each faction

\- “Open last resolution log”



\### Screen 2: Orders input

\- Select faction, enter one order sheet:

&nbsp; - Section I: list of edicts

&nbsp; - Section II: list of edicts

&nbsp; - Section III: list of edicts

&nbsp; - Military: list of military edicts

\- Each edict entry is structured (dropdown type + parameters), not free text.

\- Validate:

&nbsp; - max 1 order sheet per turn per faction

&nbsp; - economic collapse restriction

&nbsp; - reachability constraints (for trade/war/pacts)



\### Screen 3: Turn log viewer

\- Render markdown log with filters: Global / Per faction / Battles only.



\## Acceptance criteria (v1)

\- `dotnet build` succeeds

\- App can:

&nbsp; 1) Load sample world+state JSON

&nbsp; 2) Input at least 2 factions’ orders

&nbsp; 3) Resolve turn

&nbsp; 4) Produce deterministic log + updated state JSON

\- Unit tests cover:

&nbsp; - edict limit calc

&nbsp; - economic collapse gating

&nbsp; - battle resolution + casualties

&nbsp; - recruitment rule

&nbsp; - dominion/enclave detection




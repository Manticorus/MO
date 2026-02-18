\# Tasks (MO3 Resolver)



Principles:

\- Keep repo buildable after every task.

\- Each PR/task must include at least 1 unit test if it changes resolver logic.

\- Engine first, UI second.



\## M0 — Solution \& scaffolding

\- \[ ] Create solution structure:

&nbsp;     src/Mo3.Engine (classlib)

&nbsp;     src/Mo3.App (WPF)

&nbsp;     tests/Mo3.Tests (xUnit)

\- \[ ] Add basic CI-ish script (optional): `dotnet test` in README.

\- \[ ] Add MVVM helpers to App: ObservableObject, RelayCommand.



\## M1 — Core domain model (Engine)

\- \[ ] Define enums/models:

&nbsp;     ResourceType, UnitType

&nbsp;     Faction, City, WorldGraph

&nbsp;     FactionState (resources, units, collapse flag)

&nbsp;     CityState (owner, occupation/vassal flags, garrison strength)

&nbsp;     GameState (turn number, seed, factions, cities)

\- \[ ] JSON serialization model + version field.

\- \[ ] Unit tests: JSON roundtrip for GameState.



\## M2 — Map graph + dominion/enclaves

\- \[ ] Implement adjacency graph:

&nbsp;     roads edges + “all ports adjacent to all ports”

\- \[ ] Implement:

&nbsp;     GetDominionCities(faction)

&nbsp;     GetEnclaveCities(faction)

&nbsp;     IsNeighboringFactionViaDominion(A,B)

\- \[ ] Unit tests for dominion/enclave detection.



\## M3 — Edict limit + production math

\- \[ ] Implement EdictLimitCalculator:

&nbsp;     base=2

&nbsp;     per-city bonus = (20 - size)

&nbsp;     city contributes to 3 resource types (from city definition)

&nbsp;     enclaves do NOT contribute

\- \[ ] Implement ProductionScaling:

&nbsp;     allow up to 2\*limit

&nbsp;     above limit => half output

\- \[ ] Unit tests with small sample world.



\## M4 — Edict definitions (typed commands)

\- \[ ] Create Edict model hierarchy:

&nbsp;     InternalEdict (Farm, Sawmill, etc.)

&nbsp;     ExternalEdict (Spy, TradeContract, DefensivePact, Expedition placeholder)

&nbsp;     MilitaryEdict (Attack, SupportAttack, EndOccupation, TradeTakeover, Vassalization, Rebellion...)

\- \[ ] Define parameters per edict (e.g., target city, amounts, outcome choice).

\- \[ ] Add validation layer: EdictValidator with clear error messages.



\## M5 — Turn resolution pipeline (non-military first)

\- \[ ] Implement TurnResolver.ResolveTurn(state, ordersByFaction, seed):

&nbsp;     - shuffle order sheets by seed

&nbsp;     - resolve section I (simultaneous per faction)

&nbsp;     - resolve section II

&nbsp;     - resolve section III

&nbsp;     - collect military intents (do not fight yet)

&nbsp;     - apply economic collapse gating

&nbsp;     - create log entries

\- \[ ] Unit test: deterministic result with fixed seed.



\## M6 — Military resolution + battles

\- \[ ] Implement reachability rules:

&nbsp;     - target must neighbor dominion unless mage-only

&nbsp;     - water constraint: army <= fleet

&nbsp;     - inland: no fleet

&nbsp;     - defense auto-mobilization priority Army > Fleet > Mages

&nbsp;     - enclave defense only from Mages

&nbsp;     - garrison always defends (city-level stat)

\- \[ ] Implement battle resolution and casualties (ceil 1/4, ceil 1/2).

\- \[ ] Implement outcomes: occupation (default), takeover, liberation.

\- \[ ] Unit tests: tie -> defender, casualty rounding, default outcome behavior.



\## M7 — Recruitment step

\- \[ ] Implement “no military edicts this turn” check.

\- \[ ] Implement recruitment and caps:

&nbsp;     max Army/Fleet = (free branci/lodě)\*3, max mages=(učenci)\*1

&nbsp;     mages increment = floor(učenci/4)

&nbsp;     fleet removed if faction has no port cities

\- \[ ] Unit tests for all caps and conditions.



\## M8 — Persistence + snapshots

\- \[ ] AppData storage service:

&nbsp;     load world.json + state.json

&nbsp;     save updated state.json

&nbsp;     optional turn snapshots + log file

\- \[ ] Graceful missing files => create sample data.



\## M9 — WPF UI MVP

\- \[ ] Dashboard screen: load/save, faction list, resolve button, log link.

\- \[ ] Orders input screen:

&nbsp;     structured edict editor (type dropdown + parameter fields)

&nbsp;     per-section lists

&nbsp;     validation summary

\- \[ ] Log viewer screen (markdown rendering or simple text viewer).



\## M10 — Polish

\- \[ ] “Explain why edict failed” UX

\- \[ ] Export logs per faction

\- \[ ] Basic import/export of orders as JSON (so you can paste from Google Docs later)



\## Later (not MVP)

\- \[ ] Expedition questbook support (tags/boost/solve flow)

\- \[ ] Region crisis modifiers

\- \[ ] Better map UI

\- \[ ] Multi-operator merge/conflict support




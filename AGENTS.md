# AGENTS.md — Codex instructions for this repo

## Mission
Work as an incremental pair programmer. Keep diffs small, keep the repo buildable, and follow the MVP spec.

## MVP hard constraints
- **NO geography/map logic** in MVP:
  - Do not implement adjacency, dominion, enclaves, ports/sea travel, or reachability rules.
  - Assume every city is reachable from every other city.
  - Therefore all attacks are legal.
  - Defensive-pact allies are assumed to come to defend (subject to available units/resources).
- Do not invent rules beyond what `SPEC.md` states and the provided rule/story docs.

## Workflow rules
1. Before coding, briefly restate:
   - which TASK you’re doing (from `TASKS.md`)
   - which files you will touch
2. Keep changes small and reviewable (prefer < 300 lines diff per task).
3. Do not reformat unrelated files (especially XAML).
4. Prefer MVVM (bindings + ViewModels) over code-behind except trivial wiring.
5. After each task, run:
   - `dotnet build`
   - `dotnet test` (if tests exist; if not, still run it once tests exist)
6. If build/tests fail, fix them before stopping.
7. End each task by:
   - checking the task in `TASKS.md`
   - summarizing changes
   - listing commands run + results
   - listing assumptions (if any)

## Git hygiene (avoid conflicts)
Before any work:
- Run `git status --porcelain` and show the output.
- If clean:
  - `git fetch --all --prune`
  - `git pull --rebase`
  - Create a fresh branch per task: `git checkout -b codex/<short-task-name>`
- If not clean: STOP and report dirty files. Do not pull or rebase.

Never commit generated or local cache files:
- `.vs/`, `bin/`, `obj/`, `TestResults/`, `*.user`, `*.suo`
Ensure `.gitignore` includes these.

## Repo commands (update paths if they differ)
- Build: `dotnet build`
- Run: `dotnet run --project src/Mo3.App/Mo3.App.csproj`
- Test: `dotnet test`

## Structure conventions
- Engine: `src/Mo3.Engine/` (no WPF refs)
- App: `src/Mo3.App/` (WPF + MVVM)
- Tests: `tests/Mo3.Tests/` (xUnit)

## Output format
At the end of each task, output:
- Files changed/added
- Summary (bullet points)
- Commands run + results (build/test)
- Any follow-up tasks discovered

# AGENTS

## Repository rules

- Keep diffs small and focused.
- Prefer MVVM patterns in the WPF app.
- Always run:
  - `dotnet build`
  - `dotnet test`
- Do not reformat unrelated files.

## Git hygiene
Before any work:
- Run `git status --porcelain`
- If clean: `git fetch --all --prune` then `git pull --rebase`
- If not clean: stop and report dirty files; do not pull/rebase
Prefer working on a fresh branch per task: `codex/<task>`
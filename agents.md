\# Agent instructions (Codex)



\## Goal

Work in this repository as an incremental pair programmer. Keep diffs small and always keep the app buildable.



\## Tech

\- .NET

\- UI: WPF (.NET)

\- Language: C#



\## Workflow rules

1\. Before coding, briefly restate what you’re going to do and which files you’ll touch.

2\. Make small, reviewable changes (prefer < 300 lines diff per step).

3\. Do not introduce new frameworks unless explicitly requested.

4\. Follow existing patterns in the repo. Don’t reformat unrelated files.

5\. Prefer MVVM (ViewModels + binding) over code-behind, except for trivial view wiring.

6\. After each change, run:

&nbsp;  - `dotnet build`

&nbsp;  - `dotnet test` (if tests exist)

7\. If build/tests fail, fix them before proceeding.

8\. When finished, summarize changes and list commands run.



\## Quality bar

\- No crashes on startup.

\- No blocking UI thread for long operations (use async).

\- Use `INotifyPropertyChanged` properly for bindings.


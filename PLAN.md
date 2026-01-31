# Plan: Add `run` command (validation only)

## Summary
Add a new `run` CLI command that resolves a pipeline by definition path, verifies the current Git branch is clean and fully synced with its Azure DevOps upstream (no uncommitted changes, no ahead/behind), and exits with clear messaging if updates must be pushed. This iteration will **not** queue a pipeline run; it only validates readiness.

## Workplan
- [ ] Add a Git branch status service in `PipelineMonitor.Git` that:
  - Checks for a clean working tree (`git status --porcelain`).
  - Resolves upstream tracking ref (`git rev-parse --abbrev-ref --symbolic-full-name @{u}`), failing with a clear message if missing.
  - Computes ahead/behind counts (`git rev-list --left-right --count HEAD...@{u}`) and exposes results via an immutable record.
- [ ] Map the upstream remote name to its URL (via `IGitRemoteUrlProvider`) and verify it is an Azure DevOps remote. Parse with `IVstsGitUrlParser` and confirm it matches the resolved `LocalPipelineInfo` organization/project/repository.
- [ ] Implement `RunCommand` (RunCommand.cs) to:
  - Resolve the pipeline using `IPipelineInteractionService`.
  - Validate repo sync status and emit actionable errors/warnings for:
    - Uncommitted changes.
    - No upstream tracking branch.
    - Ahead/behind/diverged state (require push/pull).
    - Upstream remote mismatch or non-Azure DevOps remote.
  - On success, display a “ready to run” message (no run queued).
- [ ] Register `RunCommand` in `Program.cs` and register the new Git status service in DI.
- [ ] Add/adjust MSTest coverage for Git status parsing/logic where feasible (mock `IProcessRunner`).
- [ ] Run `dotnet build` and `dotnet test` before/after changes to confirm no regressions.

## Notes
- Follow repo conventions: file-scoped namespaces, `internal sealed` classes, collection expressions, no blank lines between `using` statements.
- Use `IInteractionService` for all user messaging; avoid silent failures.
- Do not trigger pipeline runs in this session; validation only.

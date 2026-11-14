---
name: cls-automerge
description: Resolve the conflicts of a merge the developer has started (e.g. master into research/WebApp-01, or research/WebApp-01 into research/WebApp-01-1) without losing changes of either side, then build and stage the resolved files. Use when the user types /cls-automerge or asks to resolve merge conflicts ("resolve the conflicts", "merge conflicts lossless", "Konflikte auflösen").
---

# Resolve merge conflicts losslessly

The developer runs `git merge` themselves and stops at the conflicts. This skill
resolves them so that **nothing of either side is lost**, verifies the result and
stages it. It never commits, aborts or resets the merge.

## 1. Look at the situation

```bash
git status --short
git branch --show-current          # target (ours / HEAD)
git log --oneline -3 MERGE_HEAD    # source (theirs)
git diff --name-only --diff-filter=U
```

- No `MERGE_HEAD` → no merge in progress: tell the user and stop.
- Usual chain in this repo: `master` → `research/WebApp-01` → `research/WebApp-01-1`.
  `master` is the main line; the research branches add remote monitoring
  (`RemoteMonitoring/`, `GameTimeNext.Shared/`, `RemoteMonitoring*` processes, settings,
  `EXEV_RemoteMonitoring*` / `EXEV_remoteMonitoring*` handlers, DTO conversion in `TX*` classes).

## 2. Understand each conflict before touching it

For every conflicted file, read the whole hunk with some context, and when it is
not obvious what each side did, compare with the merge base:

```bash
git show $(git merge-base HEAD MERGE_HEAD):<file>   # base
git show HEAD:<file>                                 # ours
git show MERGE_HEAD:<file>                           # theirs
```

Classify the hunk:

| Case | Resolution |
|------|------------|
| Both sides **added** different things at the same place (new methods, properties, registrations, background processes, XAML sections) | Keep both. Source side (the main line) first, then the branch's own additions, unless the order matters. |
| One side **changed** a line, the other **added** next to it | Keep the change and the addition. |
| Both sides changed the **same** line differently | Combine the intent of both (e.g. a new signature from one side, a new body from the other). If they really contradict each other, don't guess: ask the user with AskUserQuestion, showing both versions. |
| One side **deleted / moved** code the other changed | Follow the deletion / move and carry the change over to the new place. Check with `git grep` where it went. |
| Generated files (`T1*.cs`, `K1*.cs`, `TX*Basic.cs`, `Metadata/Data/`) | Never merge by hand into something new; take the side that matches the merged metadata (`devsync/T1METAP.csv`) and tell the user to regenerate if both sides changed the metadata. |
| `devsync/*.csv` | Keep the rows of both sides; a row (key) changed on both sides is asked about. |

## 3. Watch for conflicts git doesn't mark

Lossless also means the merged code still fits together:

- **Ordinal numbers** both sides assigned independently: `[UIXSignatureField(n)]`, enum values,
  message numbers, `MigTask_<version>_<seq>`, menu orders. The main line keeps its numbers;
  the branch's entries get the next free ones.
- **Changed signatures**: if one side changed a method signature (e.g. `EXEV_*` with a new
  parameter), `git grep` all callers, including `CallDispatcher.Trigger("EXEV_...", ...)`
  strings, and code the other side added that calls it the old way.
- **Renamed / moved types and members** (e.g. `ToastMessage` → `CFToast`, file dialogs → `FnSystemDialogs`):
  code added by the other side must use the new name.
- Duplicate registrations, `using`s, or `StartedBackgroundProcesses.Add` keys after keeping both sides.

## 4. Edit

Resolve with the Edit tool, hunk by hunk; keep comments, doc comments and formatting of both
sides. Don't refactor or "clean up" anything that isn't needed for the merge.

## 5. Verify

```bash
git grep -n -E '^(<<<<<<<|=======$|>>>>>>>)' -- . ':!*.md'   # no markers left
dotnet build GameTimeNext.slnx                               # or the project, see below
python Scripts/cleancode.py
```

If a running GTN / RemoteMonitoring locks `bin/`, build into the scratchpad with
`-o <scratchpad>/build` instead. The build must have 0 errors; new warnings in merged
files are mentioned.

## 6. Stage, don't commit

`git add` the resolved files so the developer only has to commit. Do **not** run
`git commit`, `git merge --continue`, `--abort` or `git reset`, and don't offer to.

## 7. Report

Reply with one short block per conflicted file: what each side brought and how it was
combined, plus every non-marked fix from step 3 (renumbered fields, adjusted callers, ...)
and anything that was asked or left open. End with the build / check result.

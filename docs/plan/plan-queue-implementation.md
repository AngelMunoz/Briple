# Implementation Plan — Plan Queue (date-driven timeline)

> Imports queue by date. Each import covers `[anchor, anchor + 7 * Semanas)`.
> Renders resolve per date; the preview calendar overrides the chained default.
> **Gate:** `dotnet build`, `pnpm test:browser`, `pnpm test:e2e`, `pnpm build`, fantomas clean.

## Slices

### S1 — Projection (pure)

- `planFinish (plan) (anchor)` = `addDays anchor (7 * plan.Semanas)`, the first free date.
- `chainedAnchor (plan) (genero) (opcionId) (today) (finishes)` = `max(defaultAnchor, latest finish)`; empty list = the heuristic. Compare via `daysBetween`, never DateOnly operators.
- `Projection.Tests.fs`: finish one day past the last covered date; chained picks the later of heuristic and latest finish; empty list falls back; completed plans never backdate.

### S2 — Store: retire the active pointer

- `putImport` writes the import row only; `deleteImport` deletes the row only. `getActiveImport` and `ActiveKey` die (the read dies in S3 with the boot swap so the solution builds at every slice). DB version stays 1.
- `Store.Tests.fs`: rewrite the three pointer tests against `listImports`.

### S3 — State + boot + views (one compile unit)

- `QueueEntry = { Import: StoredImport; Parsed: ParsedPlan }`; `plans: Var<QueueEntry list>` replaces `parsed`/`activeImport`.
- `entryFor (date)`: covering entries (latest anchor wins, tie by newest `ImportedAt`) → next upcoming (min anchor after the date) → latest finish → `None` on an empty queue. `currentEntry()` = `entryFor (today())`; `hasPlans()`; `viewFor (plan)` = stored view when valid, else `firstVariant`, no write-back.
- `StagedImport` gains `ChainedFrom: DateOnly option`. `stageImport`: `listImports()` → parse each `Raw` (skip failures) → `chainedAnchor` over every stored finish; a repeated file chains like any plan. Import ids carry a millisecond stamp so a repeat queues as its own entry instead of overwriting the previous copy.
- `commitImport` appends; `reAnchor` → `updateAnchor (id)`; `removePlan` → `removeAt (id)`; `activateImport` deleted; `resetToDefaults` clears `plans`; `init` takes `StoredImport list` and skips corrupt entries; the boot view rewrite runs against `currentEntry`.
- `Program.fs` boots from `listImports()`.
- Views resolve per date (a hand-moved anchor can split a week): Today's pivot keys on `(plans, view, weekStart)` and the hub/week rows resolve per row date; `planLineText`, `weekChipText`, `hasSelectedSession`, and the view flyout resolve by `selectedDate`. Shell's day share resolves by `selectedDate`; the plan bar by `currentEntry`. PlanPage shows `currentEntry`, edits its anchor through `updateAnchor`, and the history section becomes the queue (name + date range, today's row highlighted, tap jumps to the start date, per-row delete). SessionDetail resolves by `selectedDate`. Intents' deep-link guards use `hasPlans`. Preview shows a chained-start hint from `ChainedFrom` (es/en).

### S4 — e2e (three user stories) + devlog

Clock pinned 2026-09-14; the 4-week fixture finishes 2026-10-12. Asserted through rendered surfaces only.

1. Adding plans in advance: second import proposes "12 de octubre"; after commit Today still shows "Semana 1 de 4"; a third import proposes "9 de noviembre"; paging forward shows the queued weeks.
2. The queued plan starts on its date: fastForward past the finish; Today renders the second plan's "Semana 1 de 4".
3. My own date wins: override preview 2's start in the calendar; the third import chains after the chosen date.

`CHANGELOG.md`: Added bullet for the queue; **Breaking (behavioral):** manual activation removed, imports never replace a running plan's dates.

## Known trade-off

An open screen turns stale at midnight until the next interaction: Ripple renders on var writes, not the clock. Accepted.

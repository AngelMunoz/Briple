# Implementation Plan - Shell, Projection, Today (slices 2 to 5)

> Status: slices 2 to 5 are committed. Slice 6 is implemented in the working tree (section 12) and awaits review.
> Design: `docs/design/training-pwa-storyboards.md`, build order items 2 to 5.
> Style: Simplified Technical English (ASD-STE100). Procedural sentences: 20 words or fewer. Descriptive sentences: 25 words or fewer.

## 1. Scope

| Slice | Content |
|---|---|
| 2 | Projection: pure date to session functions |
| 3 | Shell, app bar, Today screen S1 and S2, interim import |
| 3b | Day hub inversion: hub of day sections inside the Day pivot |
| 4 | Week pivot S3, selectedDate, plan week chip, F3 navigation |
| 5 | View chip and variant flyout, F2b |

Out of scope: import preview (slice 6), session detail (slice 7), service worker (slice 8), desktop adaptation (slice 9).

## 2. Component facts

These facts come from the Metrino.Ripple bindings and the component sources. They control the decisions in section 3.

- `metro-pivot`: selection by tap. No pan handler exists. Content slide uses the `slow` token, 333 ms. Header moves use 167 ms. `selectedIndex` is a property. Event: `selectionchanged`.
- `metro-list-view`: a vertical virtual list. The container sets `overflow-y: auto` and `overflow-x: hidden`. Row math uses `scrollTop` and `item-height`. Items render as text through `display-member`. No item template surface exists.
- `metro-app-bar`: bottom placement is the default. Slots: buttons and `menu`.
- `metro-menu-flyout`: `open` attribute and `show(target)`. Minimal API.
- `metro-toast`: options are `title`, `message`, `severity`, `duration`. No action button. API is module level: `showToast`.
- Typography classes ship in `metrino.css`: `.display` 56 weight 300, `.title` 42, `.subheader` 26, `.header` 28, `.body` 15, `.caption` 12, `.badge-text` 11 bold upper.
- Tokens: transitions 167, 250, 333 ms. Spacing scale: 4, 8, 12, 16, 24, 40 px.
- Theme: no default. The stylesheet follows `prefers-color-scheme`. `data-theme` on `<html>` overrides. The app sets no `data-theme` in V1.
- Registration is explicit. `registerMetroX()` runs before element use.

## 3. Decisions

| Decision | Reason |
|---|---|
| Day navigation: a `metro-hub` of seven day sections inside the Day pivot item. Supersedes the strip row (slice 3b). | The pivot cannot pan, so the hub is the pan surface. The strip above the pivot stayed visible in the Week view, where it had no effect. |
| Week rows: plain rows, not list-view. | The rows need per-row markup: today bar, counts. list-view renders text only. |
| Today card: the full routine, flat. Mesociclo, circuit grouping, rests, and guía detail stay in the session detail (slice 7). | Today is the calendar surface; the detail is the plan view. Criteria revision 2026-09-20. |
| Pivot: tap selection. Chevrons scroll the hub day in Day view; they move a week in Week view. | The pivot has no pan handler (section 2). |
| Touch targets: 34 px target, 26 px floor. | Windows Phone 7 UI guide. The 44 px figure is UWP. |
| Navigation: Today is home. Plan and Settings are pages with a back chevron. The Today app bar menu holds the Plan and Settings items. | The app bar holds commands, never navigation (Windows Phone model). |
| Update, slice 8: toast announces, inline Reload state on Today. | Toast has no action button. The design shows the state inline, not as a popup. |
| Unknown Genero blocks: warned at parse, not shown. | `Genero` is `Hombre` or `Mujer` in format v1. The preview shows the warning. |
| Slice 3 import is interim: direct commit. | The preview flow is slice 6. Comments mark the interim code. |
| Opcion display: "3dias" renders "3 días". | The design chip shows this display form. Helper in slice 5. |

## 4. Slice 2 - Projection

### 4.1 Files

- New: `src/Plan/Projection.fs`. Position: after `Parser.fs`, before `Store\Db.fsi`.
- Edit: compile lists in `src/Briple.fsproj` and `tests/Briple.Tests.fsproj`.

Layer rule: Plan never references Store. The functions take plain arguments, not `StoredImport` or `ViewState`.

### 4.2 Contracts

```fsharp
module Plan.Projection

open System
open Plan.Types

/// The dia for a date inside the plan range; None before, after, or on rest.
val trySession: plan: Plan -> genero: Genero -> opcionId: string ->
                anchor: DateOnly -> date: DateOnly -> Dia option

/// 1-based plan week number for a date; None outside the plan range.
val planWeek: plan: Plan -> anchor: DateOnly -> date: DateOnly -> int option

/// The semana for a date; None when the opcion has no week there.
val trySemana: plan: Plan -> genero: Genero -> opcionId: string ->
               anchor: DateOnly -> date: DateOnly -> Semana option

/// True when the variant has a session on the date.
val trainsOn: plan: Plan -> genero: Genero -> opcionId: string ->
              anchor: DateOnly -> date: DateOnly -> bool

/// Next session at or after the date; for the rest-day line.
val nextSession: plan: Plan -> genero: Genero -> opcionId: string ->
                 anchor: DateOnly -> date: DateOnly -> (DateOnly * Dia) option

/// RIR target text for a 1-based plan week ("2-3"); None when absent or malformed.
val tryMesocicloRir: plan: Plan -> week: int -> string option

/// Design section 1 default anchor: the Monday of the current week when
/// the variant trains today, else the next Monday.
val defaultAnchor: plan: Plan -> genero: Genero -> opcionId: string ->
                   today: DateOnly -> DateOnly
```

### 4.3 Semantics

1. Plan week N covers `anchor + 7(N-1)` through `+ 6` days. Week offset: floor of the day difference over 7. A negative offset is before the anchor.
2. `trySemana` uses the length of the `Semanas` list of the opcion. `planWeek` uses `Plan.Semanas`. A week between the two counts renders as rest days.
3. Day match: map the date to a weekday and `Dia.Id` through `Parser.tryDayOfWeek`. First match wins. An unmapped `Dia.Id` never matches; the date is rest.
4. Mesociclo format: `#x mesociclo | Semana N: <text> | RIR <a-b> | <note>`. Match `Semana N:` at the start of the first segment. Take the `RIR` token from the next segment when present. Plain split and prefix checks. A missing or malformed RIR yields None.
5. `defaultAnchor`: the Monday of the current week. Take it when `trainsOn` is true for today. Else take it plus 7 days.

### 4.4 Tasks

1. Write `Projection.fs` with the section 4.2 signatures.
2. Add the file to both compile lists.
3. Add `Projection.Tests` to the test project.

### 4.5 Tests

Fixture anchor: Monday 2026-09-07.

- 2026-09-14 with `3dias` gives semana `Numero` 2. 2026-09-16 gives Full Body B.
- 2026-09-06 and 2026-10-05 give None.
- A Wednesday anchor shifts every window by two days.
- `3dias` trains on Monday, Wednesday, and Friday. Not on Tuesday.
- `nextSession` from 2026-09-12 gives 2026-09-14, Full Body A.
- `tryMesocicloRir`: week 2 gives "2-3". Week 5 gives None. Malformed gives None.
- `defaultAnchor`: Wednesday today gives this Monday. Tuesday today gives next Monday.
- Synthetic plan with an unmapped `Dia.Id`: that date is rest.
- Synthetic opcion with 2 semanas in a 4-week plan: weeks 3 and 4 are rest. `planWeek` still returns 3 and 4.

### 4.6 Acceptance

- `dotnet build` clean, src and tests, zero warnings.
- New asserts green in `pnpm test:browser`.
- No Plan to Store reference in the new code.

## 5. Slice 3 - Shell and Today

### 5.1 Files

- Rewrite `src/Program.fs`: boot point. Registrations, store read, mount.
- New `src/App/State.fs`: app state and persistence.
- New `src/App/Locale.fs`: chrome strings.
- New `src/App/Shell.fs`: screen switch, app bar.
- New `src/App/Today.fs`: date block, S1, S2.
- Compile order: State, Locale, Shell, Today, Program.

### 5.2 Interim import

The preview flow is slice 6. Until then:

1. "Try the sample plan" parses the bundled fixture, commits with `defaultAnchor`, shows a toast.
2. "Load a plan" opens a picker, parses, and commits on success. A parse error renders an error card with the line number.
3. Mark both code paths `INTERIM: replaced by S5b preview in slice 6`.

### 5.3 State

`Var` values in `State.fs`:

- `screen`: Today, Plan, or Settings. Plan and Settings show placeholders.
- `activeImport`: `StoredImport option`, read at boot.
- `view`: `Genero * string`. Default: store value, else first Bloque and first Opcion in file order.
- `selectedDate`: `DateOnly`, init today. Slice 4 adds persistence.
- `pivotIndex`: 0 or 1.

Persistence: `getViewState` and `setViewState` for the view. `setStateRaw "selectedDate"` from slice 4.

### 5.4 Locale

- String table, es and en. Selector: `navigator.language`. Fallback: en.
- Keys now: the empty-state lines, the button labels, the plan week line, the rest line, and the summary line.
- More keys: the Hoy line, the before-plan line, the completed line, and the start-date line.
- Dates and weekdays come from `Intl.DateTimeFormat`, not the table.

### 5.5 Boot

1. Register: Button, Pivot, PivotItem, AppBar, AppBarButton, Icon. Remove the demo registrations for TextBox, CheckBox, and ListView.
2. Read `getActiveImport` and `getViewState`.
3. Mount the shell.

### 5.6 Shell and pages

- Navigation: Today is home. Plan and Settings are pages (section 3). The app bar holds commands, never navigation.
- Today app bar: `metro-app-bar`, bottom placement. Menu slot: two `metro-app-bar-button` with `menu-item`, labels Plan and Settings. No icon buttons in V1.
- Plan page: back chevron in the header. App bar: Load file button and menu (export, remove) in slice 6.
- Settings page: placeholder with back chevron. Full page is V1.1.
- Browser back works: push a history state on page open. The chevron calls the same goBack path.
- `Html.show` switches pages on `screen`.

### 5.7 Today

Date block:

- Caption: weekday and date from the locale.
- Display: day number, `.display` class.
- Body: ISO week and quarter, "Week 38 · Q3" form. Helper with tests.
- Divider line below.

S1, no import:

- Empty-state lines from the table. Buttons "Load a plan" and "Try the sample plan" (5.2).
- No install banner. That work is slice 8.

S2, import present:

- Plan line: "Semana N de M · RIR x-y". `planWeek` gives N. `tryMesocicloRir` gives the RIR text; omit when None. Outside the range: the quiet table line.
- Day strip: seven `metro-button` (section 3). Letters: `Intl` weekday narrow. Dots: `trainsOn` per date, accent color. Selected day: accent. Target 34 px, floor 26 px.
- Chevrons: `back` and `forward` icons. They move `selectedDate` one day.
- Session card: accent rule on the left. Title `.header`. Badge line `.badge-text`: "6 EJERCICIOS · 2 CIRCUITOS". The full routine: every exercise of the dia with its scheme line, flat. Fields verbatim; `-` omitted. No truncation line. Mesociclo, circuit grouping, rests, and guía detail stay in slice 7. The card is non-interactive until slice 7.
- Rest day: centered caption "Descanso · próxima sesión: {weekday}, {title}" from `nextSession`.
- Dia title fallback: when `Dia.Titulo` is None, use the locale weekday name.
- Pivot: two `metro-pivot-item`, headers "Day" and "Week". Day holds the day hub and card. Week holds a placeholder until slice 4. `selectionchanged` writes `pivotIndex`. Set `selectedIndex` as a property.
- The view chip is slice 5. The plan line reads the `view` Var.

Slice 3b supersedes the day strip and chevron bullets above. See section 10.

### 5.8 Tests

- Interim import: parse fixture, commit, `getActiveImport` round trip through the store lane.
- App behavior is covered end to end by `tests/e2e.mjs` (Playwright): boot, the S1 state, the sample and file-import flows, the toast, the plan week line with RIR, the session card fields, reload persistence, the rest-day line, day strip selection, menu navigation, browser back, and both locales.
- The E2E clock is fixed on Monday 2026-09-14, the storyboard's example state, so every assertion is deterministic.

### 5.9 Acceptance

- S1 renders on a fresh browser profile. The sample commit leads to S2 on reload.
- The menu reaches Plan and Settings. Browser back and the chevron return to Today.
- S2 matches the storyboard with fixture data at anchor 2026-09-07.
- Gates green (section 8).

## 6. Slice 4 - Week pivot

### 6.1 Tasks

1. Persist `selectedDate`. Boot reads it and falls back to today.
2. Week rows: plain rows (section 3). For the seven dates of the plan week of the selected date. Each row: day number, locale weekday, title plus exercise count, or the rest text. The row for today: accent bar and highlight. Row model: pure builder.
3. Plan week chip. Inside: "Semana N de M". Before: "Empieza el {date}". After: "Plan completado". Quiet styles.
4. `back` and `forward` move `selectedDate` seven days. "Hoy" sets today.
5. Row tap: set `selectedDate`, set `pivotIndex` to Day. One `selectedDate` in state.
6. Summary line: "N sesiones esta semana".
7. Remove the Week placeholder.

### 6.2 Tests

- Row builder at anchor, week 2: seven rows, three sessions, four rest.
- Chip states: before, inside, after.
- `selectedDate` persistence round trip.
- Date to week and back helpers.

## 7. Slice 5 - View chip

### 7.1 Tasks

1. Chip: badge-style `metro-button`. Text: "{Genero} · {opcion display}". Icon: `chevron-down`.
2. Display helper: "3dias" to "3 días". Replace the `dias` suffix when present. Else the raw id. Tests pin it.
3. `metro-menu-flyout`: one group per Bloque. Group header: Genero, `.badge-text`. One item per Opcion, label `Titulo` or the helper. Active item: accent plus `check` icon.
4. Open: bind `open` to a Var, or `show(target)` through `attr.ref`. Verify light dismiss at build. Selection closes.
5. Selection: `setViewState`, close, re-render. Same anchor, same store.
6. Fallback: the stored view names a missing variant. Use the first variant in file order. Rewrite the store value.

### 7.2 Tests

- Group builder: Hombre `3dias` and `5dias`, Mujer `5dias`.
- Display helper: both mappings and the raw case.
- Fallback: the view references "7dias". First variant wins, store rewritten.

## 8. Gates

All green before the next slice:

1. `dotnet build`: zero errors, zero warnings, src and tests.
2. `pnpm test:browser`: 24 tests green. Parser, store, and projection suites against the real fixture and real IndexedDB.
3. `pnpm test:e2e`: nine end-to-end scenarios over the real app. Zero failures, zero page errors.
4. `pnpm build`: exit 0.
5. fantomas clean on touched files.

## 9. Risks

| Risk | Handling |
|---|---|
| `iconMap` key for the Plan icon | Verify at build. |
| Flyout light dismiss | Verify at slice 5. Fallback: explicit close. |
| 0.5.0 changes beyond the hub | Audit the 0.5.0 changelog before the hub work (10.1). |
| ISO week math errors | Small helper, table tests. |
| Interim import survives past slice 6 | INTERIM comments. Slice 6 removes them. |

## 10. Slice 3b - Day hub inversion

Metrino 0.5.0 shipped the hub API from the pre-validation, plus other changes. The bindings update (10.1) landed in commit 6c895f2. This slice is implemented in the working tree; it awaits review and no commit exists yet.

### 10.1 Prerequisite - Metrino.Ripple bindings for 0.5.0 (done)

1. Update the metrino package reference to 0.5.0.
2. Audit the 0.5.0 changelog for changes beyond the hub. Fix the affected bindings and code.
3. Verify the new hub surface: `part` attributes, `selectedIndex`, `selectionchanged`, `scrollToSection`, `sections`, the `snap` attribute.
4. Extend the bindings: register functions, `Html.metroHub`, `Html.metroHubSection`, and the new hub properties and events.

### 10.2 Design

- The pivot is the enclosing component. The strip row above the pivot is removed.
- The Day pivot item holds one `metro-hub`. The hub holds seven `metro-hub-section` children: one date of the visible week each.
- A section holds the content the old card view held: session card, rest line, or before-plan line.
- The chevrons sit with the hub in the Day item. They scroll the hub to the selected day.
- A hub pan settles on a day section. The app writes that date to `selectedDate`. The date block and plan line follow.
- The hub subtree is keyed on the week of `selectedDate`, never on the date. A day change is a scroll; only a week rollover rebuilds.
- The selected section header takes the accent color.
- The Week pivot item keeps the slice 4 rows. Its chevrons move `selectedDate` seven days.

### 10.3 Tasks

1. Bindings update (10.1).
2. Register hub and hub-section at boot.
3. Replace `stripRow` with the hub inside the Day pivot item.
4. Chevron handler scrolls the hub; the scroll listener writes `selectedDate`.
5. Key the hub switch on the plan week. Style the selected header.
6. Rework e2e scenarios 2, 5, and 6: section headers replace the strip letters; the chevron scrolls; a header tap selects the day.

### 10.4 Acceptance

- The pivot headers are the only always-visible day and week control.
- The Week view shows no day navigation.
- Chevron, pan, and header tap agree on one `selectedDate`.
- The section 8 gates stay green.

## 11. Status

Slices 2 and 3 are implemented and committed (29c0570). Gates were green at commit: builds clean, 24 browser tests, nine end-to-end scenarios.

Paused on 2026-09-20: metrino 0.5.0 is out. The bindings update (10.1) landed the same day in commit 6c895f2: package 0.5.0, hub bindings (snap, selectedIndex, selectionchanged, scroll helpers), and the ToastHost migration.

Slice 3b (day hub inversion) is committed (a8ca8d2): the Day pivot item holds the day hub, the strip row is gone. The section 8 gates re-ran green after the rework: builds clean, 24 browser tests, nine end-to-end scenarios (2, 5, and 6 reworked for the hub). Slices 4 and 5 are next.

Criteria revision 2026-09-20: the Today card shows the full routine of the dia, flat (no "first two" truncation, no "+ N más" line). Mesociclo, circuit grouping, rests, and guía detail move to the session detail (slice 7). The card model, locale strings, and e2e scenario 2 reflect the revision.

Slice 4 (week pivot) is committed (3260aa3): selectedDate persists through the raw state lane with a defensive boot parse; `weekRows` and `planChipState` landed in the projection with browser tests; the Week item renders the range header, chip row (chip, week chevrons, Hoy), seven plain rows, and the sessions summary; a row tap writes selectedDate and pivots to Day. External date writes go through `setSelectedDate`, which scrolls the day hub and marks the section when the week is unchanged. Gates re-ran green: builds clean, 27 browser tests, ten end-to-end scenarios.

Slice 5 (view chip) is committed (0ef52bf). The chip sits between the plan line and the pivot and reads "{Genero word} · {opcion display}" from the locale table ("Hombre · 3 días"; en: "Men · 3 days"). `App.Variants` owns the display helper ("3dias" to "3 días", raw id otherwise), the flyout group builder (one group per Bloque, items "{display} — {titulo}"), and `resolveView`; the boot fallback now rewrites the store's ViewState lane when the stored view names a missing variant. The chip's flyout opened with the `open` attribute only: the flyout node sits in the layout under the chip, and `attr.open'` (new binding members over `Base.booleanAttribute`) drove the reflected boolean. This mechanism is dead since metrino 0.5.2 (section 14). `metro-menu-flyout` registers through a dynamic import (`registerMetroMenuFlyoutDynamic` in Metrino.Ripple, raw `import()` emit) with no readiness gate: the imports fire at boot, and a tag defined after its node exists upgrades that node in place. Selection closes the flyout and goes through `State.setView` (store lane first, then the view var). Gates re-ran green: builds clean, 30 browser tests (3 new: display, groups, resolveView), thirteen end-to-end scenarios (new: chip switch and reload persistence, boot fallback rewrite seeded through raw IndexedDB, light dismiss without selection). The compiled graph holds no static import of `@angelmunoz/metrino/menu-flyout`; the dynamic import is the only reference, so the component ships as its own chunk.

Implementation notes:

- `Metrino.Ripple/Primitives.fs`: the `showToast` / `hideToast` module imports were broken against metrino 0.4.0. The d.ts declares them; the built module exports only `MetroToast` and `registerMetroToast`. They are now typed helpers over the existing `MetroToast` element (`Elements.fs`) and reproduce the metrino source's own global-instance behavior.
- `Fable.Browser.Navigator` was added for the typed `navigator.language` binding.
- Fable drops `[<Emit>]` bindings that a `.fsi` file exposes. The Intl emits live in a nested module inside `Locale.fs`, and the public surface wraps them.
- Upstream metrino 0.5.1 bug (date-picker-roller): `#handlePointerDown` calls `setPointerCapture`, which retargets the later click to the column, so `#handleClick`'s `.picker-item` target check fails - tapping an item never selects it. Drag and wheel work. The e2e drives the real drag gesture (40 px up = one day forward). A fix would clear the capture or read the item from the pointer coordinates.
- The e2e `see` helper filters matches to visible elements: the closed flyout holds hidden copies of the opcion labels, and a plain `.first()` match can pick a hidden one.

## 13. Status

Slices 2 to 5 are committed. Slice 6 (import preview and Plan page) is implemented in the working tree and awaits review: `stageImport` parses and opens the `#/import` route; nothing touches the store before the preview's commit button, which writes the import with the anchor chosen on the roller, toasts, and walks history back to Today. The Plan page carries the file title and origin, the full variant list (tap = switch view and jump to Today), the anchor row with the re-anchor roller, the `#x` extras grouped by key under `metro-expander`, the imports history (current marked, re-loadable), and an app bar with export (raw download) and remove. The interim direct-commit import is gone. Gates: builds clean, 31 browser tests (new: `extraKey`), sixteen end-to-end scenarios (new: preview commit flows everywhere, cancel stores nothing, warnings render, re-anchor re-projects). The compiled graph holds no static import of the lazily registered components.

## 12. Slice 6 - Import preview and Plan page

Design: storyboard 4.6 (preview `S5b`) and the Plan screen storyboard. Scope is full: preview flow plus the whole Plan page. The plan text (5.6) already puts the Plan page app bar items in slice 6, and no later slice claims the rest.

### 12.1 Decisions

| Decision | Reason |
|---|---|
| The preview is a full-bleed app layer, not `metro-content-dialog`. | The phone layout is the design of record (storyboard responsive table). The phone MDL form is full screen; the centered dialog is the Windows 8 desktop form. The component is a fixed centered card (`max-width: 500px`) with no part attributes, so it cannot go full bleed from the app. Slice 9 wraps the same content in the dialog for desktop. |
| The preview is the route `#/import`. Back chevron and browser back cancel. | Hub and spokes; the other chooser pages do the same. Cancel stores nothing and shows no toast. |
| The sample plan runs the same preview. | Storyboard S1: "runs the same import flow". After slice 6 no direct-commit path exists. |
| A hard parse error never opens a preview. | There is nothing to preview. The error card on Today stays. |
| Start date: a field button shows the localized date; a tap expands `metro-date-picker-roller` in place. | Option 2. The roller is the WP8 picker idiom; the preview layer is already full screen, so the roller embeds inside it. Default from `defaultAnchor`. `metro-date-picker` is not used: its native input opens a second, non-MDL picker over the roller. |
| Commit writes the import with the chosen anchor, toasts, and lands on Today. `selectedDate` stays today. | Design 4.6. A future anchor shows the quiet before-plan line; no auto-jump. |
| No progress ring. | Parsing is synchronous and the file is local; a "Reading plan…" state can never render. |
| Warnings render kind, message, and line. | `ParseWarning` carries no semana; a line-to-semana lookup does not exist. |
| Plan page: variants list, anchor row, guía, imports history, export, remove. Variants tap = `setView` + Today. Anchor tap = field + roller, re-put with new anchor. | Plan screen storyboard. Re-anchor is `putImport` with the same id; every view re-projects from the anchor. |
| Guía: split each extra on the first `|`; key verbatim, body under `metro-expander`. | `Extras` holds `#x `-stripped text (`Parser.fs:111`); keys are file content, so verbatim. |
| Remove runs `deleteImport` with no confirmation. | The file is re-importable; `deleteImport` clears the active pointer. |
| Export downloads the stored `Raw` as a file. | No component needed. |
| Lazy registrations: `date-picker-roller`, `expander`. | The flyout pattern. Neither is needed at first paint. `content-dialog` and `date-picker` are untouched in V1. |
| Imports history reads `listImports`; re-load re-puts the old import and re-parses. | The store API already supports it. |

### 12.2 Files

- Edit `src/App/State.fsi` + `State.fs`: `pendingImport` var (file name, raw, parsed, anchor), `stageImport` replaces the direct commit in `importText`, `commitImport`, `reAnchor`, `removePlan`. The toast moves to `commitImport`.
- New `src/App/Preview.fsi` + `Preview.fs`: the `#/import` route view. Field + roller for the anchor; contents list from `Variants.groups`; warnings list; commit and cancel.
- New `src/App/PlanPage.fsi` + `PlanPage.fs`: the Plan page view; `Shell.fs` drops its placeholder.
- Edit `src/Program.fs`: lazy registrations.
- Edit `src/App/Locale.fsi` + `Locale.fs`: about fifteen keys (preview title, commit, cancel, reading line, start-date label, warning line, Plan page headings, origin, export, remove, history stamp).
- Edit `tests/e2e.mjs`: scenarios 2, 3, 4 gain the preview commit step. New: cancel stores nothing; warnings render; re-anchor re-projects. The roller takes plain taps on column values, so Playwright drives it without drags.

### 12.3 Tasks

1. State: staging, commit, re-anchor, remove. Drop the interim flow.
2. Preview route view with field + roller.
3. Plan page view; wire the router.
4. Lazy registrations and locale keys.
5. Browser-lane helper for the extras key split (`Extras` text to key and body).
6. E2E updates and new scenarios.
7. Gates (section 8).

### 12.4 Acceptance

- Import always goes through the preview; cancel leaves the store untouched.
- The roller sets the anchor; commit lands on Today with the chosen anchor in the store.
- The Plan page matches the storyboard: variants, anchor, guía, history, export, remove.
- Gates green, fantomas clean, no static import of the four lazy components.

## 14. Status

Metrino 0.5.2 made `metro-menu-flyout` imperative-only: `open` is a read-only getter and the element renders nothing until `show(target)` runs. The view chip now captures the chip and the flyout with `attr.ref` and calls `show`/`hide`; `attr.open'` no longer drives it. The `MetroMenuFlyout` binding gains `hide`. Gates re-ran green: builds clean, 31 browser tests, sixteen end-to-end scenarios, `pnpm build`, fantomas.

## 15. Status

The app bar is fixed chrome: `Shell` renders it per route and measures it with a `ResizeObserver` into `--app-bar-height`. The shell's scrolling region reserves that height plus the safe-area inset, so the Plan page's imports section no longer hides behind the bar. Today, Plan, and Preview no longer own a scroller or their app bar. Gates re-ran green: builds clean, 31 browser tests, seventeen end-to-end scenarios (new: Plan imports clear the fixed app bar).

## 16. Slice 7 - Session detail

Design: storyboard 4.4 (S4). The entry point differs from the storyboard: the Today app bar's info command opens the page. The command is a Windows Phone command, not navigation. The Today card stays non-interactive, and the week row keeps its jump-to-Day behavior, so flow F3's "row tap opens the detail" is dropped.

### 16.1 Decisions

| Decision | Reason |
|---|---|
| The detail is the route `#/session`, a full-bleed page. | The content is taller than one phone viewport. The shell owns the scroll region and the back chevron. The centered `metro-content-dialog` was already rejected on phone in slice 6. |
| One info `metro-app-bar-button` on Today's bar, hidden on rest days and outside the plan range. | The app bar holds commands, never navigation. No route parameters: the page reads `selectedDate` and `view` from state, so it always shows the variant it was opened from. |
| Circuit headers take the accent; a heavier divider separates circuits; exercises inside one circuit separate by spacing alone. | Accent discipline (design section 2); a circuit's exercises belong together. |
| The exercise order renders as the `{circuito}{orden}` idiom ("A1"); the round count renders in the circuit header. | Storyboard S4; the format keeps one `Vueltas` value per circuit. |
| Guía trailer: the first `descansos` line and the first `regla-circuito` line, descansos first, "· " prefix. | One or two lines next to the circuits they describe; the full lists stay on the Plan page's guide. |
| The exercise scheme line is one definition, shared by the Today card and the detail; hoisted to `Chrome.schemeLine`. | Two surfaces, one line: reps, RIR, and rest verbatim, `-` dropped. |
| A direct `#/session` visit on a rest day or outside the range degrades to the quiet Today states. | Rest, before-plan, and completed are visible states, never errors. |
| The hub `selectionchanged` handler writes only while the hub is attached. | A reactive rebuild can dispose the hub mid-scroll; its dying settle reported a stale index and reset the selected day before the fresh hub mounted (the week-row tap regression, found when the gates re-ran). |

### 16.2 Files

- New `src/App/SessionDetail.fsi` + `SessionDetail.fs`: header (date caption, session title, plan-week badge), circuit groups, trailer, quiet states. `detailGroups` and `guideTrailer` are pure.
- Edit `src/App/Chrome.fsi` + `Chrome.fs`: `schemeLine` hoisted from Today.
- Edit `src/App/Today.fsi` + `Today.fs`: the local schemeLine is gone; `hasSelectedSession` gates the app bar command; the hub event guard.
- Edit `src/App/State.fsi` + `State.fs`: `SessionPage` with the `#/session` route.
- Edit `src/App/Shell.fsi` + `Shell.fs`: the route case (no app bar on the detail), Today bar's info command.
- Edit `src/App/Locale.fsi` + `Locale.fs`: `DetailMenu`, `CircuitHeading`, `CircuitHeadingOnly`.
- Edit `tests/e2e.mjs`: scenario 20 drives the command, the badge, both circuit headers, the A1 idiom, the verbatim scheme, both trailer lines, and back; scenario 21 pins that the command hides on rest days. Scenario 10 doubles as the regression test for the hub guard.

### 16.3 Acceptance

- The info command opens the selected day's routine; the back chevron returns to Today.
- The page shows the mesociclo badge, circuit groups with round counts and the A1 idiom, verbatim schemes with notes, and the two trailer lines.
- Rest days and the quiet states show no command.
- Gates green (section 8): builds clean, 31 browser tests, twenty-one end-to-end scenarios, `pnpm build`, fantomas clean.

## 17. Status

Slice 7 is implemented in the working tree and awaits review. It carries one bug fix found while re-running the gates: a week-row tap could lose the selected day, because a reactive rebuild disposed the day hub mid-scroll and the dying hub's last notification reset `selectedDate` before the fresh hub mounted. The hub event handler now writes only while the hub is attached. Gates: builds clean, 31 browser tests, twenty-one end-to-end scenarios (new: session detail, command hidden on rest days), `pnpm build`, fantomas clean.

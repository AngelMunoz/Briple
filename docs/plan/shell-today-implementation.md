# Implementation Plan - Shell, Projection, Today (slices 2 to 5)

> Status: plan only. No code written.
> Design: `docs/design/training-pwa-storyboards.md`, build order items 2 to 5.
> Style: Simplified Technical English (ASD-STE100). Procedural sentences: 20 words or fewer. Descriptive sentences: 25 words or fewer.

## 1. Scope

| Slice | Content |
|---|---|
| 2 | Projection: pure date to session functions |
| 3 | Shell, app bar, Today screen S1 and S2, interim import |
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
| Day strip: a row of seven `metro-button`. | list-view is vertical and virtualized. Seven fixed items need neither. |
| Week rows: plain rows, not list-view. | The rows need per-row markup: today bar, counts. list-view renders text only. |
| Pivot: tap selection. Chevrons move day and week. | The pivot has no pan handler (section 2). |
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
- Session card: accent rule on the left. Title `.header`. Badge line `.badge-text`: "6 EJERCICIOS · 2 CIRCUITOS". First two exercises with scheme lines. Fields verbatim; `-` omitted. Last line: "+ N más · M circuitos". The card is non-interactive until slice 7.
- Rest day: centered caption "Descanso · próxima sesión: {weekday}, {title}" from `nextSession`.
- Dia title fallback: when `Dia.Titulo` is None, use the locale weekday name.
- Pivot: two `metro-pivot-item`, headers "Day" and "Week". Day holds the strip and card. Week holds a placeholder until slice 4. `selectionchanged` writes `pivotIndex`. Set `selectedIndex` as a property.
- The view chip is slice 5. The plan line reads the `view` Var.

### 5.8 Tests

- ISO week and quarter helper.
- Weekday narrow letters. Strip model: dates, letters, dot flags, selected flag.
- Card model: counts, first two exercises, more count.
- Interim import: parse fixture, commit, `getActiveImport` round trip.
- String table: es and en lines.
- Smoke: page boots, app bar renders, no page errors.

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
2. `pnpm test:browser`: the 18 existing asserts plus the new suites.
3. `pnpm build`: exit 0.
4. fantomas clean on touched files.
5. Smoke: page boots, no page errors. From slice 3 on.

## 9. Risks

| Risk | Handling |
|---|---|
| `iconMap` key for the Plan icon | Verify at build. |
| Flyout light dismiss | Verify at slice 5. Fallback: explicit close. |
| ISO week math errors | Small helper, table tests. |
| Interim import survives past slice 6 | INTERIM comments. Slice 6 removes them. |

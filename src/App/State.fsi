module App.State

// App state over the local store. Reactive vars only; the DOM
// lives in Shell/Today. The parsed plan is never persisted: it is parsed from
// Raw once at boot and again on every import.

open System
open Fable.Core
open Fable.Ripple
open Browser.Types
open Plan.Types
open Plan.Parser
open Briple.Store

type Page =
  | TodayPage
  | PlanPage
  | SettingsPage
  | ImportPage

val today: unit -> DateOnly

val activeImport: Var<StoredImport option>

val parsed: Var<ParsedPlan option>

val view: Var<Genero * string>

val selectedDate: Var<DateOnly>

val pivotIndex: Var<float>

val importError: Var<string option>

/// A parsed import waiting in the preview. Nothing is in the store until
/// `commitImport` runs.
type StagedImport = {
  FileName: string
  Raw: string
  Parsed: ParsedPlan
  Anchor: DateOnly
}

val pendingImport: Var<StagedImport option>

val isValidView: plan: Plan -> genero: Genero -> opcionId: string -> bool

/// First Genero's first Opcion in file order.
val defaultView: plan: Plan -> Genero * string

/// Persists and applies a view-chip selection: writes the store's ViewState
/// lane, then updates the view var. Same anchor, same store.
val setView: genero: Genero -> opcionId: string -> JS.Promise<unit>

/// Parses a file for the preview. A hard parse error keeps Today's error
/// card; a successful parse stages the import and opens `#/import`.
val stageImport: fileName: string -> raw: string -> unit

/// Writes the staged import with the anchor chosen in the preview, toasts,
/// and returns to Today through history.
val commitImport: anchor: DateOnly -> unit

/// Re-puts the active import with a new anchor; every view re-projects.
val reAnchor: anchor: DateOnly -> unit

/// Deletes the active import; Today degrades to the empty state.
val removePlan: unit -> unit

/// Re-activates an import from history: re-put, re-parse, jump to Today.
val activateImport: import: StoredImport -> unit

/// Restores boot state. `storedDate` is the raw ISO selectedDate; a missing
/// or corrupted value falls back to today. Also starts the change
/// subscription that persists selectedDate.
val init:
  stored: StoredImport option ->
  storedView: ViewState option ->
  storedDate: string option ->
    unit

/// Hash router over the four pages; NewUrl pushes history, Jump -1 is back.
val router: Fable.Ripple.Dom.Routing.HashRouter<Page>

val goTo: page: Page -> unit

val goBack: unit -> unit

/// Loads the bundled sample plan into the preview.
val importSample: unit -> JS.Promise<unit>

val importFile: file: File -> JS.Promise<unit>

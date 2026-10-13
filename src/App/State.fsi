module App.State

// App state over the local store. Reactive vars only; the DOM
// lives in Shell/Today. Plans are never persisted parsed: every import is
// parsed from Raw once at boot and again on every import, and the queue
// resolves by date.

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
  | SessionPage

val today: unit -> DateOnly

val view: Var<Genero * string>

val selectedDate: Var<DateOnly>

val pivotIndex: Var<float>

val importError: Var<string option>

/// The shared metrino toast host. It registers its own `<metro-toast>`
/// element on the document body at the first `show`.
val toastHost: Metrino.Ripple.Primitives.ToastHost

/// A parsed import waiting in the preview. Nothing is in the store until
/// `commitImport` runs. `ChainedFrom` is the stored finish behind a chained
/// default anchor; `None` when the Monday heuristic produced the default.
type StagedImport = {
  FileName: string
  Raw: string
  Parsed: ParsedPlan
  Anchor: DateOnly
  ChainedFrom: DateOnly option
}

val pendingImport: Var<StagedImport option>

/// One queued import with its parse. The import covers
/// [anchor, anchor + 7 * Semanas).
type QueueEntry = {
  Import: StoredImport
  Parsed: ParsedPlan
}

/// Every queued import, in order of addition.
val plans: Var<QueueEntry list>

/// The entry a date renders: the plan covering it, else the next one to
/// start, else the latest finish; None only on an empty queue. A hand-moved
/// overlap resolves to the latest start, ties by newest import.
val entryFor: date: DateOnly -> QueueEntry option

/// The plan in effect today. The Plan page and the plan app bar resolve
/// against it.
val currentEntry: unit -> QueueEntry option

/// True when the queue holds at least one import.
val hasPlans: unit -> bool

/// The view a plan renders with: the stored selection when the plan carries
/// that variant, else the plan's own first variant. Read-only: the stored
/// view belongs to whichever plan the user picked it on.
val viewFor: plan: Plan -> Genero * string

val isValidView: plan: Plan -> genero: Genero -> opcionId: string -> bool

/// First Genero's first Opcion in file order.
val defaultView: plan: Plan -> Genero * string

/// Persists and applies a view-chip selection: writes the store's ViewState
/// lane, then updates the view var. Same anchor, same store.
val setView: genero: Genero -> opcionId: string -> JS.Promise<unit>

/// Restores boot state. `stored` lists every stored import; entries whose
/// Raw no longer parses drop out alone. `storedDate` is the raw ISO
/// selectedDate; a missing or corrupted value falls back to today. Also
/// starts the change subscription that persists selectedDate.
val init:
  stored: StoredImport list ->
  storedView: ViewState option ->
  storedDate: string option ->
    unit

/// Hash router over the pages; NewUrl pushes history, Jump -1 is back.
val router: Fable.Ripple.Dom.Routing.HashRouter<Page>

val goTo: page: Page -> unit

val goBack: unit -> unit

/// Parses a file for the preview. A hard parse error keeps Today's error
/// card; a successful parse stages the import with the chained default
/// anchor and opens `#/import`.
val stageImport: fileName: string -> raw: string -> unit

/// Writes the staged import with the anchor chosen in the preview, queues
/// it behind the stored plans, toasts, and returns through history.
val commitImport: anchor: DateOnly -> unit

/// Re-puts the named import with a new anchor; every view re-projects.
val updateAnchor: id: string -> anchor: DateOnly -> unit

/// Deletes the named import from the store and the queue; the jump to Today
/// happens when the queue empties.
val removeAt: id: string -> unit

/// Restores the default theme and accent and wipes the store: every import
/// and every state lane. Then toasts, and returns to Today's empty state.
val resetToDefaults: unit -> unit

/// Loads the bundled sample plan into the preview.
val importSample: unit -> JS.Promise<unit>

val importFile: file: File -> JS.Promise<unit>

module App.State

// App state over the local store. Reactive vars only; the DOM
// lives in Shell/Today. The parsed plan is never persisted: it is parsed from
// Raw once at boot and again on every import.

open System
open Fable.Core
open Fable.Ripple
open Browser.Types
open Plan.Types
open Briple.Store

type Page =
  | TodayPage
  | PlanPage
  | SettingsPage

val today: unit -> DateOnly

val activeImport: Var<StoredImport option>

val parsed: Var<ParsedPlan option>

val view: Var<Genero * string>

val selectedDate: Var<DateOnly>

val pivotIndex: Var<float>

val importError: Var<string option>

val isValidView: plan: Plan -> genero: Genero -> opcionId: string -> bool

/// First Genero's first Opcion in file order.
val defaultView: plan: Plan -> Genero * string

/// Restores boot state. `storedDate` is the raw ISO selectedDate; a missing
/// or corrupted value falls back to today. Also starts the change
/// subscription that persists selectedDate.
val init:
  stored: StoredImport option ->
  storedView: ViewState option ->
  storedDate: string option ->
    unit

/// Hash router over the three pages; NewUrl pushes history, Jump -1 is back.
val router: Fable.Ripple.Dom.Routing.HashRouter<Page>

val goTo: page: Page -> unit

val goBack: unit -> unit

val importText: fileName: string -> raw: string -> JS.Promise<unit>

/// Loads the bundled sample plan.
val importSample: unit -> JS.Promise<unit>

val importFile: file: File -> JS.Promise<unit>

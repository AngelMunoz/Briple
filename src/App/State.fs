module App.State

// App state over the local store. Reactive vars only; the DOM
// lives in Shell/Today. The parsed plan is never persisted: it is parsed from
// Raw once at boot and again on every import.

open System
open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Browser.Types
// Primitives only: the parent namespace would drag Metrino's Severity union
// in, whose Error case shadows Result's Error in the parse matches below.
open Metrino.Ripple.Primitives
open Plan.Types
open Plan.Parser
open Plan.Projection
open Briple.Store
open App.Locale
open App.Theme
open App.Variants

type Page =
  | TodayPage
  | PlanPage
  | SettingsPage
  | ImportPage
  | SessionPage

let today() : DateOnly =
  let now = DateTime.Now
  DateOnly(now.Year, now.Month, now.Day)

let view: Var<Genero * string> = Var.create(Hombre, "3dias")

let selectedDate: Var<DateOnly> = Var.create(today())

let pivotIndex: Var<float> = Var.create 0.0

let importError: Var<string option> = Var.create None

/// A parsed import waiting in the preview. The store stays untouched until
/// `commitImport` runs. `ChainedFrom` is the stored finish behind a chained
/// default anchor; `None` when the Monday heuristic produced the default.
type StagedImport = {
  FileName: string
  Raw: string
  Parsed: ParsedPlan
  Anchor: DateOnly
  ChainedFrom: DateOnly option
}

let pendingImport: Var<StagedImport option> = Var.create None

// --- Plan queue -------------------------------------------------------------
// The date-resolved timeline. An import covers [anchor, finish); the entry
// a date falls in is the plan every screen renders on that date. No timer:
// the takeover lands on the next render after the finish date passes.

type QueueEntry = {
  Import: StoredImport
  Parsed: ParsedPlan
}

let plans: Var<QueueEntry list> = Var.create []

let entryFor(date: DateOnly) : QueueEntry option =
  match plans.Value with
  | [] -> None
  | entries ->
    let finishOf entry =
      planFinish entry.Parsed.Plan entry.Import.Anchor

    let covering, others =
      entries
      |> List.partition(fun entry ->
        daysBetween entry.Import.Anchor date >= 0
        && daysBetween date (finishOf entry) > 0)

    match covering with
    | [] ->
      match
        others
        |> List.filter(fun entry -> daysBetween date entry.Import.Anchor > 0)
      with
      | [] ->
        // Nothing covers the date and nothing is queued: the latest
        // finish (every plan completed) owns the quiet state.
        others
        |> List.maxBy(fun entry -> daysBetween date (finishOf entry))
        |> Some
      | upcoming ->
        upcoming
        |> List.minBy(fun entry -> daysBetween date entry.Import.Anchor)
        |> Some
    | covering ->
      // Latest start wins a hand-moved overlap; the newest import
      // breaks a same-anchor tie.
      covering
      |> List.sortBy(fun entry ->
        (daysBetween date entry.Import.Anchor, entry.Import.ImportedAt))
      |> List.tryLast

let currentEntry() : QueueEntry option = entryFor(today())

let hasPlans() : bool = not plans.Value.IsEmpty

// Persistence subscription for selectedDate, created in `init` so its
// immediate first run cannot clobber the restored value with today's date.
let mutable datePersist: IDisposable option = None

// The state-lane key of the selected date. The boot read in Program.fs uses
// the same literal.
let selectedDateKey = "selectedDate"

// Shared metrino toast host. ToastHost registers its own <metro-toast>
// element on the document body at the first `show`.
let toastHost: ToastHost = ToastHost()

// --- Boot ------------------------------------------------------------------

let isValidView (plan: Plan) (genero: Genero) (opcionId: string) : bool =
  Variants.isValid plan genero opcionId

/// First Genero's first Opcion in file order.
let defaultView(plan: Plan) : Genero * string = Variants.firstVariant plan

/// Persists and applies a view-chip selection. Same anchor, same store: only
/// the ViewState lane and the view var change.
let setView (genero: Genero) (opcionId: string) : JS.Promise<unit> = promise {
  do! setViewState { Genero = genero; OpcionId = opcionId }
  view.Value <- (genero, opcionId)
}

/// The view a plan renders with: the stored selection when the plan carries
/// that variant, else the plan's own first variant. Read-only on purpose —
/// the stored view belongs to whichever plan the user picked it on.
let viewFor(plan: Plan) : Genero * string =
  let genero, opcionId = view.Value

  if isValidView plan genero opcionId then
    (genero, opcionId)
  else
    defaultView plan

let init
  (stored: StoredImport list)
  (storedView: ViewState option)
  (storedDate: string option)
  : unit =
  // Restore the date first, defensively: a corrupted store entry degrades
  // to today instead of failing the boot.
  selectedDate.Value <-
    storedDate
    |> Option.map(fun raw ->
      try
        Iso.toDateOnly raw
      with _ ->
        today())
    |> Option.defaultValue(today())

  plans.Value <-
    stored
    |> List.choose(fun import ->
      parsePlan import.Raw
      |> Result.toOption
      |> Option.map(fun parsedPlan -> { Import = import; Parsed = parsedPlan }))

  match currentEntry() with
  | Some entry ->
    let resolvedView, rewrite =
      Variants.resolveView entry.Parsed.Plan storedView

    view.Value <- resolvedView

    if rewrite then
      // The stored view named a missing variant: write the resolved one
      // back so the next boot reads a valid view.
      let genero, opcionId = resolvedView
      setViewState { Genero = genero; OpcionId = opcionId } |> Promise.start
  | None -> ()

  // Persist every later change. Today persists as absence (the boot
  // fallback is today, so a default date never needs a record); Var
  // notifies only on real changes, so the immediate first run either
  // deletes a lane that was not there or rewrites the value just read.
  match datePersist with
  | Some subscription -> subscription.Dispose()
  | None -> ()

  datePersist <-
    Some(
      Signal.subscribe
        (fun date ->
          if date = today() then
            deleteStateRaw selectedDateKey |> ignore
          else
            setStateRaw selectedDateKey (Iso.ofDateOnly date) |> ignore)
        selectedDate
    )

// --- Navigation (hub and spokes; system back where it exists) ---------------
// Fable.Ripple's hash router connects the address bar to a signal: NewUrl
// pushes a history entry (browser back returns to Today), Jump -1 is the
// chevron's back. Hash routes keep the local-first app serverless.

let parsePage =
  function
  | "#/plan" -> Some PlanPage
  | "#/settings" -> Some SettingsPage
  | "#/import" -> Some ImportPage
  | "#/session" -> Some SessionPage
  | "#/"
  | "" -> Some TodayPage
  | _ -> None

let pageToUrl =
  function
  | PlanPage -> "#/plan"
  | SettingsPage -> "#/settings"
  | ImportPage -> "#/import"
  | SessionPage -> "#/session"
  | TodayPage -> "#/"

let router = new Routing.HashRouter<Page>(parsePage, pageToUrl)

let goTo(page: Page) : unit =
  if router.CurrentRoute.Value <> Some page then
    router.NewUrl(page)

let goBack() : unit = router.Jump(-1)

// --- Import -----------------------------------------------------------------
// Stage, then commit. `stageImport` parses and opens the preview; nothing
// touches the store until `commitImport` writes the staged import with the
// anchor chosen there (storyboard 4.6, S5b).

let nowIso() : string =
  let now = DateTime.UtcNow
  $"{now.Year:D4}-{now.Month:D2}-{now.Day:D2}T{now.Hour:D2}:{now.Minute:D2}:{now.Second:D2}Z"

let hash32(text: string) : uint32 =
  let mutable hash = 2166136261u

  for character in text do
    hash <- (hash ^^^ uint32 character) * 16777619u

  hash

let toHex(value: uint32) : string =
  let hex = "0123456789abcdef"
  let mutable rest = value
  let characters = Array.create 8 '0'

  for index in 7..-1..0 do
    characters.[index] <- hex.[int(rest &&& 15u)]
    rest <- rest >>> 4

  System.String characters

let idFor (fileName: string) (raw: string) (stamp: float) : string =
  let stem = fileName.Split('.') |> Array.head
  $"{stem}-{toHex(hash32 raw)}-{string(int stamp)}"

// The stamp separates repeated plans: the content hash alone would fold a
// repeat into its previous queue entry.
[<Emit("Date.now()")>]
let nowMs() : float = jsNative

[<Emit("fetch($0).then((response) => response.text())")>]
let fetchText(url: string) : JS.Promise<string> = jsNative

let stageImport (fileName: string) (raw: string) : unit =
  promise {
    match parsePlan raw with
    | Error error ->
      importError.Value <-
        Some(strings().ImportFailedDetail error.Line error.Message)
    | Ok parsedPlan ->
      let plan = parsedPlan.Plan
      let genero, opcionId = defaultView plan

      // The queued start chains after every stored finish, repeats included:
      // repeating a plan is a queue entry like any other.
      let! stored = listImports()

      let finishes =
        stored
        |> List.choose(fun import ->
          parsePlan import.Raw
          |> Result.toOption
          |> Option.map(fun parsed -> planFinish parsed.Plan import.Anchor))

      let anchor =
        chainedAnchor {
          Plan = plan
          View = genero, opcionId
          Today = today()
          Finishes = finishes
        }

      let heuristic = defaultAnchor plan genero opcionId (today())

      pendingImport.Value <-
        Some {
          FileName = fileName
          Raw = raw
          Parsed = parsedPlan
          Anchor = anchor
          ChainedFrom = if sameDate anchor heuristic then None else Some anchor
        }

      goTo ImportPage
  }
  |> Promise.start

let commitImport(anchor: DateOnly) : unit =
  match pendingImport.Value with
  | None -> ()
  | Some staged ->
    let parsedPlan = staged.Parsed

    let import = {
      Id = idFor staged.FileName staged.Raw (nowMs())
      FileName = staged.FileName
      ImportedAt = nowIso()
      Anchor = anchor
      Raw = staged.Raw
    }

    promise {
      do! putImport import

      plans.Value <- plans.Value @ [ { Import = import; Parsed = parsedPlan } ]

      importError.Value <- None
      pendingImport.Value <- None

      let variants =
        parsedPlan.Plan.Bloques
        |> List.sumBy(fun bloque -> bloque.Opciones.Length)

      toastHost.show {
        title = None
        message = strings().ToastLoaded variants parsedPlan.Plan.Semanas
        severity = Some Metrino.Ripple.Success
        duration = Some 4000.0
      }
      |> ignore

      goBack()
    }
    |> Promise.catch(fun err -> importError.Value <- Some(string err))
    |> Promise.start

let updateAnchor (id: string) (anchor: DateOnly) : unit =
  plans.Value
  |> List.tryFind(fun entry -> entry.Import.Id = id)
  |> Option.iter(fun entry ->
    let updated = { entry.Import with Anchor = anchor }

    promise {
      do! putImport updated

      plans.Value <-
        plans.Value
        |> List.map(fun e ->
          if e.Import.Id = id then { e with Import = updated } else e)
    }
    |> Promise.start)

let removeAt(id: string) : unit =
  promise {
    do! deleteImport id

    plans.Value <-
      plans.Value |> List.filter(fun entry -> entry.Import.Id <> id)

    if plans.Value.IsEmpty then
      goTo TodayPage
  }
  |> Promise.start

/// Loads the bundled sample plan into the preview.
let importSample() : JS.Promise<unit> = promise {
  let! text = fetchText "./samples/plan_entrenamiento_4sem.txt"
  stageImport "plan_entrenamiento_4sem.txt" text
}

let importFile(file: File) : JS.Promise<unit> = promise {
  let! text = file.text()
  stageImport file.name text
}

// --- Reset ------------------------------------------------------------------
// The settings reset restores the theme defaults and wipes the store: every
// import and every state lane. Every lane persists its default as absence
// (theme, accent, and the date), so nothing is written back into the store
// after the clear, and the next boot reads a fresh install.

let resetToDefaults() : unit =
  Theme.setTheme SystemTheme
  Theme.setAccent Blue
  plans.Value <- []
  pendingImport.Value <- None
  importError.Value <- None
  view.Value <- (Hombre, "3dias")
  selectedDate.Value <- today()

  promise {
    do! clearAllData()

    toastHost.show {
      title = None
      message = strings().ToastReset
      severity = Some Metrino.Ripple.Success
      duration = Some 4000.0
    }
    |> ignore

    goTo TodayPage
  }
  |> Promise.catch(fun err -> importError.Value <- Some(string err))
  |> Promise.start

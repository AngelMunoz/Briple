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
open App.Variants

type Page =
  | TodayPage
  | PlanPage
  | SettingsPage
  | ImportPage

let today() : DateOnly =
  let now = DateTime.Now
  DateOnly(now.Year, now.Month, now.Day)

let activeImport: Var<StoredImport option> = Var.create None

let parsed: Var<ParsedPlan option> = Var.create None

let view: Var<Genero * string> = Var.create(Hombre, "3dias")

let selectedDate: Var<DateOnly> = Var.create(today())

let pivotIndex: Var<float> = Var.create 0.0

let importError: Var<string option> = Var.create None

/// A parsed import waiting in the preview. The store stays untouched until
/// `commitImport` runs.
type StagedImport = {
  FileName: string
  Raw: string
  Parsed: ParsedPlan
  Anchor: DateOnly
}

let pendingImport: Var<StagedImport option> = Var.create None

// Persistence subscription for selectedDate, created in `init` so its
// immediate first run cannot clobber the restored value with today's date.
let mutable datePersist: IDisposable option = None

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

let init
  (stored: StoredImport option)
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

  match stored with
  | None -> ()
  | Some import ->
    match parsePlan import.Raw with
    | Error _ ->
      // Corrupted store entry: degrade to the empty state, Raw stays intact.
      ()
    | Ok parsedPlan ->
      let plan = parsedPlan.Plan
      activeImport.Value <- Some import
      parsed.Value <- Some parsedPlan

      let resolvedView, rewrite = Variants.resolveView plan storedView
      view.Value <- resolvedView

      if rewrite then
        // The stored view named a missing variant: write the resolved one
        // back so the next boot reads a valid view.
        let genero, opcionId = resolvedView
        setViewState { Genero = genero; OpcionId = opcionId } |> Promise.start

  // Persist every later change. Var notifies only on real changes, so the
  // immediate first run rewrites the value just read back - harmless.
  match datePersist with
  | Some subscription -> subscription.Dispose()
  | None -> ()

  datePersist <-
    Some(
      Signal.subscribe
        (fun date -> setStateRaw "selectedDate" (Iso.ofDateOnly date) |> ignore)
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
  | "#/"
  | "" -> Some TodayPage
  | _ -> None

let pageToUrl =
  function
  | PlanPage -> "#/plan"
  | SettingsPage -> "#/settings"
  | ImportPage -> "#/import"
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

let idFor (fileName: string) (raw: string) : string =
  let stem = fileName.Split('.') |> Array.head
  $"{stem}-{toHex(hash32 raw)}"

[<Emit("fetch($0).then((response) => response.text())")>]
let fetchText(url: string) : JS.Promise<string> = jsNative

let stageImport (fileName: string) (raw: string) : unit =
  match parsePlan raw with
  | Error error ->
    importError.Value <-
      Some(strings().ImportFailedDetail error.Line error.Message)
  | Ok parsedPlan ->
    let plan = parsedPlan.Plan
    let genero, opcionId = defaultView plan
    let anchor = defaultAnchor plan genero opcionId (today())

    pendingImport.Value <-
      Some {
        FileName = fileName
        Raw = raw
        Parsed = parsedPlan
        Anchor = anchor
      }

    goTo ImportPage

let commitImport(anchor: DateOnly) : unit =
  match pendingImport.Value with
  | None -> ()
  | Some staged ->
    let parsedPlan = staged.Parsed
    let plan = parsedPlan.Plan
    let genero, opcionId = defaultView plan

    let import = {
      Id = idFor staged.FileName staged.Raw
      FileName = staged.FileName
      ImportedAt = nowIso()
      Anchor = anchor
      Raw = staged.Raw
    }

    promise {
      do! putImport import
      do! setViewState { Genero = genero; OpcionId = opcionId }
      activeImport.Value <- Some import
      parsed.Value <- Some parsedPlan
      view.Value <- (genero, opcionId)
      importError.Value <- None
      pendingImport.Value <- None

      let variants =
        plan.Bloques |> List.sumBy(fun bloque -> bloque.Opciones.Length)

      toastHost.show {
        title = None
        message = strings().ToastLoaded variants plan.Semanas
        severity = Some Metrino.Ripple.Success
        duration = Some 4000.0
      }
      |> ignore

      goBack()
    }
    |> Promise.catch(fun err -> importError.Value <- Some(string err))
    |> Promise.start

let reAnchor(anchor: DateOnly) : unit =
  match activeImport.Value with
  | None -> ()
  | Some import ->
    let updated = { import with Anchor = anchor }

    promise {
      do! putImport updated
      activeImport.Value <- Some updated
    }
    |> Promise.start

let removePlan() : unit =
  match activeImport.Value with
  | None -> ()
  | Some import ->
    promise {
      do! deleteImport import.Id
      activeImport.Value <- None
      parsed.Value <- None
      goTo TodayPage
    }
    |> Promise.start

let activateImport(import: StoredImport) : unit =
  match parsePlan import.Raw with
  | Error _ -> ()
  | Ok parsedPlan ->
    let plan = parsedPlan.Plan
    let genero, opcionId = defaultView plan

    promise {
      do! putImport import
      do! setViewState { Genero = genero; OpcionId = opcionId }
      activeImport.Value <- Some import
      parsed.Value <- Some parsedPlan
      view.Value <- (genero, opcionId)
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

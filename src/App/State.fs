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

type Page =
  | TodayPage
  | PlanPage
  | SettingsPage

let today() : DateOnly =
  let now = DateTime.Now
  DateOnly(now.Year, now.Month, now.Day)

let activeImport: Var<StoredImport option> = Var.create None

let parsed: Var<ParsedPlan option> = Var.create None

let view: Var<Genero * string> = Var.create(Hombre, "3dias")

let selectedDate: Var<DateOnly> = Var.create(today())

let pivotIndex: Var<float> = Var.create 0.0

let importError: Var<string option> = Var.create None

// --- Boot ------------------------------------------------------------------

let isValidView (plan: Plan) (genero: Genero) (opcionId: string) : bool =
  plan.Bloques
  |> List.exists(fun bloque ->
    bloque.Genero = genero
    && bloque.Opciones |> List.exists(fun opcion -> opcion.Id = opcionId))

/// First Genero's first Opcion in file order.
let defaultView(plan: Plan) : Genero * string =
  plan.Bloques
  |> List.tryHead
  |> Option.bind(fun bloque ->
    bloque.Opciones
    |> List.tryHead
    |> Option.map(fun opcion -> (bloque.Genero, opcion.Id)))
  |> Option.defaultValue(Hombre, "3dias")

let init (stored: StoredImport option) (storedView: ViewState option) : unit =
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

      let resolvedView =
        storedView
        |> Option.filter(fun candidate ->
          isValidView plan candidate.Genero candidate.OpcionId)
        |> Option.map(fun candidate -> (candidate.Genero, candidate.OpcionId))
        |> Option.defaultWith(fun () -> defaultView plan)

      view.Value <- resolvedView

// --- Navigation (hub and spokes; system back where it exists) ---------------
// Fable.Ripple's hash router connects the address bar to a signal: NewUrl
// pushes a history entry (browser back returns to Today), Jump -1 is the
// chevron's back. Hash routes keep the local-first app serverless.

let parsePage =
  function
  | "#/plan" -> Some PlanPage
  | "#/settings" -> Some SettingsPage
  | "#/"
  | "" -> Some TodayPage
  | _ -> None

let pageToUrl =
  function
  | PlanPage -> "#/plan"
  | SettingsPage -> "#/settings"
  | TodayPage -> "#/"

let router = new Routing.HashRouter<Page>(parsePage, pageToUrl)

let goTo(page: Page) : unit =
  if router.CurrentRoute.Value <> Some page then
    router.NewUrl(page)

let goBack() : unit = router.Jump(-1)

// --- Import -----------------------------------------------------------------
// Direct path: parse, store under the default anchor, toast.

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

let importText (fileName: string) (raw: string) : JS.Promise<unit> =
  match parsePlan raw with
  | Error error ->
    importError.Value <-
      Some(strings().ImportFailedDetail error.Line error.Message)

    Promise.lift()
  | Ok parsedPlan ->
    promise {
      let plan = parsedPlan.Plan
      let genero, opcionId = defaultView plan
      let anchor = defaultAnchor plan genero opcionId (today())

      let import = {
        Id = idFor fileName raw
        FileName = fileName
        ImportedAt = nowIso()
        Anchor = anchor
        Raw = raw
      }

      do! putImport import
      do! setViewState { Genero = genero; OpcionId = opcionId }
      activeImport.Value <- Some import
      parsed.Value <- Some parsedPlan
      view.Value <- (genero, opcionId)
      importError.Value <- None

      let variants =
        plan.Bloques |> List.sumBy(fun bloque -> bloque.Opciones.Length)

      showToast {
        title = None
        message = strings().ToastLoaded variants plan.Semanas
        severity = Some Metrino.Ripple.Success
        duration = Some 4000.0
      }
      |> ignore
    }
    |> Promise.catch(fun err -> importError.Value <- Some(string err))

/// Loads the bundled sample plan.
let importSample() : JS.Promise<unit> = promise {
  let! text = fetchText "./samples/plan_entrenamiento_4sem.txt"
  return! importText "plan_entrenamiento_4sem.txt" text
}

let importFile(file: File) : JS.Promise<unit> = promise {
  let! text = file.text()
  return! importText file.name text
}

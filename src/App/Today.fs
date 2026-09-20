module App.Today

// Today screen: date block, empty state with the import paths, and the
// Day pivot. The pivot is the enclosing component; its Day item holds the
// day hub - one section per day of the week - and its chevrons. The app bar
// holds commands only: its ⋯ menu reaches the Plan and Settings pages.


open System
open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Browser.Types
open Plan.Types
open Plan.Projection
open App.Locale
open App.State

// --- Models --------------------------------------

let weekDates(selected: DateOnly) : DateOnly list =
  let monday = mondayOf selected
  [ for offset in 0..6 -> addDays monday offset ]

type CardExercise = { Name: string; Scheme: string option }

type CardModel = {
  Title: string
  Badge: string
  Exercises: CardExercise list
}

let schemeLine(exercise: Ejercicio) : string option =
  [ exercise.Reps; exercise.Rir; exercise.Descanso ]
  |> List.filter(fun part -> part <> "-")
  |> String.concat " · "
  |> fun line -> if line = "" then None else Some line

// The card shows the full routine of the dia, flat: every exercise with its
// scheme. Mesociclo, circuit grouping, and guía detail live in slice 7.
let cardModel
  (dia: Dia)
  (weekdayName: string)
  (badge: int -> int -> string)
  : CardModel =
  let exercises = dia.Ejercicios

  let circuits =
    exercises |> List.map(fun e -> e.Circuito) |> List.distinct |> List.length

  {
    Title = dia.Titulo |> Option.defaultValue weekdayName
    Badge = badge exercises.Length circuits
    Exercises =
      exercises
      |> List.map(fun e -> {
        Name = e.Nombre
        Scheme = schemeLine e
      })
  }

// --- View -------------------------------------------------------------------

let mutable fileInput: HTMLInputElement = Unchecked.defaultof<_>

let firstFile(input: HTMLInputElement) : File option =
  if input.files.length > 0 then
    Some input.files.[0]
  else
    None

let dateBlock() =
  Html.div [
    attr.style "padding:16px 16px 0"
    Html.div [
      attr.className "caption"
      attr.style "text-transform:uppercase;opacity:0.6"
      Html.text(fun () ->
        let dt = toDateTime selectedDate.Value
        $"{weekdayLong(locale(), dt)} · {dayMonth(locale(), dt)}")
    ]
    Html.div [
      attr.className "display"
      Html.text(fun () -> string selectedDate.Value.Day)
    ]
    Html.div [
      attr.className "body"
      attr.style "opacity:0.6"
      Html.text(fun () ->
        let (_, week) = isoWeek selectedDate.Value
        (strings()).WeekQuarter week (quarter selectedDate.Value))
    ]
    Html.div [
      attr.style "height:1px;margin:16px;background:currentColor;opacity:0.15"
    ]
  ]

let emptyState() =
  Html.div [
    attr.style "padding:16px;display:flex;flex-direction:column;gap:12px"
    Html.p [
      attr.className "body"
      attr.style "margin:0"
      Html.text (strings()).EmptyTitle
    ]
    Html.p [
      attr.className "body"
      attr.style "opacity:0.6;margin:0"
      Html.text (strings()).EmptyBody
    ]
    Html.input [
      attr.custom("type", "file")
      attr.custom("accept", ".txt,text/plain")
      attr.style "display:none"
      attr.ref(fun el -> fileInput <- el :?> HTMLInputElement)
      on.event(
        "change",
        fun _ ->
          match firstFile fileInput with
          | Some file -> importFile file |> ignore
          | None -> ()
      )
    ]
    Html.metroButton [
      on.click(fun _ -> fileInput.click())
      Html.text (strings()).LoadPlan
    ]
    Html.metroButton [
      on.click(fun _ -> importSample() |> ignore)
      Html.text (strings()).TrySample
    ]
    Html.show(
      (fun () -> importError.Value.IsSome),
      fun () ->
        Html.div [
          attr.style
            "border-left:3px solid var(--metro-accent);padding:8px 12px"
          Html.div [ attr.className "body"; Html.text (strings()).ImportFailed ]
          Html.div [
            attr.className "caption"
            attr.style "opacity:0.7"
            Html.text(importError.Value |> Option.defaultValue "")
          ]
        ]
    )
  ]

let planLineText() =
  match parsed.Value, activeImport.Value with
  | Some parsedPlan, Some import ->
    let plan = parsedPlan.Plan
    let s = strings()

    match planWeek plan import.Anchor selectedDate.Value with
    | Some week ->
      match tryMesocicloRir plan week with
      | Some rir -> $"{s.PlanWeek week plan.Semanas} · RIR {rir}"
      | None -> s.PlanWeek week plan.Semanas
    | None ->
      if daysBetween import.Anchor selectedDate.Value < 0 then
        s.BeforePlan
      else
        s.PlanDone
  | _ -> ""

let planLine() =
  Html.div [
    attr.className "body"
    attr.style "opacity:0.6;padding:0 16px"
    Html.text planLineText
  ]

let cardView (date: DateOnly) (dia: Dia) =
  let s = strings()
  let weekday = weekdayLong(locale(), toDateTime date)
  let model = cardModel dia weekday s.ExercisesBadge

  Html.div [
    attr.style
      "border-left:3px solid var(--metro-accent);padding:8px 0 8px 12px;margin:8px 16px"
    Html.div [ attr.className "header"; Html.text model.Title ]
    Html.div [
      attr.className "badge-text"
      attr.style "opacity:0.8;margin-top:4px"
      Html.text model.Badge
    ]

    yield!
      model.Exercises
      |> List.collect(fun exercise -> [
        Html.div [
          attr.style "margin-top:10px"
          Html.div [ attr.className "body"; Html.text exercise.Name ]
          yield!
            (match exercise.Scheme with
             | Some scheme -> [
                 Html.div [
                   attr.className "caption"
                   attr.style "opacity:0.65"
                   Html.text scheme
                 ]
               ]
             | None -> [])
        ]
      ])
  ]

let quietLine(text: string) =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.6;text-align:center;padding:16px"
    Html.text text
  ]

let dayContent
  (plan: Plan option)
  (anchor: DateOnly option)
  (viewValue: Genero * string)
  (date: DateOnly)
  =
  match plan, anchor with
  | Some plan, Some anchor ->
    let (genero, opcionId) = viewValue
    let s = strings()

    match trySession plan genero opcionId anchor date with
    | Some dia -> cardView date dia
    | None ->
      if daysBetween anchor date < 0 then
        quietLine s.BeforePlan
      else
        match nextSession plan genero opcionId anchor date with
        | Some(nextDate, nextDia) ->
          let weekday = weekdayLong(locale(), toDateTime nextDate)
          let title = nextDia.Titulo |> Option.defaultValue weekday
          quietLine(s.RestNext weekday title)
        | None -> quietLine s.PlanDone
  | _ -> Html.none

// --- Day hub ----------------------------------------------------------------

// The raw emit lives in a nested module the signature file does not declare:
// Fable drops [<Emit>] bindings that a signature file exposes (see Locale.fs).
module private Dom =
  [<Emit("window.requestAnimationFrame($0)")>]
  let requestAnimationFrame(callback: unit -> unit) : unit = jsNative

// The metrino hub surface this screen drives (metrino 0.5). The hub owns no
// selection state: chevrons and the mount scroll call `scrollToSection`, and
// a pan reports the settled section back through `selectionchanged`.
[<AllowNullLiteral>]
type HubElement =
  inherit HTMLElement
  abstract selectedIndex: float
  abstract sections: HTMLElement array
  abstract scrollToSection: index: float * behavior: string -> unit

let mutable dayHub: HubElement = Unchecked.defaultof<_>

let dayIndexOf(weekStart: DateOnly) =
  daysBetween weekStart selectedDate.Value

let markSelectedSection(index: int) =
  if not(isNull dayHub) then
    dayHub.sections
    |> Array.iteri(fun i section ->
      if i = index then
        section.setAttribute("selected", "")
      else
        section.removeAttribute("selected"))

let chevronButton (direction: string) (step: int) =
  Html.button [
    attr.className "day-chevron"
    attr.style "min-height:34px"
    on.click(fun _ ->
      if not(isNull dayHub) then
        let target = int dayHub.selectedIndex + step

        if target < 0 || target > 6 then
          // Week rollover: the rebuild keyed on the new week opens on the day.
          selectedDate.Value <- addDays selectedDate.Value step
        else
          dayHub.scrollToSection(float target, "smooth"))
    Html.metroIcon [ attr.icon direction ]
  ]

let daySection
  (plan: Plan option)
  (anchor: DateOnly option)
  (viewValue: Genero * string)
  (weekStart: DateOnly)
  (index: int, date: DateOnly)
  =
  Html.metroHubSection [
    attr.header(weekdayShort(locale(), toDateTime date))
    yield!
      (if index = dayIndexOf weekStart then
         [ attr.custom("selected", "") ]
       else
         [])
    on.click(fun _ ->
      // Tap a peeking section to bring its day into view. Taps on the
      // in-view section stay free for the session card (slice 7).
      if not(isNull dayHub) && int dayHub.selectedIndex <> index then
        dayHub.scrollToSection(float index, "smooth"))
    dayContent plan anchor viewValue date
  ]

// The hub subtree is keyed on the week of the selected date, never on the
// date itself: a day change is a scroll, only a week rollover rebuilds.
let dayHubView
  (plan: Plan option)
  (anchor: DateOnly option)
  (viewValue: Genero * string)
  (weekStart: DateOnly)
  =
  let dates = weekDates weekStart

  Html.div [
    attr.style "display:flex;flex-direction:column;gap:4px"
    Html.div [
      attr.style "display:flex;justify-content:center;gap:16px;padding:4px 0"
      chevronButton "back" -1
      chevronButton "forward" 1
    ]
    Html.metroHub [
      attr.snap true
      attr.ref(fun el ->
        let hub = unbox<HubElement> el
        dayHub <- hub
        // Open on the selected day of the week; instant, once laid out.
        Dom.requestAnimationFrame(fun _ ->
          hub.scrollToSection(float(dayIndexOf weekStart), "auto")))
      on.hubSelectionChanged(fun detail ->
        selectedDate.Value <- addDays weekStart detail.selectedIndex
        markSelectedSection detail.selectedIndex)
      yield!
        dates
        |> List.indexed
        |> List.map(fun (index, date) ->
          daySection plan anchor viewValue weekStart (index, date))
    ]
  ]

let pivot() =
  Html.metroPivot [
    attr.selectedIndex pivotIndex
    on.pivotSelectionChanged(fun detail ->
      pivotIndex.Value <- float detail.selectedIndex)
    Html.metroPivotItem [
      attr.header "Day"
      Html.switchWith(
        (fun () ->
          (parsed.Value |> Option.map(fun parsedPlan -> parsedPlan.Plan),
           activeImport.Value |> Option.map(fun import -> import.Anchor),
           view.Value,
           mondayOf selectedDate.Value)),
        fun (plan, anchor, viewValue, weekStart) ->
          dayHubView plan anchor viewValue weekStart
      )
    ]
    Html.metroPivotItem [
      attr.header "Week"
      Html.p [
        attr.className "body"
        attr.style "opacity:0.5;padding:16px"
        Html.text "…"
      ]
    ]
  ]

let appBar() =
  Html.metroAppBar [
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "calendar"
      attr.label (strings()).PlanMenu
      on.click(fun _ -> goTo PlanPage)
    ]
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "settings"
      attr.label (strings()).SettingsMenu
      on.click(fun _ -> goTo SettingsPage)
    ]
  ]

let view() =
  Html.div [
    attr.style "display:flex;flex-direction:column;height:100%"
    Html.div [
      attr.style "flex:1 1 auto;min-height:0;overflow-y:auto"
      dateBlock()
      Html.show((fun () -> activeImport.Value.IsNone), emptyState)
      Html.show(
        (fun () -> activeImport.Value.IsSome),
        fun () ->
          Html.div [
            attr.style "display:flex;flex-direction:column;gap:8px"
            planLine()
            pivot()
          ]
      )
    ]
    appBar()
  ]

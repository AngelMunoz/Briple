module App.Today

// Today screen : date block, empty state with
// the import paths, and the Day pivot holding the day strip and the
// session card.
// session card. The app bar holds
// commands only: its ⋯ menu reaches the Plan and Settings pages.


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

type StripDay = {
  Date: DateOnly
  Letter: string
  IsSession: bool
  IsSelected: bool
}

let weekDates(selected: DateOnly) : DateOnly list =
  let monday = mondayOf selected
  [ for offset in 0..6 -> addDays monday offset ]

let stripModel
  (plan: Plan option)
  (genero: Genero, opcionId: string)
  (anchor: DateOnly option)
  (selected: DateOnly)
  (letters: string list)
  : StripDay list =
  List.zip letters (weekDates selected)
  |> List.map(fun (letter, date) -> {
    Date = date
    Letter = letter
    IsSession =
      match plan, anchor with
      | Some p, Some a -> trainsOn p genero opcionId a date
      | _ -> false
    IsSelected = sameDate date selected
  })

type CardExercise = { Name: string; Scheme: string option }

type CardModel = {
  Title: string
  Badge: string
  Exercises: CardExercise list
  More: (int * int) option
}

let schemeLine(exercise: Ejercicio) : string option =
  [ exercise.Reps; exercise.Rir; exercise.Descanso ]
  |> List.filter(fun part -> part <> "-")
  |> String.concat " · "
  |> fun line -> if line = "" then None else Some line

let cardModel
  (dia: Dia)
  (weekdayName: string)
  (badge: int -> int -> string)
  : CardModel =
  let exercises = dia.Ejercicios

  let circuits =
    exercises |> List.map(fun e -> e.Circuito) |> List.distinct |> List.length

  let visible =
    exercises
    |> List.truncate 2
    |> List.map(fun e -> {
      Name = e.Nombre
      Scheme = schemeLine e
    })

  {
    Title = dia.Titulo |> Option.defaultValue weekdayName
    Badge = badge exercises.Length circuits
    Exercises = visible
    More =
      let remaining = exercises.Length - visible.Length
      if remaining > 0 then Some(remaining, circuits) else None
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

let dotStyle(isSession: bool) =
  if isSession then
    "width:6px;height:6px;border-radius:50%;background:var(--metro-accent)"
  else
    "width:6px;height:6px;border-radius:50%;background:transparent"

let stripButton(day: StripDay) =
  Html.metroButton [
    attr.className "strip-day"
    attr.style
      "min-width:34px;min-height:44px;display:flex;flex-direction:column;align-items:center;gap:2px;padding:4px"
    yield! (if day.IsSelected then [ attr.accent Blue ] else [])
    on.click(fun _ -> selectedDate.Value <- day.Date)
    Html.span [ attr.className "caption"; Html.text day.Letter ]
    Html.span [ attr.style(dotStyle day.IsSession) ]
  ]

let chevron (direction: string) (move: int) =
  Html.metroButton [
    attr.className "strip-day"
    attr.style "min-width:34px;min-height:34px"
    on.click(fun _ -> selectedDate.Value <- addDays selectedDate.Value move)
    Html.metroIcon [ attr.icon direction ]
  ]

let stripRow() =
  Html.div [
    attr.style "display:flex;align-items:center;gap:4px;padding:0 8px"
    chevron "back" -1
    Html.switchWith(
      (fun () ->
        let plan =
          parsed.Value |> Option.map(fun parsedPlan -> parsedPlan.Plan)

        let anchor =
          activeImport.Value |> Option.map(fun import -> import.Anchor)

        let (genero, opcionId) = view.Value

        let letters =
          weekDates selectedDate.Value
          |> List.map(fun date -> weekdayNarrow(locale(), toDateTime date))

        stripModel plan (genero, opcionId) anchor selectedDate.Value letters),
      fun days ->
        Html.div [
          attr.style "display:flex;flex:1;justify-content:space-between;gap:2px"
          yield! days |> List.map stripButton
        ]
    )
    chevron "forward" 1
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

    yield!
      (match model.More with
       | Some(remaining, circuits) -> [
           Html.div [
             attr.className "caption"
             attr.style "opacity:0.65;margin-top:10px"
             Html.text(s.MoreExercises remaining circuits)
           ]
         ]
       | None -> [])
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
           selectedDate.Value)),
        fun (plan, anchor, viewValue, date) ->
          dayContent plan anchor viewValue date
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
            stripRow()
            pivot()
          ]
      )
    ]
    appBar()
  ]

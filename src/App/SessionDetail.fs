module App.SessionDetail

open System
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Plan.Types
open Plan.Projection
open App.Locale
open App.Variants
open App.Chrome
open App.State

type DetailExercise = {
  Idiom: string
  Name: string
  Scheme: string option
  Notes: string option
}

type CircuitGroup = {
  Circuito: string
  Vueltas: string
  Exercises: DetailExercise list
}

let detailGroups(dia: Dia) : CircuitGroup list =
  dia.Ejercicios
  |> List.groupBy(fun exercise -> exercise.Circuito)
  |> List.map(fun (circuito, exercises) -> {
    Circuito = circuito
    Vueltas = (List.head exercises).Vueltas
    Exercises =
      exercises
      |> List.map(fun exercise -> {
        Idiom = $"{circuito}{exercise.Orden}"
        Name = exercise.Nombre
        Scheme = schemeLine exercise
        Notes =
          exercise.Notas
          |> fun notes ->
            if notes = "" || notes = "-" then None else Some notes
      })
  })

let guideTrailer(plan: Plan) : string list =
  plan.Extras
  |> List.map extraKey
  |> List.filter(fun (key, _) -> key = "descansos" || key = "regla-circuito")
  |> List.sortBy(fun (key, _) -> if key = "descansos" then 0 else 1)
  |> List.distinctBy(fun (key, _) -> key)
  |> List.map snd

// --- View -------------------------------------------------------------------

let inline planBadge (s: Strings) (plan: Plan) (week: int) : string =
  match tryMesocicloRir plan week with
  | Some rir -> $"{s.PlanWeek week plan.Semanas} · RIR {rir}"
  | None -> s.PlanWeek week plan.Semanas

let inline header
  (s: Strings)
  (date: DateOnly)
  (title: string option)
  (badge: string option)
  : DomItem =
  let dt = toDateTime date

  Html.header [
    attr.style "display:flex;align-items:center;padding:16px 16px 0"
    Html.metroHyperlinkButton [
      on.click(fun _ -> goBack())
      Html.metroIcon [
        attr.icon "back"
        attr.size XLarge
        attr.style "margin-right:8px"
      ]
      Html.div [
        Html.div [
          attr.className "caption"
          attr.style "text-transform:uppercase;opacity:0.6"
          Html.text($"{weekdayLong(locale(), dt)} · {dayMonth(locale(), dt)}")
        ]
        yield!
          match title with
          | Some title -> [
              Html.div [ attr.className "subheader"; Html.text title ]
            ]
          | None -> []
        yield!
          match badge with
          | Some badge -> [
              Html.div [
                attr.className "badge-text"
                attr.style "opacity:0.7;margin-top:2px"
                Html.text badge
              ]
            ]
          | None -> []
      ]
    ]
  ]

let circuitHeader (s: Strings) (group: CircuitGroup) : DomItem =
  let text =
    if group.Vueltas = "" || group.Vueltas = "-" then
      s.CircuitHeadingOnly group.Circuito
    else
      s.CircuitHeading group.Circuito group.Vueltas

  // Accent discipline: circuit headers are one of the accent surfaces.
  Html.div [
    attr.className "badge-text"
    attr.style "color:var(--metro-accent);margin:16px 0 4px"
    Html.text text
  ]

let inline exerciseRow(model: DetailExercise) : DomItem =
  Html.div [
    attr.style "padding:6px 0"
    Html.div [
      attr.style "display:flex;gap:8px;align-items:baseline"
      Html.span [
        attr.className "badge-text"
        attr.style "opacity:0.7;min-width:24px"
        Html.text model.Idiom
      ]
      Html.div [ attr.className "body"; Html.text model.Name ]
    ]
    yield!
      match model.Scheme with
      | Some scheme -> [
          Html.div [
            attr.className "caption"
            attr.style "opacity:0.65;margin-left:32px"
            Html.text scheme
          ]
        ]
      | None -> []
    yield!
      match model.Notes with
      | Some notes -> [
          Html.div [
            attr.className "caption"
            attr.style "opacity:0.6;margin-left:32px"
            Html.text notes
          ]
        ]
      | None -> []
  ]

let inline groupDivider() : DomItem =
  Html.div [
    attr.style "height:1px;background:currentColor;opacity:0.15;margin-top:8px"
  ]

let inline circuitSections (s: Strings) (dia: Dia) : DomItem list =
  detailGroups dia
  |> List.indexed
  |> List.collect(fun (index, group) -> [
    if index > 0 then
      groupDivider()
    circuitHeader s group
    yield! group.Exercises |> List.map exerciseRow
  ])

let inline trailerLines(plan: Plan) : DomItem list =
  guideTrailer plan
  |> List.map(fun line ->
    Html.div [
      attr.className "caption"
      attr.style "opacity:0.7;margin-top:8px"
      Html.text $"· {line}"
    ])

let inline quietLine(text: string) : DomItem =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.6;text-align:center;padding:16px"
    Html.text text
  ]

let inline content
  (s: Strings)
  (plan: Plan)
  (anchor: DateOnly)
  (viewValue: Genero * string)
  (date: DateOnly)
  : DomItem =
  let (genero, opcionId) = viewValue

  match trySession plan genero opcionId anchor date with
  | Some dia ->
    let dt = toDateTime date
    let weekday = weekdayLong(locale(), dt)
    let title = dia.Titulo |> Option.defaultValue weekday

    Html.div [
      attr.style "display:flex;flex-direction:column;padding:0 16px 16px"
      header
        s
        date
        (Some title)
        (planWeek plan anchor date |> Option.map(planBadge s plan))
      yield! circuitSections s dia
      yield! trailerLines plan
    ]
  | None ->
    // Only reachable through a direct URL on a rest day or outside the
    // plan range: degrade quietly, like Today does.
    let quiet =
      if daysBetween anchor date < 0 then
        s.BeforePlan
      else
        match nextSession plan genero opcionId anchor date with
        | Some(nextDate, nextDia) ->
          let weekday = weekdayLong(locale(), toDateTime nextDate)
          let title = nextDia.Titulo |> Option.defaultValue weekday
          s.RestNext weekday title
        | None -> s.PlanDone

    Html.div [ header s date None None; quietLine quiet ]

let view() : DomItem =
  let s = strings()

  Html.div [
    Html.show(
      (fun () -> parsed.Value.IsSome && activeImport.Value.IsSome),
      fun () ->
        let parsedPlan = parsed.Value |> Option.get
        let import = activeImport.Value |> Option.get
        content s parsedPlan.Plan import.Anchor view.Value selectedDate.Value
    )
  ]

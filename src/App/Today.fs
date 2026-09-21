module App.Today

// Today screen: date block, empty state with the import paths, and the
// Day pivot. The pivot is the enclosing component; its Day item holds the
// day hub - one section per day of the week - and its chevrons. The shell
// owns the app bar.


open System
open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Browser.Types
open Plan.Types
open Plan.Projection
open App.Locale
open App.Variants
open App.Chrome
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
// scheme. Mesociclo, circuit grouping, and guía detail belong to the
// session detail view.
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

let inline dateBlock() =
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

let inline emptyState() =
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
        Chrome.noticeCard (strings()).ImportFailed [
          importError.Value |> Option.defaultValue ""
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

let inline planLine() =
  Html.div [
    attr.className "body"
    attr.style "opacity:0.6;padding:0 16px"
    Html.text planLineText
  ]

// --- View chip -----------------------------------------------------------
// The chip is the only place Genero/Opcion appear on Today. The flyout is
// imperative in metrino 0.5.2: `open` is a read-only getter, so the chip
// calls `show()` on the captured element with itself as the anchor. The
// backdrop and light dismiss belong to the component.

let mutable viewChipButton: HTMLElement = Unchecked.defaultof<_>
let mutable viewMenu: MetroMenuFlyout = Unchecked.defaultof<_>

let openViewMenu() =
  if not(isNull viewMenu) && not(isNull viewChipButton) then
    viewMenu.show viewChipButton

let hideViewMenu() =
  if not(isNull viewMenu) then
    viewMenu.hide()

let chipText() =
  let genero, opcionId = view.Value
  let s = strings()
  $"{s.GeneroWord genero} · {Variants.display s.DiasUnit opcionId}"

let inline viewChip() =
  Html.metroButton [
    attr.className "view-chip"
    attr.style "min-height:34px;color:var(--metro-accent)"
    attr.ref(fun el -> viewChipButton <- el)
    on.click(fun _ -> openViewMenu())
    Html.span [ attr.className "badge-text"; Html.text chipText ]
    Html.metroIcon [ attr.icon "chevron-down" ]
  ]

let inline flyoutItem (genero: Genero) (item: VariantItem) =
  let isActive() = view.Value = (genero, item.Id)

  Html.div [
    attr.className "menu-item"
    attr.style(fun () -> if isActive() then "color:var(--metro-accent)" else "")
    on.click(fun _ ->
      hideViewMenu()
      setView genero item.Id |> ignore)
    Html.show(isActive, fun () -> Html.metroIcon [ attr.icon "check" ])
    Html.span [ Html.text item.Label ]
  ]

let inline viewFlyout() =
  let s = strings()

  let blocks =
    match parsed.Value with
    | Some parsedPlan -> Variants.groups parsedPlan.Plan s.DiasUnit
    | None -> []

  let rows =
    blocks
    |> List.indexed
    |> List.collect(fun (index, group) ->
      let header =
        Html.div [
          attr.className "badge-text"
          attr.style "opacity:0.6;padding:8px 16px 0"
          Html.text(s.GeneroWord group.Genero)
        ]

      let items = group.Items |> List.map(flyoutItem group.Genero)

      if index = 0 then
        header :: items
      else
        Html.div [ attr.className "menu-divider" ] :: header :: items)

  Html.metroMenuFlyout [
    attr.ref(fun el -> viewMenu <- el :?> MetroMenuFlyout)
    yield! rows
  ]

let inline viewChipArea() =
  Html.div [
    attr.style
      "display:flex;flex-direction:column;align-items:flex-start;padding:0 16px"
    viewChip()
    viewFlyout()
  ]

let inline cardView (date: DateOnly) (dia: Dia) =
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

let inline quietLine(text: string) =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.6;text-align:center;padding:16px"
    Html.text text
  ]

let inline dayContent
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
module Dom =
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

// The week the current hub was built for; set together with `dayHub`.
let mutable dayHubWeek: DateOnly = Unchecked.defaultof<_>

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

// Every selectedDate write from outside the hub goes through here. The hub
// is keyed on the week, so a same-week change is a scroll plus a header
// mark; a week change leaves the work to the rebuild's mount scroll.
let setSelectedDate(date: DateOnly) : unit =
  selectedDate.Value <- date

  if not(isNull dayHub) && dayHubWeek = mondayOf date then
    let index = daysBetween dayHubWeek date
    dayHub.scrollToSection(float index, "smooth")
    markSelectedSection index

let inline chevronButton (direction: string) (step: int) =
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

let inline daySection
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
      // in-view section stay free for the session card.
      if not(isNull dayHub) && int dayHub.selectedIndex <> index then
        dayHub.scrollToSection(float index, "smooth"))
    dayContent plan anchor viewValue date
  ]

// The hub subtree is keyed on the week of the selected date, never on the
// date itself: a day change is a scroll, only a week rollover rebuilds.
let inline dayHubView
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
        dayHubWeek <- weekStart
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

// --- Week pivot -------------------------------------------------------------

// "14 – 20 SEPTEMBER"; the month is named once when both ends share it.
let weekRangeText(weekStart: DateOnly) =
  let weekEnd = addDays weekStart 6
  let l = locale()
  let d1 = dayNumber(l, toDateTime weekStart)
  let d2 = dayNumber(l, toDateTime weekEnd)
  let m1 = monthLong(l, toDateTime weekStart)
  let m2 = monthLong(l, toDateTime weekEnd)

  if m1 = m2 then
    $"{d1} – {d2} {m1.ToUpper()}"
  else
    $"{d1} {m1.ToUpper()} – {d2} {m2.ToUpper()}"

let weekChipText(date: DateOnly) =
  match parsed.Value, activeImport.Value with
  | Some parsedPlan, Some import ->
    let s = strings()

    match planChipState parsedPlan.Plan import.Anchor date with
    | Inside(week, total) -> s.PlanWeek week total
    | StartsOn start -> s.StartsOn(dayMonth(locale(), toDateTime start))
    | Completed -> s.PlanDone
  | _ -> ""

let inline weekRowView(row: WeekRow) =
  let s = strings()
  let dt = toDateTime row.Date
  let weekday = weekdayShort(locale(), dt)

  let title, count =
    match row.Session with
    | Some dia ->
      let fallback = weekdayLong(locale(), dt)
      dia.Titulo |> Option.defaultValue fallback, Some dia.Ejercicios.Length
    | None -> s.RestRow, None

  Html.button [
    attr.className(if row.IsToday then "week-row today" else "week-row")
    on.click(fun _ ->
      setSelectedDate row.Date
      pivotIndex.Value <- 0.0)
    Html.span [
      attr.className "caption"
      attr.style "opacity:0.7;width:24px;text-align:right"
      Html.text(dayNumber(locale(), dt))
    ]
    Html.span [
      attr.className "caption"
      attr.style "opacity:0.7;width:36px;text-align:left"
      Html.text weekday
    ]
    Html.span [
      attr.className "body"
      attr.style(
        if row.Session.IsSome then
          "flex:1;text-align:left"
        else
          "flex:1;text-align:left;opacity:0.55"
      )
      Html.text(if row.Session.IsSome then title else $"· {title}")
    ]
    yield!
      match count with
      | Some n -> [
          Html.span [
            attr.className "caption"
            attr.style "opacity:0.6"
            Html.text(s.ExerciseCount n)
          ]
        ]
      | None -> []
  ]

// Keyed on the week like the day hub: the chip, rows, and summary are
// week-scoped, so day changes within the week never rebuild them.
let inline weekView
  (plan: Plan option)
  (anchor: DateOnly option)
  (viewValue: Genero * string)
  (weekStart: DateOnly)
  =
  plan
  |> Option.bind(fun plan ->
    match anchor with
    | Some anchor -> Some(plan, anchor)
    | None -> None)
  |> Option.map(fun (plan, anchor) ->
    let genero, opcionId = viewValue
    let s = strings()
    let rows = weekRows plan genero opcionId anchor (today()) weekStart

    let sessions =
      rows |> List.sumBy(fun row -> if row.Session.IsSome then 1 else 0)

    Html.div [
      attr.style "display:flex;flex-direction:column"
      Html.div [
        attr.className "header"
        attr.style "padding:8px 16px 0"
        Html.text(weekRangeText weekStart)
      ]
      Html.div [
        attr.style "display:flex;align-items:center;gap:8px;padding:8px 16px"
        Html.span [
          attr.className "caption"
          attr.style "opacity:0.6"
          Html.text(weekChipText weekStart)
        ]
        Html.span [ attr.style "flex:1" ]
        Html.button [
          attr.className "day-chevron"
          on.click(fun _ -> setSelectedDate(addDays selectedDate.Value -7))
          Html.metroIcon [ attr.icon "back" ]
        ]
        Html.button [
          attr.className "day-chevron"
          on.click(fun _ -> setSelectedDate(addDays selectedDate.Value 7))
          Html.metroIcon [ attr.icon "forward" ]
        ]
        Html.button [
          attr.className "day-chevron"
          attr.style "min-width:auto;padding:0 8px"
          on.click(fun _ -> setSelectedDate(today()))
          Html.span [ attr.className "caption"; Html.text s.Hoy ]
        ]
      ]
      yield! rows |> List.map(fun row -> weekRowView row)
      Html.div [
        attr.className "caption"
        attr.style "opacity:0.6;padding:12px 16px"
        Html.text(s.SessionsSummary sessions)
      ]
    ])
  |> Option.defaultValue Html.none

let inline pivot() =
  Html.metroPivot [
    attr.selectedIndex pivotIndex
    on.pivotSelectionChanged(fun detail ->
      pivotIndex.Value <- float detail.selectedIndex)
    Html.metroPivotItem [
      attr.header "Day"
      Html.switchWith(
        (fun () ->
          parsed.Value |> Option.map(fun parsedPlan -> parsedPlan.Plan),
          activeImport.Value |> Option.map(fun import -> import.Anchor),
          view.Value,
          mondayOf selectedDate.Value),
        fun (plan, anchor, viewValue, weekStart) ->
          dayHubView plan anchor viewValue weekStart
      )
    ]
    Html.metroPivotItem [
      attr.header "Week"
      Html.switchWith(
        (fun () ->
          parsed.Value |> Option.map(fun parsedPlan -> parsedPlan.Plan),
          activeImport.Value |> Option.map(fun import -> import.Anchor),
          view.Value,
          mondayOf selectedDate.Value),
        fun (plan, anchor, viewValue, weekStart) ->
          weekView plan anchor viewValue weekStart
      )
    ]
  ]

let view() =
  Html.div [
    dateBlock()
    Html.show((fun () -> activeImport.Value.IsNone), emptyState)
    Html.show(
      (fun () -> activeImport.Value.IsSome),
      fun () ->
        Html.div [
          attr.style "display:flex;flex-direction:column;gap:8px"
          planLine()
          viewChipArea()
          pivot()
        ]
    )
  ]

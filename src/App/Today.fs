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

let inline firstFile(input: HTMLInputElement) : File option =
  if input.files.length > 0 then
    Some input.files.[0]
  else
    None

let dateBlock(s: Strings) =
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
        s.WeekQuarter week (quarter selectedDate.Value))
    ]
    Html.div [
      attr.style "height:1px;margin:16px;background:currentColor;opacity:0.15"
    ]
  ]

let emptyState(s: Strings) =
  Html.div [
    attr.style "padding:16px;display:flex;flex-direction:column;gap:12px"
    Html.p [
      attr.className "body"
      attr.style "margin:0"
      Html.text s.EmptyTitle
    ]
    Html.p [
      attr.className "body"
      attr.style "opacity:0.6;margin:0"
      Html.text s.EmptyBody
    ]
    Html.input [
      attr.custom("type", "file")
      attr.custom("accept", ".txt,.briple,text/plain")
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
      Html.text s.LoadPlan
    ]
    Html.metroButton [
      on.click(fun _ -> importSample() |> ignore)
      Html.text s.TrySample
    ]
    Html.show(
      (fun () -> importError.Value.IsSome),
      fun () ->
        Chrome.noticeCard s.ImportFailed [
          importError.Value |> Option.defaultValue ""
        ]
    )
  ]

let planLineText(s: Strings) =
  entryFor selectedDate.Value
  |> Option.map(fun entry ->
    let plan = entry.Parsed.Plan
    let anchor = entry.Import.Anchor

    match planWeek plan anchor selectedDate.Value with
    | Some week ->
      match tryMesocicloRir plan week with
      | Some rir -> $"{s.PlanWeek week plan.Semanas} · RIR {rir}"
      | None -> s.PlanWeek week plan.Semanas
    | None ->
      if daysBetween anchor selectedDate.Value < 0 then
        s.BeforePlan
      else
        s.PlanDone)
  |> Option.defaultValue ""

let inline planLine(s: Strings) =
  Html.div [
    attr.className "body"
    attr.style "opacity:0.6;padding:0 16px"
    // Lazy on purpose: an eager call would read selectedDate inside the
    // enclosing dynamic scope and rebuild the whole branch - pivot included
    // - on every date change.
    Html.text(fun () -> planLineText s)
  ]

// The session-detail command on the app bar exists only when the selected
// date carries a session in the viewed variant; rest days and the quiet
// before/after states have nothing to show.
let hasSelectedSession() : bool =
  match entryFor selectedDate.Value with
  | Some entry ->
    let plan = entry.Parsed.Plan
    let genero, opcionId = viewFor plan

    trySession plan genero opcionId entry.Import.Anchor selectedDate.Value
    |> Option.isSome
  | None -> false

// --- View chip -----------------------------------------------------------
// The chip is the only place Genero/Opcion appear on Today. The flyout is
// imperative in metrino 0.5.2: `open` is a read-only getter, so the chip
// calls `show()` on the captured element with itself as the anchor. The
// backdrop and light dismiss belong to the component.

let mutable viewChipButton: HTMLElement = Unchecked.defaultof<_>
let mutable viewMenu: MetroMenuFlyout = Unchecked.defaultof<_>

let inline openViewMenu() =
  if not(isNull viewMenu) && not(isNull viewChipButton) then
    viewMenu.show viewChipButton

let inline hideViewMenu() =
  if not(isNull viewMenu) then
    viewMenu.hide()

let inline chipText(s: Strings) =
  entryFor selectedDate.Value
  |> Option.map(fun entry ->
    let genero, opcionId = viewFor entry.Parsed.Plan

    $"{s.GeneroWord genero} · {Variants.display s.DiasUnit opcionId}")
  |> Option.defaultValue ""

let inline viewChip(s: Strings) =
  Html.metroButton [
    attr.className "view-chip"
    attr.style "min-height:34px;color:var(--metro-accent)"
    attr.ref(fun el -> viewChipButton <- el)
    on.click(fun _ -> openViewMenu())
    Html.span [ attr.className "badge-text"; Html.text(chipText s) ]
    Html.metroIcon [ attr.icon "chevron-down" ]
  ]

let inline flyoutItem (plan: Plan) (genero: Genero) (item: VariantItem) =
  let isActive() = viewFor plan = (genero, item.Id)

  Html.div [
    attr.className "menu-item"
    attr.style(fun () -> if isActive() then "color:var(--metro-accent)" else "")
    on.click(fun _ ->
      hideViewMenu()
      setView genero item.Id |> ignore)
    Html.show(isActive, fun () -> Html.metroIcon [ attr.icon "check" ])
    Html.span [ Html.text item.Label ]
  ]

let inline viewFlyout(s: Strings) =
  match entryFor selectedDate.Value with
  | None -> Html.none
  | Some entry ->
    let plan = entry.Parsed.Plan

    let blocks = Variants.groups plan s.DiasUnit

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

        let items = group.Items |> List.map(flyoutItem plan group.Genero)

        if index = 0 then
          header :: items
        else
          Html.div [ attr.className "menu-divider" ] :: header :: items)

    Html.metroMenuFlyout [
      attr.ref(fun el -> viewMenu <- el :?> MetroMenuFlyout)
      yield! rows
    ]

let inline viewChipArea(s: Strings) =
  // Scoped rebuild: the chip and flyout read selectedDate at build time, so
  // they re-key per date here instead of inside the page's dynamic scope,
  // where a rebuild would replace the pivot element and reset its tab.
  Html.switchWith(
    (fun () -> plans.Value, selectedDate.Value, view.Value),
    fun _ ->
      Html.div [
        attr.style
          "display:flex;flex-direction:column;align-items:flex-start;padding:0 16px"
        viewChip s
        viewFlyout s
      ]
  )

let inline cardView (s: Strings) (date: DateOnly) (dia: Dia) =
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
            match exercise.Scheme with
            | Some scheme -> [
                Html.div [
                  attr.className "caption"
                  attr.style "opacity:0.65"
                  Html.text scheme
                ]
              ]
            | None -> []
        ]
      ])
  ]

let inline quietLine(text: string) =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.6;text-align:center;padding:16px"
    Html.text text
  ]

let inline dayContent (s: Strings) (entry: QueueEntry option) (date: DateOnly) =
  match entry with
  | Some entry ->
    let plan = entry.Parsed.Plan
    let anchor = entry.Import.Anchor
    let genero, opcionId = viewFor plan

    match trySession plan genero opcionId anchor date with
    | Some dia -> cardView s date dia
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
  | None -> Html.none

// --- Day hub ----------------------------------------------------------------

// The raw emit lives in a nested module the signature file does not declare:
// Fable drops [<Emit>] bindings that a signature file exposes (see Locale.fs).
module Dom =
  [<Emit("window.requestAnimationFrame($0)")>]
  let requestAnimationFrame(callback: unit -> unit) : unit = jsNative

  [<Emit("document.contains($0)")>]
  let isAttached(element: HTMLElement) : bool = jsNative

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

// The date the last programmatic navigation asked for. A hub settle that
// disagrees with it is a mid-flight scroll reporting an older position:
// the asked date wins until the user touches the hub again.
let mutable askedDate: DateOnly option = None

// Every selectedDate write from outside the hub goes through here. The hub
// is keyed on the week, so a same-week change is a scroll plus a header
// mark; a week change leaves the work to the rebuild's mount scroll.
let inline setSelectedDate(date: DateOnly) : unit =
  askedDate <- Some date
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
      // The date leads; the hub follows as far as its scroll range allows.
      // Reading the hub's settled index would stall on the trailing days a
      // viewport cannot bring to the padding line - the scroll ends before
      // their start, so they can never become the settled section.
      setSelectedDate(addDays selectedDate.Value step))
    Html.metroIcon [ attr.icon direction ]
  ]

// The Week view's "Hoy" jump, mirrored in the Day hub: after a few chevron
// weeks the user cannot tell whether the shown week is current. A tap
// writes today; the same-week scroll or the week rebuild opens on it.
let inline todayButton(s: Strings) =
  Html.button [
    attr.className "day-chevron"
    attr.style "min-width:auto;padding:0 8px"
    on.click(fun _ -> setSelectedDate(today()))
    Html.span [ attr.className "caption"; Html.text s.Hoy ]
  ]

/// One day of the visible week: its position in the hub, the date, whether
/// it opens selected, and the queue entry that owns it.
type DayModel = {
  Index: int
  Date: DateOnly
  Selected: bool
  Entry: QueueEntry option
}

let inline daySection (s: Strings) (day: DayModel) : DomItem =
  Html.metroHubSection [
    attr.header(weekdayShort(locale(), toDateTime day.Date))
    yield! if day.Selected then [ attr.custom("selected", "") ] else []
    on.click(fun _ ->
      // Tap a peeking section to bring its day into view. Taps on the
      // in-view section stay free for the session card.
      if not(isNull dayHub) && int dayHub.selectedIndex <> day.Index then
        dayHub.scrollToSection(float day.Index, "smooth"))
    dayContent s day.Entry day.Date
  ]

// The hub subtree is keyed on the week of the selected date, never on the
// date itself: a day change is a scroll, only a week rollover rebuilds.
// Each day resolves its own entry: a hand-moved anchor can split the week
// across two plans.
let inline dayHubView (s: Strings) (weekStart: DateOnly) =
  let dates = weekDates weekStart

  Html.div [
    attr.style "display:flex;flex-direction:column;gap:4px"
    // The hub reports settles with the same bubbling `selectionchanged`
    // name the pivot uses: left alone, a hub settle lands in the pivot's
    // handler and moves the tab. This wrapper ends hub events before they
    // reach it; the hub's own listener already ran at the target.
    on.event("selectionchanged", fun event -> event.stopPropagation())
    // A touch on the container hands selection back to the user's pan.
    on.event("pointerdown", fun _ -> askedDate <- None)
    Html.div [
      attr.style "display:flex;justify-content:center;gap:16px;padding:4px 0"
      chevronButton "back" -1
      todayButton s
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
        // A reactive rebuild disposes the hub mid-scroll; its dying settle
        // reports a stale index. Only a connected hub speaks for the user,
        // and never against the date a programmatic navigation asked for.
        if Dom.isAttached dayHub then
          let date = addDays weekStart detail.selectedIndex

          match askedDate with
          | Some asked when date <> asked -> ()
          | _ ->
            askedDate <- None
            selectedDate.Value <- date
            markSelectedSection detail.selectedIndex)
      yield!
        dates
        |> List.indexed
        |> List.map(fun (index, date) ->
          let selected = index = dayIndexOf weekStart

          daySection s {
            Index = index
            Date = date
            Selected = selected
            Entry = entryFor date
          })
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

let weekChipText (s: Strings) (date: DateOnly) =
  match entryFor date with
  | Some entry ->
    match planChipState entry.Parsed.Plan entry.Import.Anchor date with
    | Inside(week, total) -> s.PlanWeek week total
    | StartsOn start -> s.StartsOn(dayMonth(locale(), toDateTime start))
    | Completed -> s.PlanDone
  | None -> ""

let weekRowView (s: Strings) (row: WeekRow) =
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
// week-scoped, so day changes within the week never rebuild them. Rows
// resolve per date: a hand-moved anchor can split the week across plans.
let inline weekView (s: Strings) (weekStart: DateOnly) =
  let rows =
    weekDates weekStart
    |> List.map(fun date ->
      let session =
        match entryFor date with
        | Some entry ->
          let plan = entry.Parsed.Plan
          let genero, opcionId = viewFor plan

          trySession plan genero opcionId entry.Import.Anchor date
        | None -> None

      {
        Date = date
        IsToday = sameDate date (today())
        Session = session
      })

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
        Html.text(weekChipText s weekStart)
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
    yield! rows |> List.map(weekRowView s)
    Html.div [
      attr.className "caption"
      attr.style "opacity:0.6;padding:12px 16px"
      Html.text(s.SessionsSummary sessions)
    ]
  ]

let inline pivot(s: Strings) =
  // The key carries the queue and the view only to re-key the subtree when
  // either changes; the builders resolve everything per date.
  Html.metroPivot [
    attr.selectedIndex pivotIndex
    on.pivotSelectionChanged(fun detail ->
      pivotIndex.Value <- float detail.selectedIndex)
    Html.metroPivotItem [
      attr.header "Day"
      Html.switchWith(
        (fun () -> plans.Value, view.Value, mondayOf selectedDate.Value),
        fun (_, _, weekStart) -> dayHubView s weekStart
      )
    ]
    Html.metroPivotItem [
      attr.header "Week"
      Html.switchWith(
        (fun () -> plans.Value, view.Value, mondayOf selectedDate.Value),
        fun (_, _, weekStart) -> weekView s weekStart
      )
    ]
  ]

let view() =
  let s = strings()

  Html.div [
    dateBlock s
    Html.show((fun () -> plans.Value.IsEmpty), fun () -> emptyState s)
    Html.show(
      (fun () -> hasPlans()),
      fun () ->
        Html.div [
          attr.style "display:flex;flex-direction:column;gap:8px"
          planLine s
          viewChipArea s
          pivot s
        ]
    )
  ]

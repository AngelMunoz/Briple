module App.PlanPage

// The Plan page (storyboard 4.5), built as small components: every function
// owns one piece and takes its data as props, the way Solid components do.
// view() only composes them.

open System
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Browser.Types
open Briple.Store
open Plan.Types
open Plan.Projection
open App.Locale
open App.Variants
open App.Chrome
open App.State

let mutable fileInput: HTMLInputElement = Unchecked.defaultof<_>

let inline titleBlock (s: Strings) (plan: Plan) : DomItem list = [
  Html.div [
    attr.className "title"
    Html.text $"{plan.Titulo} · {s.WeeksCount plan.Semanas}"
  ]
  Html.div [
    attr.className "caption"
    attr.style "opacity:0.6"
    Html.text $"origen: {plan.Origen}"
  ]
]

/// The row's right caption: the label minus its display form ("3 días — X"
/// shows "X"); a label without the separator shows nothing.
let inline rowTitle(label: string) : string =
  match label.IndexOf(" — ") with
  | -1 -> ""
  | index -> label.Substring(index + 3)

let variantRow
  (s: Strings)
  (cell: Genero * VariantItem)
  (isActive: unit -> bool)
  : DomItem =
  let genero, item = cell

  Html.button [
    attr.className(fun () ->
      if isActive() then "week-row today" else "week-row")
    on.click(fun _ ->
      setView genero item.Id |> ignore
      goTo TodayPage)
    Html.span [
      attr.className "body"
      Html.text $"{s.GeneroWord genero} · {item.Display}"
    ]
    Html.span [ attr.style "flex:1" ]
    Html.span [
      attr.className "caption"
      attr.style
        "opacity:0.6;max-width:55%;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"
      Html.text(rowTitle item.Label)
    ]
  ]

let inline variantList (s: Strings) (plan: Plan) : DomItem list = [
  heading s.VariantsHeading
  let activeView = viewFor plan

  yield!
    groups plan s.DiasUnit
    |> List.collect(fun group ->
      group.Items
      |> List.map(fun item ->
        variantRow s (group.Genero, item) (fun () ->
          activeView = (group.Genero, item.Id))))
]

let anchorRow (s: Strings) (entry: QueueEntry) : DomItem list =
  let anchor = Var.create entry.Import.Anchor

  [
    Html.div [
      attr.className "caption"
      attr.style "opacity:0.6"
      Html.text s.AnchorLabel
    ]
    anchorField anchor (updateAnchor entry.Import.Id)
  ]

let inline loadFile(s: Strings) : DomItem list = [
  Html.metroButton [
    attr.style "margin-top:8px"
    on.click(fun _ -> fileInput.click())
    Html.text s.LoadPlan
  ]
  Html.input [
    attr.custom("type", "file")
    attr.custom("accept", ".txt,.briple,text/plain")
    attr.style "display:none"
    attr.ref(fun el -> fileInput <- el :?> HTMLInputElement)
    on.event(
      "change",
      fun _ ->
        if fileInput.files.length > 0 then
          importFile fileInput.files.[0] |> ignore
    )
  ]
]

let inline guideSection (s: Strings) (plan: Plan) : DomItem list = [
  heading s.GuiaHeading
  yield!
    plan.Extras
    |> List.map extraKey
    |> List.groupBy fst
    |> List.map(fun (key, pairs) ->
      Html.metroExpander [
        attr.title key
        yield!
          pairs
          |> List.map(fun (_, body) ->
            Html.div [
              attr.className "body"
              attr.style "opacity:0.8;padding:4px 0"
              Html.text body
            ])
      ])
]

// The queue row's right caption: "14 de septiembre – 11 de octubre", the
// covered range.
let queueRange(entry: QueueEntry) : string =
  let l = locale()
  let start = dayMonth(l, toDateTime entry.Import.Anchor)
  let lastDay = addDays (planFinish entry.Parsed.Plan entry.Import.Anchor) -1
  $"{start} – {dayMonth(l, toDateTime lastDay)}"

let queueRow(entry: QueueEntry) : DomItem =
  let lastDay = addDays (planFinish entry.Parsed.Plan entry.Import.Anchor) -1

  let isCurrent() =
    daysBetween entry.Import.Anchor (today()) >= 0
    && daysBetween (today()) lastDay >= 0

  Html.div [
    attr.style "display:flex;align-items:stretch;gap:8px"
    Html.button [
      attr.className(fun () ->
        if isCurrent() then "week-row today" else "week-row")

      attr.style "flex:1"
      on.click(fun _ ->
        selectedDate.Value <- entry.Import.Anchor
        pivotIndex.Value <- 0.0
        goTo TodayPage)
      Html.span [
        attr.className "body"
        attr.style "flex:1;text-align:left"
        Html.text entry.Import.FileName
      ]
      Html.span [
        attr.className "caption"
        attr.style "opacity:0.6"
        Html.text(queueRange entry)
      ]
    ]
    Html.button [
      attr.className "day-chevron"
      attr.custom("aria-label", "delete")
      on.click(fun _ -> removeAt entry.Import.Id)
      Html.metroIcon [ attr.icon "delete" ]
    ]
  ]

let queueSection(s: Strings) : DomItem =
  Html.div [
    attr.style "display:flex;flex-direction:column"
    heading s.ImportsHeading
    Html.switchWith(
      (fun () -> plans.Value),
      fun entries ->
        Html.div [
          attr.style "display:flex;flex-direction:column"
          yield! entries |> List.map queueRow
        ]
    )
  ]

let inline content(s: Strings) : DomItem =
  match currentEntry() with
  | Some entry ->
    let plan = entry.Parsed.Plan

    Html.div [
      attr.style
        "display:flex;flex-direction:column;gap:12px;padding:0 16px 16px"
      yield! titleBlock s plan
      yield! variantList s plan
      yield! anchorRow s entry
      yield! loadFile s
      yield! guideSection s plan
      queueSection s
    ]
  | None -> Html.none

let inline emptyPlan(s: Strings) : DomItem =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.5;padding:0 16px"
    Html.text s.EmptyBody
  ]

let view() : DomItem =
  let s = strings()

  Html.div [
    backHeader s.PagePlan
    Html.show((fun () -> plans.Value.IsEmpty), fun () -> emptyPlan s)
    Html.show((fun () -> hasPlans()), fun () -> content s)
  ]

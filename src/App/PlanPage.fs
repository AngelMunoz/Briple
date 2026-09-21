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
open App.Locale
open App.Variants
open App.Chrome
open App.State

let mutable fileInput: HTMLInputElement = Unchecked.defaultof<_>

let inline titleBlock(plan: Plan) : DomItem list =
  let s = strings()

  [
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

let inline variantRow
  (s: Strings)
  (genero: Genero)
  (item: VariantItem)
  : DomItem =
  let isActive() = view.Value = (genero, item.Id)

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

let inline variantList(plan: Plan) : DomItem list =
  let s = strings()

  [
    heading s.VariantsHeading
    yield!
      groups plan s.DiasUnit
      |> List.collect(fun group ->
        group.Items |> List.map(fun item -> variantRow s group.Genero item))
  ]

let inline anchorRow(active: StoredImport) : DomItem list =
  let anchor = Var.create active.Anchor

  [
    Html.div [
      attr.className "caption"
      attr.style "opacity:0.6"
      Html.text (strings()).AnchorLabel
    ]
    anchorField anchor reAnchor
  ]

let inline loadFile() : DomItem list = [
  Html.metroButton [
    attr.style "margin-top:8px"
    on.click(fun _ -> fileInput.click())
    Html.text (strings()).LoadPlan
  ]
  Html.input [
    attr.custom("type", "file")
    attr.custom("accept", ".txt,text/plain")
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

let inline guideSection(plan: Plan) : DomItem list = [
  heading (strings()).GuiaHeading
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

let historyStamp(importedAt: string) : string =
  try
    let day = Iso.toDateOnly importedAt.[0..9]

    if day = today() then
      (strings()).Hoy
    else
      dayMonth(locale(), toDateTime day)
  with _ ->
    ""

let inline historyRow (currentId: string) (import: StoredImport) : DomItem =
  let isCurrent = import.Id = currentId

  Html.button [
    attr.className(if isCurrent then "week-row today" else "week-row")
    on.click(fun _ ->
      if not isCurrent then
        activateImport import)
    Html.span [
      attr.className "body"
      attr.style "flex:1;text-align:left"
      Html.text import.FileName
    ]
    Html.span [
      attr.className "caption"
      attr.style "opacity:0.6"
      Html.text(historyStamp import.ImportedAt)
    ]
  ]

let inline importsSection(currentId: string) : DomItem =
  let history: Var<StoredImport list> = Var.create []

  listImports()
  |> Promise.map(fun imports -> history.Value <- imports)
  |> ignore

  Html.div [
    attr.style "display:flex;flex-direction:column"
    heading (strings()).ImportsHeading
    Html.switchWith(
      (fun () -> history.Value),
      fun imports ->
        Html.div [
          attr.style "display:flex;flex-direction:column"
          yield! imports |> List.map(fun import -> historyRow currentId import)
        ]
    )
  ]

let inline content() : DomItem =
  match parsed.Value, activeImport.Value with
  | Some parsedPlan, Some active ->
    let plan = parsedPlan.Plan

    Html.div [
      attr.style
        "display:flex;flex-direction:column;gap:12px;padding:0 16px 16px"
      yield! titleBlock plan
      yield! variantList plan
      yield! anchorRow active
      yield! loadFile()
      yield! guideSection plan
      importsSection active.Id
    ]
  | _ -> Html.none

let inline emptyPlan() : DomItem =
  Html.p [
    attr.className "body"
    attr.style "opacity:0.5;padding:0 16px"
    Html.text (strings()).EmptyBody
  ]

let view() : DomItem =
  Html.div [
    backHeader (strings()).PagePlan
    Html.show((fun () -> parsed.Value.IsNone), fun () -> emptyPlan())
    Html.show(
      (fun () -> parsed.Value.IsSome && activeImport.Value.IsSome),
      fun () -> content()
    )
  ]

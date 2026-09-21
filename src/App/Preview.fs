module App.Preview

// Import preview (storyboard 4.6, S5b): a full-bleed page that shows
// everything the file contains - variants as facts, not radios - plus the
// start-date decision and the parse warnings. The store stays untouched
// until the commit button runs.

open System
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Plan.Types
open App.Locale
open App.Variants
open App.Chrome
open App.State

let inline metaLine(fileName: string) : DomItem =
  Html.div [
    attr.className "caption"
    attr.style "opacity:0.6"
    Html.text $"{fileName} · ✓"
  ]

let inline planTitle (s: Strings) (plan: Plan) : DomItem =
  Html.div [
    attr.className "header"
    Html.text $"{plan.Titulo} · {s.WeeksCount plan.Semanas}"
  ]

let inline factRow(label: string) : DomItem =
  Html.div [
    attr.className "body"
    attr.style "padding:2px 0 2px 12px"
    Html.text label
  ]

let inline factGroups (s: Strings) (plan: Plan) : DomItem list =
  groups plan s.DiasUnit
  |> List.collect(fun group -> [
    heading(s.GeneroWord group.Genero)
    yield! group.Items |> List.map(fun item -> factRow item.Label)
  ])

let inline startDecision (s: Strings) (anchor: Var<DateOnly>) : DomItem list = [
  heading s.StartHeading
  Html.div [
    attr.style "display:flex;align-items:center;gap:8px"
    Html.span [
      attr.className "body"
      attr.style "opacity:0.6"
      Html.text s.StartsLabel
    ]
    anchorField anchor ignore
  ]
]

let inline actions (s: Strings) (anchor: Var<DateOnly>) : DomItem =
  Html.div [
    attr.style "display:flex;gap:8px;margin-top:8px"
    Html.metroButton [
      on.click(fun _ -> commitImport anchor.Value)
      Html.text s.Commit
    ]
    Html.metroButton [
      on.click(fun _ ->
        pendingImport.Value <- None
        goBack())
      Html.text s.Cancel
    ]
  ]

let view() : DomItem =
  match pendingImport.Value with
  | None -> Html.none
  | Some staged ->
    let plan = staged.Parsed.Plan
    let warnings = staged.Parsed.Warnings
    let anchor = Var.create staged.Anchor
    let s = strings()

    Html.div [
      backHeader s.PreviewTitle
      Html.div [
        attr.style
          "display:flex;flex-direction:column;gap:12px;padding:0 16px 16px"
        metaLine staged.FileName
        planTitle s plan
        yield! factGroups s plan
        Html.div [
          attr.style
            "height:1px;margin:8px 0;background:currentColor;opacity:0.15"
        ]
        yield! startDecision s anchor
        Html.show(
          (fun () -> not warnings.IsEmpty),
          fun () ->
            noticeCard
              (s.WarningsLine warnings.Length)
              (warnings
               |> List.map(fun warning ->
                 s.WarningLine warning.Message warning.Line))
        )
        actions s anchor
      ]
    ]

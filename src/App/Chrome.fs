module App.Chrome

// Shared UI components: the back header, the badge section heading, the
// accent notice card, and the anchor field. Pages compose these; the pieces
// depend on State and Locale, never on a page module.

open System
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Briple.Store
open Plan.Types
open App.Locale
open App.State

let inline backHeader(title: string) : DomItem =
  Html.header [
    attr.style "display:flex;align-items:center;padding:16px"
    Html.metroHyperlinkButton [
      on.click(fun _ -> goBack())
      Html.metroIcon [
        attr.icon "back"
        attr.size XLarge
        attr.style "margin-right:8px"
      ]
      Html.span [ attr.className "title"; Html.text title ]
    ]
  ]

/// Section heading in the badge typography.
let inline heading(text: string) : DomItem =
  Html.div [
    attr.className "badge-text"
    attr.style "opacity:0.6;margin-top:8px"
    Html.text text
  ]

let inline schemeLine(exercise: Ejercicio) : string option =
  [ exercise.Reps; exercise.Rir; exercise.Descanso ]
  |> List.filter(fun part -> part <> "-")
  |> String.concat " · "
  |> fun line -> if line = "" then None else Some line

/// The accent-bordered notice surface: a title line and quiet detail lines.
let inline noticeCard (title: string) (lines: string list) : DomItem =
  Html.div [
    attr.style "border-left:3px solid var(--metro-accent);padding:8px 12px"
    Html.div [ attr.className "body"; Html.text title ]
    yield!
      lines
      |> List.map(fun line ->
        Html.div [
          attr.className "caption"
          attr.style "opacity:0.7"
          Html.text line
        ])
  ]

let inline tryIso(value: string) =
  try
    Some(Iso.toDateOnly value)
  with _ ->
    None

/// Field + inline roller for a DateOnly. The field shows the localized date;
/// a tap expands the roller. Roller changes write the var and call `onChanged`.
let inline anchorField
  (anchor: Var<DateOnly>)
  (onChanged: DateOnly -> unit)
  : DomItem =
  let rollerOpen = Var.create false

  Html.div [
    attr.style
      "display:flex;flex-direction:column;gap:8px;align-items:flex-start"
    Html.button [
      attr.className "day-chevron"
      attr.style "min-width:auto;min-height:34px;padding:0 12px"
      on.click(fun _ -> rollerOpen.Value <- not rollerOpen.Value)
      Html.span [
        attr.className "body"
        Html.text(fun () -> dayMonth(locale(), toDateTime anchor.Value))
      ]
      Html.metroIcon [ attr.icon "chevron-down" ]
    ]
    Html.show(
      (fun () -> rollerOpen.Value),
      fun () ->
        Html.metroDatePickerRoller [
          attr.custom("value", Iso.ofDateOnly anchor.Value)
          attr.custom("min-year", string(anchor.Value.Year - 5))
          attr.custom("max-year", string(anchor.Value.Year + 5))
          on.valueChanged(fun change ->
            match tryIso change.value with
            | Some date ->
              anchor.Value <- date
              onChanged date
            | None -> ())
        ]
    )
  ]

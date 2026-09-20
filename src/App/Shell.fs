module App.Shell

// Page shell: Today is home; Plan and Settings are placeholder
// pages with back chevrons. The app bar itself holds
// commands, never navigation: the ⋯ menu carries the Plan / Settings items.

open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open App.Locale
open App.State

let backHeader(title: string) =
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

let planPage() =
  Html.div [
    backHeader(strings().PagePlan)
    Html.p [
      attr.className "body"
      attr.style "opacity:0.5;padding:0 16px"
      Html.text "…"
    ]
  ]

let settingsPage() =
  Html.div [
    backHeader(strings().PageSettings)
    Html.p [
      attr.className "body"
      attr.style "opacity:0.5;padding:0 16px"
      Html.text "…"
    ]
  ]

let view() =
  Html.div [
    attr.style "display:flex;flex-direction:column;height:100%"
    Html.div [
      attr.style "flex:1 1 auto;min-height:0"
      Html.switch router.CurrentRoute (function
        | Some PlanPage -> planPage()
        | Some SettingsPage -> settingsPage()
        // Today is the hub; an unparsed URL degrades to it too.
        | _ -> App.Today.view())
    ]
  ]

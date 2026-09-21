module App.Shell

// Page shell: Today is home; Plan, Settings, and the import preview are
// spokes with back chevrons. The app bar itself holds
// commands, never navigation: the ⋯ menu carries the Plan / Settings items.

open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open App.Chrome
open App.Locale
open App.State

let inline settingsPage() =
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
        | Some PlanPage -> App.PlanPage.view()
        | Some ImportPage -> App.Preview.view()
        | Some SettingsPage -> settingsPage()
        // Today is the hub; an unparsed URL degrades to it too.
        | _ -> App.Today.view())
    ]
  ]

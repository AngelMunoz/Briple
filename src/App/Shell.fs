module App.Shell

// Page shell: Today is home; Plan, Settings, and the import preview are
// spokes with back chevrons. The shell owns the fixed app bar and the
// scrolling content region: the bar is measured into `--app-bar-height`,
// which the content reserves so no page hides its last section behind it.

open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open Browser.Types
open App.Chrome
open App.Locale
open App.State

// The raw emits live in a nested module the signature file does not declare:
// Fable drops [<Emit>] bindings that a signature file exposes (see Locale.fs).
module Dom =
  [<Emit("new ResizeObserver($0)")>]
  let createResizeObserver(callback: unit -> unit) : obj = jsNative

  [<Emit("(() => { $0.disconnect(); $0.observe($1); })()")>]
  let observeResize (observer: obj) (element: HTMLElement) : unit = jsNative

  [<Emit("document.documentElement.style.setProperty($0, $1)")>]
  let setRootVar (name: string) (value: string) : unit = jsNative

  [<Emit("(() => { const blob = new Blob([$1], { type: 'text/plain' }); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = $0; document.body.appendChild(a); a.click(); a.remove(); URL.revokeObjectURL(url); })()")>]
  let downloadText (name: string) (text: string) : unit = jsNative

// --- Fixed app bar ----------------------------------------------------------
// metrino pins the bar to the viewport bottom, so it takes no layout space.
// Every mounted bar is measured; the height lands in `--app-bar-height`,
// which the scrolling region reserves. The observer follows expansion and
// font scaling.

let mutable private appBarElement: HTMLElement = Unchecked.defaultof<_>
let mutable private appBarObserver: obj = null

let private measureAppBar() =
  if not(isNull appBarElement) then
    Dom.setRootVar
      "--app-bar-height"
      $"{appBarElement.getBoundingClientRect().height}px"

let private bindAppBar(el: HTMLElement) =
  appBarElement <- el

  if not(isNull el) then
    if isNull appBarObserver then
      appBarObserver <- Dom.createResizeObserver measureAppBar

    Dom.observeResize appBarObserver el
    measureAppBar()

let todayBar() =
  Html.metroAppBar [
    attr.ref(fun el -> bindAppBar el)
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

let planBar() =
  Html.metroAppBar [
    attr.ref(fun el -> bindAppBar el)
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "download"
      attr.label (strings()).ExportMenu
      on.click(fun _ ->
        activeImport.Value
        |> Option.iter(fun import ->
          Dom.downloadText import.FileName import.Raw))
    ]
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "delete"
      attr.label (strings()).RemoveMenu
      on.click(fun _ -> removePlan())
    ]
  ]

let appBar() =
  Html.switch router.CurrentRoute (function
    | Some PlanPage -> planBar()
    | Some SettingsPage
    | Some ImportPage -> Html.none
    | _ -> todayBar())

// The import preview and the Settings placeholder have no bar; their content
// uses the whole viewport.
let private routeHasAppBar() =
  match router.CurrentRoute.Value with
  | Some SettingsPage
  | Some ImportPage -> false
  | _ -> true

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
      attr.style(fun () ->
        if routeHasAppBar() then
          "flex:1 1 auto;min-height:0;overflow-y:auto;padding-bottom:calc(var(--app-bar-height, 65px) + env(safe-area-inset-bottom, 0px))"
        else
          "flex:1 1 auto;min-height:0;overflow-y:auto")
      Html.switch router.CurrentRoute (function
        | Some PlanPage -> App.PlanPage.view()
        | Some ImportPage -> App.Preview.view()
        | Some SettingsPage -> settingsPage()
        // Today is the hub; an unparsed URL degrades to it too.
        | _ -> App.Today.view())
    ]
    appBar()
  ]

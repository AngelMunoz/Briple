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
open App.Intents
open App.SessionDetail
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

// --- Fixed app bar ----------------------------------------------------------
// metrino pins the bar to the viewport bottom, so it takes no layout space.
// Every mounted bar is measured; the height lands in `--app-bar-height`,
// which the scrolling region reserves. The observer follows expansion and
// font scaling.

let mutable appBarElement: HTMLElement = Unchecked.defaultof<_>
let mutable appBarObserver: obj = null

let measureAppBar() =
  if not(isNull appBarElement) then
    Dom.setRootVar
      "--app-bar-height"
      $"{appBarElement.getBoundingClientRect().height}px"

let bindAppBar(el: HTMLElement) =
  appBarElement <- el

  if not(isNull el) then
    if isNull appBarObserver then
      appBarObserver <- Dom.createResizeObserver measureAppBar

    Dom.observeResize appBarObserver el
    measureAppBar()

// The sheet's own success needs no app feedback; a clipboard fallback does,
// and a failure toasts once. The empty error text means "nothing to add":
// the toast shows the localized failure line alone.
let toastShareResult (s: Strings) (result: ShareResult) : unit =
  let show (message: string) (severity: Metrino.Ripple.Severity) : unit =
    State.toastHost.show {
      title = None
      message = message
      severity = Some severity
      duration = Some 4000.0
    }
    |> ignore

  match result with
  | Shared
  | Downloaded
  | Cancelled -> ()
  | Copied -> show s.ToastCopied Metrino.Ripple.Success

  | Failed error ->
    let message =
      if error = "" then
        s.ShareFailed
      else
        $"{s.ShareFailed} · {error}"

    show message Metrino.Ripple.Error

let shareSelectedDay(s: Strings) : unit =
  match parsed.Value, activeImport.Value with
  | Some parsedPlan, Some import ->
    let plan = parsedPlan.Plan

    Intents.shareText
      plan.Titulo
      (SessionDetail.sessionText
        s
        plan
        import.Anchor
        view.Value
        selectedDate.Value)
    |> Promise.iter(toastShareResult s)
  | _ -> ()

let todayBar(s: Strings) =
  Html.metroAppBar [
    attr.ref(fun el -> bindAppBar el)
    // Primary commands: the session detail and the day share for the
    // selected day. The show wrapper keeps both off the bar on rest days
    // and the quiet states.
    Html.show(
      App.Today.hasSelectedSession,
      fun () ->
        Html.metroAppBarButton [
          attr.icon "info"
          attr.label s.DetailMenu
          attr.custom("aria-label", s.DetailMenu)
          on.click(fun _ -> goTo SessionPage)
        ]
    )
    Html.show(
      App.Today.hasSelectedSession,
      fun () ->
        Html.metroAppBarButton [
          attr.icon "share"
          attr.label s.ShareDay
          attr.custom("aria-label", s.ShareDay)
          on.click(fun _ -> shareSelectedDay s)
        ]
    )
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "calendar"
      attr.label s.PlanMenu
      on.click(fun _ -> goTo PlanPage)
    ]
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "settings"
      attr.label s.SettingsMenu
      on.click(fun _ -> goTo SettingsPage)
    ]
  ]

let planBar(s: Strings) =
  Html.metroAppBar [
    attr.ref(fun el -> bindAppBar el)
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "download"
      attr.label s.ExportMenu
      on.click(fun _ ->
        activeImport.Value
        |> Option.iter(fun import ->
          Intents.downloadText import.FileName import.Raw))
    ]
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "share"
      attr.label s.SharePlan
      on.click(fun _ ->
        match parsed.Value, activeImport.Value with
        | Some parsedPlan, Some import ->
          Intents.shareFile parsedPlan.Plan.Titulo import.FileName import.Raw
          |> Promise.iter(toastShareResult s)
        | _ -> ())
    ]
    Html.metroAppBarButton [
      attr.custom("slot", "menu")
      attr.icon "delete"
      attr.label s.RemoveMenu
      on.click(fun _ -> removePlan())
    ]
  ]

let appBar(s: Strings) =
  Html.switch router.CurrentRoute (function
    | Some PlanPage -> planBar s
    | Some SettingsPage
    | Some ImportPage
    | Some SessionPage -> Html.none
    | _ -> todayBar s)

// The import preview, the Settings page, and the session detail have no
// bar; their content uses the whole viewport.
let routeHasAppBar() =
  match router.CurrentRoute.Value with
  | Some SettingsPage
  | Some ImportPage
  | Some SessionPage -> false
  | _ -> true

let view() =
  let s = strings()

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
        | Some SettingsPage -> App.SettingsPage.view()
        | Some SessionPage -> App.SessionDetail.view()
        // Today is the hub; an unparsed URL degrades to it too.
        | _ -> App.Today.view())
    ]
    appBar s
  ]

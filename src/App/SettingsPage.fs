module App.SettingsPage

// The page composes small components, Solid-style: every function owns one
// piece and takes its data as props; view() only composes them.

open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple
open App.Locale
open App.Theme
open App.Chrome
open App.Pwa
open App.State

/// One radio per theme choice. The group name makes metrino treat the three
/// as one radio group: picking one unchecks the others on its own.
let inline themeChoice (value: ThemeChoice) (label: string) : DomItem =
  Html.metroRadioButton [
    attr.custom("name", "theme")
    attr.custom("value", ThemeChoice.toString value)
    attr.radioChecked(fun () -> theme.Value = value)
    on.radioChanged(fun change ->
      if change.``checked`` then
        setTheme value)
    Html.span [ attr.className "body"; Html.text label ]
  ]

let inline themeSection(s: Strings) : DomItem =

  Html.div [
    heading s.ThemeHeading
    Html.div [
      attr.style "display:flex;flex-direction:column;align-items:flex-start"
      themeChoice SystemTheme s.ThemeSystem
      themeChoice LightTheme s.ThemeLight
      themeChoice DarkTheme s.ThemeDark
    ]
  ]

let swatch (s: Strings) (value: Accent) : DomItem =

  Html.button [
    attr.className(fun () ->
      if accent.Value = value then
        "accent-swatch selected"
      else
        "accent-swatch")
    attr.style $"background:{Accent.hex value}"
    attr.title(s.AccentName value)
    attr.custom("aria-label", s.AccentName value)
    on.click(fun _ -> setAccent value)
  ]

let inline accentSection(s: Strings) : DomItem =

  Html.div [
    heading s.AccentHeading
    Html.div [
      attr.className "accent-grid"
      yield! Accent.all |> List.map(swatch s)
    ]
  ]

/// The destructive confirm is a message dialog: the reset only runs through
/// its accept button; every other dismissal cancels. The dialog reference
/// rides the Elements.fs pattern: capture with `attr.ref`, cast, then the
/// typed `show`/`hide` members.
let resetSection(s: Strings) : DomItem =
  let mutable dialog: MetroMessageDialog = Unchecked.defaultof<_>

  Html.div [
    heading s.ResetHeading
    Html.p [
      attr.className "body"
      attr.style "opacity:0.7;margin:0"
      Html.text s.ResetBody
    ]
    Html.metroButton [
      on.click(fun _ -> dialog.show() |> ignore)
      Html.text s.ResetToDefaults
    ]
    Html.metroMessageDialog [
      attr.title s.ResetConfirmTitle
      attr.ref(fun el -> dialog <- el :?> MetroMessageDialog)
      Html.p [
        attr.className "body"
        attr.style "margin:0"
        Html.text s.ResetConfirmBody
      ]
      Html.metroButton [
        attr.custom("slot", "buttons")
        on.click(fun _ -> dialog.hide() |> ignore)
        Html.text s.Cancel
      ]
      Html.metroButton [
        attr.custom("slot", "buttons")
        on.click(fun _ ->
          dialog.hide() |> ignore
          resetToDefaults())
        Html.text s.ResetConfirmAccept
      ]
    ]
  ]

/// The section exists only while App.Pwa holds a prompt to offer; handing it
/// over opens the native install dialog and the section goes away.
let inline installSection(s: Strings) : DomItem =
  Html.show(
    (fun () -> installAvailable.Value),
    fun () ->
      Html.div [
        heading s.InstallHeading
        Html.p [
          attr.className "body"
          attr.style "opacity:0.7;margin:0"
          Html.text s.InstallBody
        ]
        Html.metroButton [
          on.click(fun _ -> promptInstall())
          Html.text s.InstallApp
        ]
      ]
  )

let view() : DomItem =
  let s = strings()

  Html.div [
    backHeader s.PageSettings
    Html.div [
      attr.style
        "display:flex;flex-direction:column;gap:12px;padding:0 16px 16px"
      themeSection s
      accentSection s
      resetSection s
      installSection s
    ]
  ]

module App.Theme

// Implementation notes live in Theme.fsi. The raw documentElement emits stay
// in a nested module the signature does not declare: Fable drops [<Emit>]
// bindings that a signature file exposes (see Locale.fs, Shell.fs).

open Fable.Core
open Fable.Ripple
open Briple.Store

type ThemeChoice =
  | SystemTheme
  | LightTheme
  | DarkTheme

type Accent =
  | Blue
  | Red
  | Orange
  | Green
  | Teal
  | Purple
  | Magenta
  | Lime
  | Brown
  | Pink
  | Mango
  | Cobalt
  | Indigo
  | Violet
  | Crimson
  | Emerald
  | Mauve
  | Sienna
  | Olive
  | Steel
  | Taupe

module Dom =

  [<Emit("document.documentElement.setAttribute($0, $1)")>]
  let setRootAttribute (name: string) (value: string) : unit = jsNative

  [<Emit("document.documentElement.removeAttribute($0)")>]
  let removeRootAttribute(name: string) : unit = jsNative

  [<Emit("document.documentElement.style.setProperty($0, $1)")>]
  let setRootStyle (name: string) (value: string) : unit = jsNative

  [<Emit("document.documentElement.style.removeProperty($0)")>]
  let removeRootStyle(name: string) : unit = jsNative

module ThemeChoice =

  let toString(choice: ThemeChoice) =
    match choice with
    | SystemTheme -> "system"
    | LightTheme -> "light"
    | DarkTheme -> "dark"

  let tryParse(value: string) =
    match value with
    | "system" -> Some SystemTheme
    | "light" -> Some LightTheme
    | "dark" -> Some DarkTheme
    | _ -> None

module Accent =

  let toString(accent: Accent) =
    match accent with
    | Blue -> "blue"
    | Red -> "red"
    | Orange -> "orange"
    | Green -> "green"
    | Teal -> "teal"
    | Purple -> "purple"
    | Magenta -> "magenta"
    | Lime -> "lime"
    | Brown -> "brown"
    | Pink -> "pink"
    | Mango -> "mango"
    | Cobalt -> "cobalt"
    | Indigo -> "indigo"
    | Violet -> "violet"
    | Crimson -> "crimson"
    | Emerald -> "emerald"
    | Mauve -> "mauve"
    | Sienna -> "sienna"
    | Olive -> "olive"
    | Steel -> "steel"
    | Taupe -> "taupe"

  let tryParse(value: string) =
    let normalized = value.Trim().ToLowerInvariant()

    match normalized with
    | "blue" -> Some Blue
    | "red" -> Some Red
    | "orange" -> Some Orange
    | "green" -> Some Green
    | "teal" -> Some Teal
    | "purple" -> Some Purple
    | "magenta" -> Some Magenta
    | "lime" -> Some Lime
    | "brown" -> Some Brown
    | "pink" -> Some Pink
    | "mango" -> Some Mango
    | "cobalt" -> Some Cobalt
    | "indigo" -> Some Indigo
    | "violet" -> Some Violet
    | "crimson" -> Some Crimson
    | "emerald" -> Some Emerald
    | "mauve" -> Some Mauve
    | "sienna" -> Some Sienna
    | "olive" -> Some Olive
    | "steel" -> Some Steel
    | "taupe" -> Some Taupe
    | _ -> None

  // Swatch fills mirror metrino's tokens.css; the attribute sets the same
  // values at runtime.
  let hex(accent: Accent) =
    match accent with
    | Blue -> "#0078d4"
    | Red -> "#e51400"
    | Orange -> "#fa6800"
    | Green -> "#00a300"
    | Teal -> "#00aba9"
    | Purple -> "#a200ff"
    | Magenta -> "#d80073"
    | Lime -> "#a4c400"
    | Brown -> "#825a2c"
    | Pink -> "#f472d6"
    | Mango -> "#f09609"
    | Cobalt -> "#0050ef"
    | Indigo -> "#6a00ff"
    | Violet -> "#aa00ff"
    | Crimson -> "#a20025"
    | Emerald -> "#008a00"
    | Mauve -> "#765f89"
    | Sienna -> "#a0522d"
    | Olive -> "#6d8764"
    | Steel -> "#647687"
    | Taupe -> "#87794e"

  let all: Accent list = [
    Blue
    Red
    Orange
    Green
    Teal
    Purple
    Magenta
    Lime
    Brown
    Pink
    Mango
    Cobalt
    Indigo
    Violet
    Crimson
    Emerald
    Mauve
    Sienna
    Olive
    Steel
    Taupe
  ]

let theme: Var<ThemeChoice> = Var.create SystemTheme

let accent: Var<Accent> = Var.create Blue

/// "system" defers to prefers-color-scheme: no override attribute, and the
/// inline color-scheme drops so the stylesheet's media queries rule. Light
/// and dark pin both, matching the metrino demo's manual switch.
let applyTheme(choice: ThemeChoice) : unit =
  match choice with
  | SystemTheme ->
    Dom.removeRootAttribute "data-theme"
    Dom.removeRootStyle "color-scheme"
  | LightTheme ->
    Dom.setRootAttribute "data-theme" "light"
    Dom.setRootStyle "color-scheme" "light"
  | DarkTheme ->
    Dom.setRootAttribute "data-theme" "dark"
    Dom.setRootStyle "color-scheme" "dark"

/// Blue is metrino's default: no attribute at all.
let applyAccent(accent: Accent) : unit =
  match accent with
  | Blue -> Dom.removeRootAttribute "accent"
  | other -> Dom.setRootAttribute "accent" (Accent.toString other)

/// Defaults persist as absence: applying one deletes its lane instead of
/// writing it back, so the reset path leaves the cleared store untouched.
let persistTheme(choice: ThemeChoice) : unit =
  match choice with
  | SystemTheme -> deleteStateRaw ThemeKey |> Promise.start
  | other -> setStateRaw ThemeKey (ThemeChoice.toString other) |> Promise.start

let persistAccent(accent: Accent) : unit =
  match accent with
  | Blue -> deleteStateRaw AccentKey |> Promise.start
  | other -> setStateRaw AccentKey (Accent.toString other) |> Promise.start

let setTheme(choice: ThemeChoice) : unit = theme.Value <- choice

let setAccent(value: Accent) : unit = accent.Value <- value

// Created in `init`: the subscriptions apply and persist, so they must not
// exist while init restores the stored choices.
let mutable private themeSubscription: System.IDisposable option = None
let mutable private accentSubscription: System.IDisposable option = None

let init (storedTheme: string option) (storedAccent: string option) : unit =
  theme.Value <-
    storedTheme
    |> Option.bind ThemeChoice.tryParse
    |> Option.defaultValue SystemTheme

  accent.Value <-
    storedAccent |> Option.bind Accent.tryParse |> Option.defaultValue Blue

  // Subscribing fires immediately: the restored choice is applied to the
  // document and its lane is rewritten (or deleted, for defaults).
  themeSubscription |> Option.iter(fun subscription -> subscription.Dispose())

  themeSubscription <-
    Some(
      Signal.subscribe
        (fun choice ->
          applyTheme choice
          persistTheme choice)
        theme
    )

  accentSubscription |> Option.iter(fun subscription -> subscription.Dispose())

  accentSubscription <-
    Some(
      Signal.subscribe
        (fun accent ->
          applyAccent accent
          persistAccent accent)
        accent
    )

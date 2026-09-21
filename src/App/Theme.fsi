module App.Theme

// The app theme and accent. The mechanism is metrino's own: `data-theme` on
// <html> overrides the system preference, and the `accent` attribute picks
// one of the 21 named Metro accent colors (blue is the default and removes
// the attribute). The "system" choice removes both so the stylesheet's
// prefers-color-scheme rules stay in charge.
//
// The choice persists in the store's state lanes as plain strings. Defaults
// persist as absence: applying a default deletes its lane, so a fresh store
// and a default-configured store are the same thing, and the reset path
// never writes anything back after clearing.

open Fable.Ripple

type ThemeChoice =
  | SystemTheme
  | LightTheme
  | DarkTheme

/// The 21 named Metro accent colors, in picker order (blue first).
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

module ThemeChoice =

  /// The state-lane encoding ("system" | "light" | "dark").
  val toString: choice: ThemeChoice -> string

  /// Reads a state-lane value; anything unrecognized is None.
  val tryParse: value: string -> ThemeChoice option

module Accent =

  /// The metrino attribute value ("blue" | "red" | ...).
  val toString: accent: Accent -> string

  /// Reads a state-lane or attribute value; anything unrecognized is None.
  val tryParse: value: string -> Accent option

  /// Swatch fill for the accent picker.
  val hex: accent: Accent -> string

  /// Every accent, in picker order.
  val all: Accent list

/// Reactive theme and accent choices. Writes go through `setTheme` /
/// `setAccent`, which apply to the document and persist.
val theme: Var<ThemeChoice>

val accent: Var<Accent>

val setTheme: choice: ThemeChoice -> unit

val setAccent: value: Accent -> unit

/// Restores the stored choices, applies them to the document, and starts the
/// subscriptions that apply and persist later changes. A missing or
/// corrupted lane falls back to the matching default.
val init: storedTheme: string option -> storedAccent: string option -> unit

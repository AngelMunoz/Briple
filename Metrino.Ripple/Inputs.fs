namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Inputs =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-auto-suggest-box` (`@angelmunoz/metrino/auto-suggest-box`).
  [<Import("registerMetroAutoSuggestBox", "@angelmunoz/metrino/auto-suggest-box")>]
  let registerMetroAutoSuggestBox: unit -> unit = jsNative

  /// Register `metro-check-box` (`@angelmunoz/metrino/check-box`).
  [<Import("registerMetroCheckBox", "@angelmunoz/metrino/check-box")>]
  let registerMetroCheckBox: unit -> unit = jsNative

  /// Register `metro-combo-box` (`@angelmunoz/metrino/combo-box`).
  [<Import("registerMetroComboBox", "@angelmunoz/metrino/combo-box")>]
  let registerMetroComboBox: unit -> unit = jsNative

  /// Register `metro-number-box` (`@angelmunoz/metrino/number-box`).
  [<Import("registerMetroNumberBox", "@angelmunoz/metrino/number-box")>]
  let registerMetroNumberBox: unit -> unit = jsNative

  /// Register `metro-password-box` (`@angelmunoz/metrino/password-box`).
  [<Import("registerMetroPasswordBox", "@angelmunoz/metrino/password-box")>]
  let registerMetroPasswordBox: unit -> unit = jsNative

  /// Register `metro-radio-button` (`@angelmunoz/metrino/radio-button`).
  [<Import("registerMetroRadioButton", "@angelmunoz/metrino/radio-button")>]
  let registerMetroRadioButton: unit -> unit = jsNative

  /// Register `metro-rating` (`@angelmunoz/metrino/rating`).
  [<Import("registerMetroRating", "@angelmunoz/metrino/rating")>]
  let registerMetroRating: unit -> unit = jsNative

  /// Register `metro-rich-edit-box` (root export - no dedicated subpath).
  [<Import("registerMetroRichEditBox", "@angelmunoz/metrino")>]
  let registerMetroRichEditBox: unit -> unit = jsNative

  /// Register `metro-slider` (`@angelmunoz/metrino/slider`).
  [<Import("registerMetroSlider", "@angelmunoz/metrino/slider")>]
  let registerMetroSlider: unit -> unit = jsNative

  /// Register `metro-text-box` (`@angelmunoz/metrino/text-box`).
  [<Import("registerMetroTextBox", "@angelmunoz/metrino/text-box")>]
  let registerMetroTextBox: unit -> unit = jsNative

  /// Register `metro-toggle-switch` (`@angelmunoz/metrino/toggle-switch`).
  [<Import("registerMetroToggleSwitch", "@angelmunoz/metrino/toggle-switch")>]
  let registerMetroToggleSwitch: unit -> unit = jsNative

  type Html with

    static member inline metroAutoSuggestBox(args: DomItem list) : DomItem =
      Html.elem "metro-auto-suggest-box" args

    static member inline metroCheckBox(args: DomItem list) : DomItem =
      Html.elem "metro-check-box" args

    static member inline metroComboBox(args: DomItem list) : DomItem =
      Html.elem "metro-combo-box" args

    static member inline metroNumberBox(args: DomItem list) : DomItem =
      Html.elem "metro-number-box" args

    static member inline metroPasswordBox(args: DomItem list) : DomItem =
      Html.elem "metro-password-box" args

    static member inline metroRadioButton(args: DomItem list) : DomItem =
      Html.elem "metro-radio-button" args

    static member inline metroRating(args: DomItem list) : DomItem =
      Html.elem "metro-rating" args

    static member inline metroRichEditBox(args: DomItem list) : DomItem =
      Html.elem "metro-rich-edit-box" args

    static member inline metroSlider(args: DomItem list) : DomItem =
      Html.elem "metro-slider" args

    static member inline metroTextBox(args: DomItem list) : DomItem =
      Html.elem "metro-text-box" args

    static member inline metroToggleSwitch(args: DomItem list) : DomItem =
      Html.elem "metro-toggle-switch" args

  type attr with

    /// Show the password reveal button (password box).
    static member inline revealed(v: bool) : DomItem =
      Base.booleanAttribute "revealed" v

    static member inline revealed(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "revealed" s.get_Value

    /// Current search query (auto suggest box).
    static member inline query(v: string) : DomItem = attr.custom("query", v)

    static member inline query(s: WithGetValueString<'s>) : DomItem =
      attr.custom("query", s.get_Value)

    /// Numeric minimum (number box, slider).
    static member inline min(v: float) : DomItem = attr.custom("min", string v)

    static member inline min(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("min", s.get_Value >> string)

    /// Numeric maximum (number box, slider, rating).
    static member inline max(v: float) : DomItem = attr.custom("max", string v)

    static member inline max(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("max", s.get_Value >> string)

    /// Numeric step (number box, slider).
    static member inline step(v: float) : DomItem =
      attr.custom("step", string v)

    static member inline step(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("step", s.get_Value >> string)

    /// Switch state (toggle switch) - the attribute is literally named `on`.
    static member inline on(v: bool) : DomItem = Base.booleanAttribute "on" v

    static member inline on(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "on" s.get_Value

  (* value: string -> base `attr.value`; value: number -> base `attr.value`;
       readonly -> base `attr.readOnly`; label/name/placeholder/required/disabled
       are shared with the base DSL. *)

  type on with

    /// Text components emit `input` with `{ value: string }`.
    static member inline valueInput(h: TextValueChange -> unit) : DomItem =
      Internal.onCustom "input" h

    /// Number components emit `input` with `{ value: number }`.
    static member inline numberInput(h: NumberValueChange -> unit) : DomItem =
      Internal.onCustom "input" h

    /// Number components commit (`change` with `{ value: number }`).
    static member inline numberChanged(h: NumberValueChange -> unit) : DomItem =
      Internal.onCustom "change" h

    /// `metro-auto-suggest-box`: query text changed (`textchanged`).
    static member inline textChanged(h: TextChanged -> unit) : DomItem =
      Internal.onCustom "textchanged" h

    /// `metro-auto-suggest-box`: suggestion accepted (`suggestionchosen`).
    static member inline suggestionChosen
      (h: SuggestionChosen -> unit)
      : DomItem =
      Internal.onCustom "suggestionchosen" h

    /// `metro-radio-button`: toggle committed (`change` with `{ checked, value }`).
    static member inline radioChanged(h: RadioChange -> unit) : DomItem =
      Internal.onCustom "change" h

    /// `metro-toggle-switch`: flipped (`change` with `{ on }`).
    static member inline toggled(h: ToggledChange -> unit) : DomItem =
      Internal.onCustom "change" h

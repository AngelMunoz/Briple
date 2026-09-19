namespace Metrino.Ripple

[<AutoOpen>]
module Buttons =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-button` (`@angelmunoz/metrino/button`).
  [<Import("registerMetroButton", "@angelmunoz/metrino/button")>]
  let registerMetroButton: unit -> unit = jsNative

  /// Register `metro-dropdown-button` (root export - no dedicated subpath).
  [<Import("registerMetroDropdownButton", "@angelmunoz/metrino")>]
  let registerMetroDropdownButton: unit -> unit = jsNative

  /// Register `metro-hyperlink-button` (`@angelmunoz/metrino/hyperlink-button`).
  [<Import("registerMetroHyperlinkButton",
           "@angelmunoz/metrino/hyperlink-button")>]
  let registerMetroHyperlinkButton: unit -> unit = jsNative

  /// Register `metro-repeat-button` (`@angelmunoz/metrino/repeat-button`).
  [<Import("registerMetroRepeatButton", "@angelmunoz/metrino/repeat-button")>]
  let registerMetroRepeatButton: unit -> unit = jsNative

  type Html with

    static member inline metroButton(args: DomItem list) : DomItem =
      Html.elem "metro-button" args

    static member inline metroDropdownButton(args: DomItem list) : DomItem =
      Html.elem "metro-dropdown-button" args

    static member inline metroHyperlinkButton(args: DomItem list) : DomItem =
      Html.elem "metro-hyperlink-button" args

    static member inline metroRepeatButton(args: DomItem list) : DomItem =
      Html.elem "metro-repeat-button" args

  (* Attributes: disabled/href/target/label/value are shared with the base DSL;
       accent/icon/placement/delay/interval live in Types. *)

  (* Events: click is a native event - use `on.click`. show/hide are shared. *)

namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Dialogs =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-content-dialog` (`@angelmunoz/metrino/content-dialog`).
  [<Import("registerMetroContentDialog", "@angelmunoz/metrino/content-dialog")>]
  let registerMetroContentDialog: unit -> unit = jsNative

  /// Register `metro-flyout` (`@angelmunoz/metrino/flyout`).
  [<Import("registerMetroFlyout", "@angelmunoz/metrino/flyout")>]
  let registerMetroFlyout: unit -> unit = jsNative

  /// Register `metro-message-dialog` (`@angelmunoz/metrino/message-dialog`).
  [<Import("registerMetroMessageDialog", "@angelmunoz/metrino/message-dialog")>]
  let registerMetroMessageDialog: unit -> unit = jsNative

  /// Register `metro-settings-flyout` (root export - no dedicated subpath).
  [<Import("registerMetroSettingsFlyout", "@angelmunoz/metrino")>]
  let registerMetroSettingsFlyout: unit -> unit = jsNative

  type Html with

    static member inline metroContentDialog(args: DomItem list) : DomItem =
      Html.elem "metro-content-dialog" args

    static member inline metroFlyout(args: DomItem list) : DomItem =
      Html.elem "metro-flyout" args

    static member inline metroMessageDialog(args: DomItem list) : DomItem =
      Html.elem "metro-message-dialog" args

    static member inline metroSettingsFlyout(args: DomItem list) : DomItem =
      Html.elem "metro-settings-flyout" args

  type attr with

    /// Settings flyout width (`narrow` | `wide`).
    static member inline width(v: FlyoutWidth) : DomItem =
      attr.custom("width", string v)

    static member inline width(s: WithGetValue<'s, FlyoutWidth>) : DomItem =
      attr.custom("width", s.get_Value >> string)

  (* Events: show/close/open are shared (Types). show/hide via `attr.isOpen` and
       the imperative `MetroContentDialog.show()/hide()` on a captured reference. *)

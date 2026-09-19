namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Tiles =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-cycle-tile` (`@angelmunoz/metrino/cycle-tile`).
  [<Import("registerMetroCycleTile", "@angelmunoz/metrino/cycle-tile")>]
  let registerMetroCycleTile: unit -> unit = jsNative

  /// Register `metro-flip-tile` (`@angelmunoz/metrino/flip-tile`).
  [<Import("registerMetroFlipTile", "@angelmunoz/metrino/flip-tile")>]
  let registerMetroFlipTile: unit -> unit = jsNative

  /// Register `metro-iconic-tile` (`@angelmunoz/metrino/iconic-tile`).
  [<Import("registerMetroIconicTile", "@angelmunoz/metrino/iconic-tile")>]
  let registerMetroIconicTile: unit -> unit = jsNative

  /// Register `metro-live-tile` (`@angelmunoz/metrino/live-tile`).
  [<Import("registerMetroLiveTile", "@angelmunoz/metrino/live-tile")>]
  let registerMetroLiveTile: unit -> unit = jsNative

  type Html with

    static member inline metroCycleTile(args: DomItem list) : DomItem =
      Html.elem "metro-cycle-tile" args

    static member inline metroFlipTile(args: DomItem list) : DomItem =
      Html.elem "metro-flip-tile" args

    static member inline metroIconicTile(args: DomItem list) : DomItem =
      Html.elem "metro-iconic-tile" args

    static member inline metroLiveTile(args: DomItem list) : DomItem =
      Html.elem "metro-live-tile" args

  type attr with

    /// Back face visibility (flip tile).
    static member inline flipped(v: bool) : DomItem =
      Base.booleanAttribute "flipped" v

    static member inline flipped(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "flipped" s.get_Value

    /// Notification counter (iconic tile, live tile).
    static member inline count(v: float) : DomItem =
      attr.custom("count", string v)

    static member inline count(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("count", s.get_Value >> string)

    /// Notification badge text (iconic tile, live tile).
    static member inline badge(v: string) : DomItem = attr.custom("badge", v)

    static member inline badge(s: WithGetValueString<'s>) : DomItem =
      attr.custom("badge", s.get_Value)

  (* size/interval/icon/title are shared (Types) or base attrs; live-tile content
       rotation is `MetroLiveTile.setItems` on a captured reference. *)

  type on with

    /// `metro-flip-tile`: tile flipped (`flipped` with `{ flipped }`).
    static member inline flipped(h: FlippedChange -> unit) : DomItem =
      Internal.onCustom "flipped" h

  (* cycle/iconic tile taps are `on.click`. *)

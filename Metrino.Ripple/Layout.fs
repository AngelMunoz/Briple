namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Layout =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-canvas` (root export - no dedicated subpath).
  [<Import("registerMetroCanvas", "@angelmunoz/metrino")>]
  let registerMetroCanvas: unit -> unit = jsNative

  /// Register `metro-grid` (`@angelmunoz/metrino/grid`).
  [<Import("registerMetroGrid", "@angelmunoz/metrino/grid")>]
  let registerMetroGrid: unit -> unit = jsNative

  /// Register `metro-scroll-viewer` (`@angelmunoz/metrino/scroll-viewer`).
  [<Import("registerMetroScrollViewer", "@angelmunoz/metrino/scroll-viewer")>]
  let registerMetroScrollViewer: unit -> unit = jsNative

  /// Register `metro-stack-panel` (`@angelmunoz/metrino/stack-panel`).
  [<Import("registerMetroStackPanel", "@angelmunoz/metrino/stack-panel")>]
  let registerMetroStackPanel: unit -> unit = jsNative

  /// Register `metro-tile-grid` (`@angelmunoz/metrino/tile-grid`).
  [<Import("registerMetroTileGrid", "@angelmunoz/metrino/tile-grid")>]
  let registerMetroTileGrid: unit -> unit = jsNative

  /// Register `metro-viewbox` (root export - no dedicated subpath).
  [<Import("registerMetroViewbox", "@angelmunoz/metrino")>]
  let registerMetroViewbox: unit -> unit = jsNative

  /// Register `metro-wrap-panel` (`@angelmunoz/metrino/wrap-panel`).
  [<Import("registerMetroWrapPanel", "@angelmunoz/metrino/wrap-panel")>]
  let registerMetroWrapPanel: unit -> unit = jsNative

  /// Register `metro-variable-sized-wrap-grid` (root export - no dedicated subpath).
  [<Import("registerMetroVariableSizedWrapGrid", "@angelmunoz/metrino")>]
  let registerMetroVariableSizedWrapGrid: unit -> unit = jsNative

  type Html with

    static member inline metroCanvas(args: DomItem list) : DomItem =
      Html.elem "metro-canvas" args

    static member inline metroGrid(args: DomItem list) : DomItem =
      Html.elem "metro-grid" args

    static member inline metroScrollViewer(args: DomItem list) : DomItem =
      Html.elem "metro-scroll-viewer" args

    static member inline metroStackPanel(args: DomItem list) : DomItem =
      Html.elem "metro-stack-panel" args

    static member inline metroTileGrid(args: DomItem list) : DomItem =
      Html.elem "metro-tile-grid" args

    static member inline metroViewbox(args: DomItem list) : DomItem =
      Html.elem "metro-viewbox" args

    static member inline metroWrapPanel(args: DomItem list) : DomItem =
      Html.elem "metro-wrap-panel" args

    static member inline metroVariableSizedWrapGrid
      (args: DomItem list)
      : DomItem =
      Html.elem "metro-variable-sized-wrap-grid" args

  type attr with

    /// CSS `grid-template-rows` value (metro grid).
    static member inline rows(v: string) : DomItem = attr.custom("rows", v)

    static member inline rows(s: WithGetValueString<'s>) : DomItem =
      attr.custom("rows", s.get_Value)

    /// CSS `grid-template-columns` (metro grid) or tile count (tile grid).
    static member inline columns(v: string) : DomItem =
      attr.custom("columns", v)

    static member inline columns(v: int) : DomItem =
      attr.custom("columns", string v)

    /// Reactive columns, string or int.
    static member inline columns(s: WithGetValue<'s, 'a>) : DomItem =
      attr.custom("columns", s.get_Value >> string)

    /// Scroll axes (`scrollOrientation` attribute - camelCase, set as property).
    static member inline scrollOrientation(v: ScrollOrientation) : DomItem =
      Base.property "scrollOrientation" (string v)

    static member inline scrollOrientation
      (s: WithGetValue<'s, ScrollOrientation>)
      : DomItem =
      Base.bindProperty "scrollOrientation" (s.get_Value >> string)

    /// Scrollbar visibility (`scrollbarMode` attribute - camelCase, set as property).
    static member inline scrollbarMode(v: ScrollbarMode) : DomItem =
      Base.property "scrollbarMode" (string v)

    static member inline scrollbarMode
      (s: WithGetValue<'s, ScrollbarMode>)
      : DomItem =
      Base.bindProperty "scrollbarMode" (s.get_Value >> string)

    /// Momentum/touch scrolling.
    static member inline touchPhysics(v: bool) : DomItem =
      Base.booleanAttribute "touch-physics" v

    static member inline touchPhysics(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "touch-physics" s.get_Value

    /// Cell width in px (variable-sized wrap grid, grid view).
    static member inline itemWidth(v: float) : DomItem =
      attr.custom("item-width", string v)

    static member inline itemWidth(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("item-width", s.get_Value >> string)

    /// Cell height in px (variable-sized wrap grid, list/grid views).
    static member inline itemHeight(v: float) : DomItem =
      attr.custom("item-height", string v)

    static member inline itemHeight(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("item-height", s.get_Value >> string)

    /// Row/column cap, `-1` = unbounded (variable-sized wrap grid).
    static member inline maximumRowsOrColumns(v: float) : DomItem =
      attr.custom("maximum-rows-or-columns", string v)

    static member inline maximumRowsOrColumns
      (s: WithGetValueFloat<'s>)
      : DomItem =
      attr.custom("maximum-rows-or-columns", s.get_Value >> string)

    /// Scaling axes (viewbox).
    static member inline stretchDirection(v: StretchDirection) : DomItem =
      attr.custom("stretch-direction", string v)

    static member inline stretchDirection
      (s: WithGetValue<'s, StretchDirection>)
      : DomItem =
      attr.custom("stretch-direction", s.get_Value >> string)

  (* orientation is shared (Types); stretch is shared (Types). *)

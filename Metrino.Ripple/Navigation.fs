namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Navigation =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-app-bar` (`@angelmunoz/metrino/app-bar`).
  [<Import("registerMetroAppBar", "@angelmunoz/metrino/app-bar")>]
  let registerMetroAppBar: unit -> unit = jsNative

  /// Register `metro-app-bar-button` (`@angelmunoz/metrino/app-bar-button`).
  [<Import("registerMetroAppBarButton", "@angelmunoz/metrino/app-bar-button")>]
  let registerMetroAppBarButton: unit -> unit = jsNative

  /// Register `metro-app-bar-separator` (root export - no dedicated subpath).
  [<Import("registerMetroAppBarSeparator", "@angelmunoz/metrino")>]
  let registerMetroAppBarSeparator: unit -> unit = jsNative

  /// Register `metro-app-bar-toggle-button` (root export - no dedicated subpath).
  [<Import("registerMetroAppBarToggleButton", "@angelmunoz/metrino")>]
  let registerMetroAppBarToggleButton: unit -> unit = jsNative

  /// Register `metro-hub` (`@angelmunoz/metrino/hub`).
  [<Import("registerMetroHub", "@angelmunoz/metrino/hub")>]
  let registerMetroHub: unit -> unit = jsNative

  /// Register `metro-hub-section` (`@angelmunoz/metrino/hub-section`).
  [<Import("registerMetroHubSection", "@angelmunoz/metrino/hub-section")>]
  let registerMetroHubSection: unit -> unit = jsNative

  /// Register `metro-panorama` (`@angelmunoz/metrino/panorama`).
  [<Import("registerMetroPanorama", "@angelmunoz/metrino/panorama")>]
  let registerMetroPanorama: unit -> unit = jsNative

  /// Register `metro-panorama-item` (`@angelmunoz/metrino/panorama-item`).
  [<Import("registerMetroPanoramaItem", "@angelmunoz/metrino/panorama-item")>]
  let registerMetroPanoramaItem: unit -> unit = jsNative

  /// Register `metro-pivot` (`@angelmunoz/metrino/pivot`).
  [<Import("registerMetroPivot", "@angelmunoz/metrino/pivot")>]
  let registerMetroPivot: unit -> unit = jsNative

  /// Register `metro-pivot-item` (`@angelmunoz/metrino/pivot-item`).
  [<Import("registerMetroPivotItem", "@angelmunoz/metrino/pivot-item")>]
  let registerMetroPivotItem: unit -> unit = jsNative

  /// Register `metro-split-view` (`@angelmunoz/metrino/split-view`).
  [<Import("registerMetroSplitView", "@angelmunoz/metrino/split-view")>]
  let registerMetroSplitView: unit -> unit = jsNative

  type Html with

    static member inline metroAppBar(args: DomItem list) : DomItem =
      Html.elem "metro-app-bar" args

    static member inline metroAppBarButton(args: DomItem list) : DomItem =
      Html.elem "metro-app-bar-button" args

    static member inline metroAppBarSeparator(args: DomItem list) : DomItem =
      Html.elem "metro-app-bar-separator" args

    static member inline metroAppBarToggleButton(args: DomItem list) : DomItem =
      Html.elem "metro-app-bar-toggle-button" args

    static member inline metroHub(args: DomItem list) : DomItem =
      Html.elem "metro-hub" args

    static member inline metroHubSection(args: DomItem list) : DomItem =
      Html.elem "metro-hub-section" args

    static member inline metroPanorama(args: DomItem list) : DomItem =
      Html.elem "metro-panorama" args

    static member inline metroPanoramaItem(args: DomItem list) : DomItem =
      Html.elem "metro-panorama-item" args

    static member inline metroPivot(args: DomItem list) : DomItem =
      Html.elem "metro-pivot" args

    static member inline metroPivotItem(args: DomItem list) : DomItem =
      Html.elem "metro-pivot-item" args

    static member inline metroSplitView(args: DomItem list) : DomItem =
      Html.elem "metro-split-view" args

  type attr with

    /// Item header (hub section, panorama item, pivot item).
    static member inline header(v: string) : DomItem = attr.custom("header", v)

    static member inline header(s: WithGetValueString<'s>) : DomItem =
      attr.custom("header", s.get_Value)

    /// Panorama backdrop image URL.
    static member inline backgroundImage(v: string) : DomItem =
      attr.custom("background-image", v)

    static member inline backgroundImage(s: WithGetValueString<'s>) : DomItem =
      attr.custom("background-image", s.get_Value)

    /// Separator visibility.
    static member inline visible(v: bool) : DomItem =
      Base.booleanAttribute "visible" v

    static member inline visible(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "visible" s.get_Value

    /// Render as a menu item (app bar toggle button).
    static member inline menuItem(v: bool) : DomItem =
      Base.booleanAttribute "menu-item" v

    static member inline menuItem(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "menu-item" s.get_Value

    /// Active pivot item (camelCase attribute - set as property).
    static member inline selectedIndex(v: float) : DomItem =
      Base.property "selectedIndex" v

    static member inline selectedIndex(s: WithGetValueFloat<'s>) : DomItem =
      Base.bindProperty "selectedIndex" s.get_Value

    /// Split view display mode.
    static member inline displayMode(v: DisplayMode) : DomItem =
      attr.custom("display-mode", string v)

    static member inline displayMode
      (s: WithGetValue<'s, DisplayMode>)
      : DomItem =
      attr.custom("display-mode", s.get_Value >> string)

    /// Split view pane side.
    static member inline panePlacement(v: Side) : DomItem =
      attr.custom("pane-placement", string v)

    static member inline panePlacement(s: WithGetValue<'s, Side>) : DomItem =
      attr.custom("pane-placement", s.get_Value >> string)

  (* expanded (app bar) and checked/checkedChanged (app bar toggle button) are
       shared (Types); open is base `attr.isOpen`; title/label/icon are base. *)

  type on with

    /// `metro-pivot`: active item changed (`selectionchanged`).
    static member inline pivotSelectionChanged
      (h: PivotSelectionChanged -> unit)
      : DomItem =
      Internal.onCustom "selectionchanged" h

    /// `metro-split-view`: pane became visible (`paneopened`).
    static member inline paneOpened(h: unit -> unit) : DomItem =
      on.event("paneopened", fun _ -> h())

    /// `metro-split-view`: pane became hidden (`paneclosed`).
    static member inline paneClosed(h: unit -> unit) : DomItem =
      on.event("paneclosed", fun _ -> h())

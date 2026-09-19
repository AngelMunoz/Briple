namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Selection =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-flip-view` (root export - no dedicated subpath).
  [<Import("registerMetroFlipView", "@angelmunoz/metrino")>]
  let registerMetroFlipView: unit -> unit = jsNative

  /// Register `metro-grid-view` (root export - no dedicated subpath).
  [<Import("registerMetroGridView", "@angelmunoz/metrino")>]
  let registerMetroGridView: unit -> unit = jsNative

  /// Register `metro-list-box` (`@angelmunoz/metrino/list-box`).
  [<Import("registerMetroListBox", "@angelmunoz/metrino/list-box")>]
  let registerMetroListBox: unit -> unit = jsNative

  /// Register `metro-list-picker` (root export - no dedicated subpath).
  [<Import("registerMetroListPicker", "@angelmunoz/metrino")>]
  let registerMetroListPicker: unit -> unit = jsNative

  /// Register `metro-list-view` (root export - no dedicated subpath).
  [<Import("registerMetroListView", "@angelmunoz/metrino")>]
  let registerMetroListView: unit -> unit = jsNative

  /// Register `metro-long-list-selector` (`@angelmunoz/metrino/long-list-selector`).
  [<Import("registerMetroLongListSelector",
           "@angelmunoz/metrino/long-list-selector")>]
  let registerMetroLongListSelector: unit -> unit = jsNative

  /// Register `metro-semantic-zoom` (`@angelmunoz/metrino/semantic-zoom`).
  [<Import("registerMetroSemanticZoom", "@angelmunoz/metrino/semantic-zoom")>]
  let registerMetroSemanticZoom: unit -> unit = jsNative

  /// Register `metro-tree-view` (root export - no dedicated subpath).
  [<Import("registerMetroTreeView", "@angelmunoz/metrino")>]
  let registerMetroTreeView: unit -> unit = jsNative

  type Html with

    static member inline metroFlipView(args: DomItem list) : DomItem =
      Html.elem "metro-flip-view" args

    static member inline metroGridView(args: DomItem list) : DomItem =
      Html.elem "metro-grid-view" args

    static member inline metroListBox(args: DomItem list) : DomItem =
      Html.elem "metro-list-box" args

    static member inline metroListPicker(args: DomItem list) : DomItem =
      Html.elem "metro-list-picker" args

    static member inline metroListView(args: DomItem list) : DomItem =
      Html.elem "metro-list-view" args

    static member inline metroLongListSelector(args: DomItem list) : DomItem =
      Html.elem "metro-long-list-selector" args

    static member inline metroSemanticZoom(args: DomItem list) : DomItem =
      Html.elem "metro-semantic-zoom" args

    static member inline metroTreeView(args: DomItem list) : DomItem =
      Html.elem "metro-tree-view" args

  type attr with

    /// List contents. The `items` property must be set as a JS property (arrays
    /// do not survive attribute serialization). Plain data lists take `obj array`.
    static member inline items(v: obj array) : DomItem =
      Base.property "items" (box v)

    /// Tree nodes (`metro-tree-view`).
    static member inline items(v: TreeViewItem array) : DomItem =
      Base.property "items" (box v)

    /// `metro-list-picker` - plain strings.
    static member inline items(v: string array) : DomItem =
      Base.property "items" (box v)

    /// `metro-list-picker` - labeled items.
    static member inline items(v: ListPickerItem array) : DomItem =
      Base.property "items" (box v)

    /// Reactive contents, any item flavor.
    static member inline items(s: WithGetValue<'s, 'a>) : DomItem =
      Base.bindProperty "items" (s.get_Value >> box)

    /// Selection behavior.
    static member inline selectionMode(v: SelectionMode) : DomItem =
      attr.custom("selection-mode", string v)

    static member inline selectionMode
      (s: WithGetValue<'s, SelectionMode>)
      : DomItem =
      attr.custom("selection-mode", s.get_Value >> string)

    /// Grouping key for grouped rendering (list/grid views, long list selector).
    static member inline groupKey(v: string) : DomItem =
      attr.custom("group-key", v)

    static member inline groupKey(s: WithGetValueString<'s>) : DomItem =
      attr.custom("group-key", s.get_Value)

    /// Group header row height, px (list view).
    static member inline groupHeaderHeight(v: float) : DomItem =
      attr.custom("group-header-height", string v)

    static member inline groupHeaderHeight(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("group-header-height", s.get_Value >> string)

    /// Item property read for the label.
    static member inline displayMember(v: string) : DomItem =
      attr.custom("display-member", v)

    static member inline displayMember(s: WithGetValueString<'s>) : DomItem =
      attr.custom("display-member", s.get_Value)

    /// Item property read for the value (long list selector).
    static member inline valueMember(v: string) : DomItem =
      attr.custom("value-member", v)

    static member inline valueMember(s: WithGetValueString<'s>) : DomItem =
      attr.custom("value-member", s.get_Value)

    /// Selected value (long list selector - arbitrary payload, set as property).
    static member inline selectedValue(v: obj) : DomItem =
      Base.property "selectedValue" v

    static member inline selectedValue(s: WithGetValue<'s, obj>) : DomItem =
      Base.bindProperty "selectedValue" s.get_Value

    /// Collapsed height (long list selector).
    static member inline maxHeight(v: string) : DomItem =
      attr.custom("max-height", v)

    static member inline maxHeight(s: WithGetValueString<'s>) : DomItem =
      attr.custom("max-height", s.get_Value)

    /// Jump list strip (long list selector).
    static member inline showJumpList(v: bool) : DomItem =
      Base.booleanAttribute "show-jump-list" v

    static member inline showJumpList(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "show-jump-list" s.get_Value

    /// Prev/next arrows (flip view).
    static member inline showNav(v: bool) : DomItem =
      Base.booleanAttribute "show-nav" v

    static member inline showNav(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "show-nav" s.get_Value

    /// Position dots (flip view).
    static member inline showIndicators(v: bool) : DomItem =
      Base.booleanAttribute "show-indicators" v

    static member inline showIndicators(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "show-indicators" s.get_Value

    /// Active slide (flip view).
    static member inline index(v: float) : DomItem =
      attr.custom("index", string v)

    static member inline index(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("index", s.get_Value >> string)

    /// Selected node id (`metro-tree-view`).
    static member inline selected(v: string) : DomItem =
      attr.custom("selected", v)

    static member inline selected(s: WithGetValueString<'s>) : DomItem =
      attr.custom("selected", s.get_Value)

    /// Selected item position (`metro-list-picker`, kebab attribute).
    static member inline selectedIndex(v: float) : DomItem =
      attr.custom("selected-index", string v)

    static member inline selectedIndex(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("selected-index", s.get_Value >> string)

    /// Zoom state (`metro-semantic-zoom`).
    static member inline zoomed(v: ZoomState) : DomItem =
      attr.custom("zoomed", string v)

    static member inline zoomed(s: WithGetValue<'s, ZoomState>) : DomItem =
      attr.custom("zoomed", s.get_Value >> string)

    /// Zoom animation length, ms (`metro-semantic-zoom`).
    static member inline transitionDuration(v: float) : DomItem =
      attr.custom("transition-duration", string v)

    static member inline transitionDuration
      (s: WithGetValueFloat<'s>)
      : DomItem =
      attr.custom("transition-duration", s.get_Value >> string)

  (* item-width/item-height/columns are shared with Layout; open/title/disabled/
       name are base or shared members. *)

  type on with

    /// An item was clicked (list/grid views, flip view, long list selector).
    static member inline itemClick(h: ItemEvent -> unit) : DomItem =
      Internal.onCustom "itemclick" h

    /// An item was activated (list/grid views).
    static member inline itemInvoke(h: ItemEvent -> unit) : DomItem =
      Internal.onCustom "iteminvoke" h

    /// `metro-list-view` / `metro-grid-view` selection changed (`selectionchange`).
    static member inline selectionChange(h: SelectionChange -> unit) : DomItem =
      Internal.onCustom "selectionchange" h

    /// `metro-list-box` selection changed (`selectionchanged`).
    static member inline listBoxSelectionChanged
      (h: ListBoxSelectionChanged -> unit)
      : DomItem =
      Internal.onCustom "selectionchanged" h

    /// `metro-long-list-selector` selection changed (`selectionchange`).
    static member inline longListSelectionChange
      (h: LongListSelectionChange -> unit)
      : DomItem =
      Internal.onCustom "selectionchange" h

    /// `metro-tree-view` selection changed (`selectionchange`).
    static member inline treeViewSelectionChanged
      (h: TreeViewSelectionChanged -> unit)
      : DomItem =
      Internal.onCustom "selectionchange" h

    /// `metro-tree-view`: node expanded (`expand`).
    static member inline itemExpanded(h: TreeItemIdChange -> unit) : DomItem =
      Internal.onCustom "expand" h

    /// `metro-tree-view`: node collapsed (`collapse`).
    static member inline itemCollapsed(h: TreeItemIdChange -> unit) : DomItem =
      Internal.onCustom "collapse" h

    /// `metro-flip-view`: slide changed (`change` with `{ index }`).
    static member inline indexChanged(h: IndexChange -> unit) : DomItem =
      Internal.onCustom "change" h

    /// `metro-list-picker`: item picked (`change`).
    static member inline listPickerChanged
      (h: ListPickerChange -> unit)
      : DomItem =
      Internal.onCustom "change" h

    /// `metro-semantic-zoom`: zoom state flipped (`zoomchanged`).
    static member inline zoomChanged(h: ZoomChanged -> unit) : DomItem =
      Internal.onCustom "zoomchanged" h

  (* list-picker open/close use the shared `on.open`/`on.close`;
       list-box select-all/clear and tree expand/collapse are on MetroListBox /
       MetroTreeView interfaces. *)

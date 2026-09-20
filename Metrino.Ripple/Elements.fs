// Typed views of the metrino element classes that expose imperative members.
// Capture a reference with `attr.ref` and cast to the interface:
//
// `attr.ref(fun el -> dialog <- el :?> MetroContentDialog)`
namespace Metrino.Ripple

open Metrino.Ripple



open Browser.Types
open Fable.Core

/// `metro-dropdown-button`.
[<AllowNullLiteral>]
type MetroDropdownButton =
  inherit HTMLElement
  abstract click: unit -> unit
  abstract show: unit -> unit
  abstract hide: unit -> unit

/// `metro-repeat-button`.
[<AllowNullLiteral>]
type MetroRepeatButton =
  inherit HTMLElement
  abstract click: unit -> unit

/// `metro-content-dialog`.
[<AllowNullLiteral>]
type MetroContentDialog =
  inherit HTMLElement
  abstract show: unit -> JS.Promise<unit>
  abstract hide: unit -> JS.Promise<unit>

/// `metro-message-dialog`.
[<AllowNullLiteral>]
type MetroMessageDialog =
  inherit HTMLElement
  abstract show: unit -> JS.Promise<unit>
  abstract hide: unit -> JS.Promise<unit>

/// `metro-settings-flyout`.
[<AllowNullLiteral>]
type MetroSettingsFlyout =
  inherit HTMLElement
  abstract show: unit -> JS.Promise<unit>
  abstract hide: unit -> JS.Promise<unit>

/// `metro-flyout`.
[<AllowNullLiteral>]
type MetroFlyout =
  inherit HTMLElement
  abstract show: target: Element -> JS.Promise<unit>
  abstract hide: unit -> JS.Promise<unit>

/// `metro-list-picker`.
[<AllowNullLiteral>]
type MetroListPicker =
  inherit HTMLElement
  abstract show: unit -> unit
  abstract hide: unit -> unit

/// `metro-split-view`.
[<AllowNullLiteral>]
type MetroSplitView =
  inherit HTMLElement
  abstract toggle: unit -> unit
  abstract show: unit -> unit
  abstract hide: unit -> unit

/// `metro-hub`.
[<AllowNullLiteral>]
type MetroHub =
  inherit HTMLElement
  /// The slotted `metro-hub-section` elements, in document order.
  abstract sections: HTMLElement array
  /// Section whose inline start is nearest the scroll position (-1 when
  /// empty). The setter scrolls that section to the container gutter - a
  /// scroll, not a state commit.
  abstract selectedIndex: int with get, set
  /// Scroll a section's inline start to the container gutter; `smooth` by
  /// default (Metro easing tokens), `auto` jumps, reduced motion always
  /// jumps. The index is clamped into range.
  abstract scrollToSection: index: float * ?behavior: ScrollBehavior -> unit

/// `metro-context-menu`.
[<AllowNullLiteral>]
type MetroContextMenu =
  inherit HTMLElement
  abstract show: x: float * y: float -> unit
  abstract hide: unit -> unit

/// `metro-menu-flyout`.
[<AllowNullLiteral>]
type MetroMenuFlyout =
  inherit HTMLElement
  abstract show: target: Element * ?x: float * ?y: float -> unit

/// `metro-tooltip`.
[<AllowNullLiteral>]
type MetroTooltip =
  inherit HTMLElement
  abstract show: target: Element -> unit
  abstract hide: unit -> unit
  abstract showDelayed: target: Element * delay: float -> unit
  abstract hideImmediate: unit -> unit

/// `metro-toast` instance.
[<AllowNullLiteral>]
type MetroToast =
  inherit HTMLElement
  abstract show: options: ToastOptions -> string
  abstract hide: id: string -> unit
  abstract clearAll: unit -> unit

/// `metro-combo-box`.
[<AllowNullLiteral>]
type MetroComboBox =
  inherit HTMLElement
  abstract setOptions: options: string array -> unit

/// `metro-auto-suggest-box`.
[<AllowNullLiteral>]
type MetroAutoSuggestBox =
  inherit HTMLElement
  abstract setSuggestions: suggestions: string array -> unit

/// `metro-media-element`.
[<AllowNullLiteral>]
type MetroMediaElement =
  inherit HTMLElement
  abstract play: unit -> unit
  abstract pause: unit -> unit
  abstract seek: time: float -> unit

/// `metro-flip-view`.
[<AllowNullLiteral>]
type MetroFlipView =
  inherit HTMLElement
  abstract goTo: index: float -> unit
  abstract next: unit -> unit
  abstract prev: unit -> unit

/// `metro-list-view`.
[<AllowNullLiteral>]
type MetroListView =
  inherit HTMLElement
  abstract getSelectedItems: unit -> obj array
  abstract getSelectedIndices: unit -> int array
  abstract clearSelection: unit -> unit
  abstract scrollToIndex: index: float -> unit

/// `metro-grid-view`.
[<AllowNullLiteral>]
type MetroGridView =
  inherit HTMLElement
  abstract getSelectedItems: unit -> obj array
  abstract getSelectedIndices: unit -> int array
  abstract clearSelection: unit -> unit
  abstract scrollToIndex: index: float -> unit

/// `metro-long-list-selector`.
[<AllowNullLiteral>]
type MetroLongListSelector =
  inherit HTMLElement
  abstract getSelectedItems: unit -> obj array
  abstract getSelectedIndices: unit -> int array
  abstract clearSelection: unit -> unit
  abstract scrollToIndex: index: float -> unit
  abstract setItems: items: obj array -> unit

/// `metro-list-box`.
[<AllowNullLiteral>]
type MetroListBox =
  inherit HTMLElement
  abstract selectAll: unit -> unit
  abstract clearSelection: unit -> unit

/// `metro-tree-view`.
[<AllowNullLiteral>]
type MetroTreeView =
  inherit HTMLElement
  abstract expandAll: unit -> unit
  abstract collapseAll: unit -> unit
  abstract expandItem: id: string -> unit
  abstract collapseItem: id: string -> unit

/// `metro-semantic-zoom`.
[<AllowNullLiteral>]
type MetroSemanticZoom =
  inherit HTMLElement
  abstract setZoomedState: state: ZoomState -> JS.Promise<unit>

/// `metro-live-tile`.
[<AllowNullLiteral>]
type MetroLiveTile =
  inherit HTMLElement
  abstract setItems: items: LiveTileItem array -> unit

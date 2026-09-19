namespace Metrino.Ripple
(* Shared vocabulary of the metrino API surface: attribute-value enums (erased to
   their lowercase string form at runtime), the record types crossing the JS
   boundary in either direction, and the attr/on members shared by several
   components (defined once here so category files never redeclare them). *)
open System
open Browser.Types
open Fable.Core
open Fable.Ripple.Dom

(*
      Attribute-value enums (compile to their string attribute values)
  *)

/// Flyout/tooltip placement: `top` | `bottom` | `left` | `right`.
[<StringEnum(CaseRules.LowerFirst)>]
type Placement =
  | Top
  | Bottom
  | Left
  | Right

/// Up/down-only placement (dropdown button, app bar): `top` | `bottom`.
[<StringEnum(CaseRules.LowerFirst)>]
type VerticalPlacement =
  | Top
  | Bottom

/// Stack/wrap panel orientation: `horizontal` | `vertical`.
[<StringEnum(CaseRules.LowerFirst)>]
type Orientation =
  | Horizontal
  | Vertical

/// Scroll viewer axes: `horizontal` | `vertical` | `both`.
[<StringEnum(CaseRules.LowerFirst)>]
type ScrollOrientation =
  | Horizontal
  | Vertical
  | Both

/// Scroll viewer scrollbar visibility: `auto` | `visible` | `hidden`.
[<StringEnum(CaseRules.LowerFirst)>]
type ScrollbarMode =
  | Auto
  | Visible
  | Hidden

/// Viewbox/image scaling: `none` | `fill` | `uniform` | `uniformToFill`.
[<StringEnum(CaseRules.LowerFirst)>]
type Stretch =
  | [<CompiledName "none">] NoStretch
  | Fill
  | Uniform
  | UniformToFill

/// Viewbox scaling axes: `upOnly` | `downOnly` | `both`.
[<StringEnum(CaseRules.LowerFirst)>]
type StretchDirection =
  | UpOnly
  | DownOnly
  | Both

/// Split view display mode: `overlay` | `inline` | `compact`.
[<StringEnum(CaseRules.LowerFirst)>]
type DisplayMode =
  | Overlay
  | Inline
  | Compact

/// Split view pane side: `left` | `right`.
[<StringEnum(CaseRules.LowerFirst)>]
type Side =
  | Left
  | Right

/// Info-bar/toast severity: `informational` | `success` | `warning` | `error`.
[<StringEnum(CaseRules.LowerFirst)>]
type Severity =
  | Informational
  | Success
  | Warning
  | Error

/// Rich text block trimming: `none` | `clip` | `ellipsis`.
[<StringEnum(CaseRules.LowerFirst)>]
type TextTrimming =
  | [<CompiledName "none">] NoTrimming
  | Clip
  | Ellipsis

/// Icon sizing: `small` | `normal` | `medium` | `large` | `xlarge`
/// (person picture uses small/normal/large/xlarge, progress ring
/// small/normal/large - pass only the values the component documents).
[<StringEnum(CaseRules.LowerFirst)>]
type IconSize =
  | Small
  | Normal
  | Medium
  | Large
  | XLarge

/// Tile sizing: `small` | `medium` | `wide` | `large`
/// (cycle tile accepts medium/wide only).
[<StringEnum(CaseRules.LowerFirst)>]
type TileSize =
  | Small
  | Medium
  | Wide
  | Large

/// Media element kind: `audio` | `video`.
[<StringEnum(CaseRules.LowerFirst)>]
type MediaType =
  | Audio
  | Video

/// Person picture presence badge: `available` | `away` | `busy` | `offline`.
[<StringEnum(CaseRules.LowerFirst)>]
type Presence =
  | Available
  | Away
  | Busy
  | Offline

/// Semantic zoom state: `in` | `out`.
[<StringEnum(CaseRules.LowerFirst)>]
type ZoomState =
  | In
  | Out

/// Settings flyout width: `narrow` | `wide`.
[<StringEnum(CaseRules.LowerFirst)>]
type FlyoutWidth =
  | Narrow
  | Wide

/// Selection list mode: `none` | `single` | `multiple` | `extended`
/// (tree view accepts none/single only).
[<StringEnum(CaseRules.LowerFirst)>]
type SelectionMode =
  | [<CompiledName "none">] NoSelection
  | Single
  | Multiple
  | Extended

/// Metro accent palette (the `accent` attribute of `metro-button` /
/// `metro-repeat-button`, and the document-level `<html accent>` theme hook).
/// `Blue` is the default. Unknown attribute values are accepted by the
/// components but have no styling behind them - see the `[accent=...]`
/// rules in metrino's tokens.css.
[<StringEnum(CaseRules.LowerFirst)>]
type AccentColor =
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

(*
      Outbound records - constructed in F# and assigned to component properties.
      Field names are lowercase on purpose: Fable compiles records to plain JS
      objects with the declared field names, which are the names metrino reads.
  *)

/// `metro-tree-view` node.
type TreeViewItem = {
  id: string
  label: string
  icon: string option
  children: TreeViewItem array option
  expanded: bool option
}

/// `metro-list-picker` item - the object variant of `string | { label, value, icon? }`
/// (plain strings are accepted by `attr.items` directly).
type ListPickerItem = {
  label: string
  value: string
  icon: string option
}

/// `metro-live-tile` rotation entry (`setItems`).
type LiveTileItem = {
  title: string option
  message: string option
}

/// `metro-toast` options (`showToast` / `MetroToast.show`). `duration` is
/// milliseconds; `0` keeps the toast persistent.
type ToastOptions = {
  title: string option
  message: string
  severity: Severity option
  duration: float option
}

(*
      Inbound records - event `detail` payloads, unboxed straight from the
      CustomEvent, so their field names match the JS detail keys exactly.
  *)

/// `metro-text-box` / `metro-password-box` / `metro-rich-edit-box` and the
/// date/time pickers: `input`/`change` detail `{ value: string }`.
type TextValueChange = { value: string }

/// `metro-number-box` / `metro-slider` / `metro-rating`: `input`/`change`
/// detail `{ value: number }`.
type NumberValueChange = { value: float }

/// `metro-check-box` / `metro-app-bar-toggle-button`: `change` detail
/// `{ checked: boolean }`.
type CheckedChange = { ``checked``: bool }

/// `metro-radio-button`: `change` detail `{ checked: boolean, value: string }`.
type RadioChange = { ``checked``: bool; value: string }

/// `metro-toggle-switch`: `change` detail `{ on: boolean }`.
type ToggledChange = { on: bool }

/// `metro-calendar-date-picker`: `change` detail `{ value: string, date: Date }`.
type DateValueChange = { value: string; date: DateTime }

/// `metro-calendar`: `dateselected` detail `{ date: Date, value: string }`.
type DateSelected = { date: DateTime; value: string }

/// `metro-calendar`: `displaymonthchanged` detail `{ year, month }`.
type MonthChanged = { year: int; month: int }

/// `metro-auto-suggest-box`: `textchanged` detail `{ text: string }`.
type TextChanged = { text: string }

/// `metro-auto-suggest-box`: `suggestionchosen` detail `{ selectedValue: string }`.
type SuggestionChosen = { selectedValue: string }

/// `metro-combo-box`: `selectionchanged` detail
/// `{ selectedValue: string, selectedIndex: number }`.
type SelectionChanged = {
  selectedValue: string
  selectedIndex: int
}

/// `metro-pivot`: `selectionchanged` detail `{ selectedIndex: number }`.
type PivotSelectionChanged = { selectedIndex: int }

/// `metro-list-box`: `selectionchanged` detail
/// `{ selectedIndices, selectedItems, selectedValues }`.
type ListBoxSelectionChanged = {
  selectedIndices: int array
  selectedItems: obj array
  selectedValues: string array
}

/// `metro-list-view` / `metro-grid-view`: `selectionchange` detail
/// `{ selectedItems, selectedIndices }`.
type SelectionChange = {
  selectedItems: obj array
  selectedIndices: int array
}

/// `metro-long-list-selector`: `selectionchange` detail
/// `{ selectedItems, selectedIndices, selectedValue }`.
type LongListSelectionChange = {
  selectedItems: obj array
  selectedIndices: int array
  selectedValue: obj
}

/// `metro-tree-view`: `selectionchange` detail `{ selectedId: string, item: TreeViewItem }`.
type TreeViewSelectionChanged = {
  selectedId: string
  item: TreeViewItem
}

/// `metro-tree-view`: `expand`/`collapse` detail `{ id: string }`.
type TreeItemIdChange = { id: string }

/// `metro-expander`: `expanded` detail `{ expanded: boolean }`.
type ExpandedChange = { expanded: bool }

/// `metro-flip-tile`: `flipped` detail `{ flipped: boolean }`.
type FlippedChange = { flipped: bool }

/// List `itemclick`/`iteminvoke` detail `{ item, index }`.
type ItemEvent = { item: obj; index: int }

/// `metro-context-menu`: `itemclick` detail `{ item }`.
type MenuItemClick = { item: obj }

/// `metro-flip-view`: `change` detail `{ index: number }`.
type IndexChange = { index: int }

/// `metro-list-picker`: `change` detail `{ index: number, item }`.
type ListPickerChange = { index: int; item: obj }

/// `metro-semantic-zoom`: `zoomchanged` detail `{ zoomed, previous }`.
type ZoomChanged = {
  zoomed: ZoomState
  previous: ZoomState
}

/// `metro-media-element`: `play`/`pause`/`ended` detail `{ originalEvent }`.
type MediaPlaybackEvent = { originalEvent: Event }

/// `metro-media-element`: `timeupdate` detail
/// `{ currentTime, duration, originalEvent }`.
type MediaTimeUpdate = {
  currentTime: float
  duration: float
  originalEvent: Event
}

/// `metro-media-element`: `volumechange` detail
/// `{ volume, muted, originalEvent }`.
type MediaVolumeChange = {
  volume: float
  muted: bool
  originalEvent: Event
}

[<AutoOpen>]
module Types =

  (*
        Attributes shared by several components - declared once, here.
    *)

  type attr with

    /// `metro-button` / `metro-repeat-button` accent color.
    static member inline accent(v: string) : DomItem = attr.custom("accent", v)

    static member inline accent(s: WithGetValueString<'s>) : DomItem =
      attr.custom("accent", s.get_Value)

    /// Accent palette color. Inline so it erases beside the string overload
    /// (the enum compiles to its attribute string).
    static member inline accent(v: AccentColor) : DomItem =
      attr.custom("accent", string v)

    /// Semantic icon name (dropdown/app-bar buttons, iconic tile).
    static member inline icon(v: string) : DomItem = attr.custom("icon", v)

    static member inline icon(s: WithGetValueString<'s>) : DomItem =
      attr.custom("icon", s.get_Value)

    /// Hold delay in ms (repeat button, context menu).
    static member inline delay(v: float) : DomItem =
      attr.custom("delay", string v)

    static member inline delay(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("delay", s.get_Value >> string)

    /// Rotation/auto-advance interval in ms (repeat button, flip view, tiles).
    static member inline interval(v: float) : DomItem =
      attr.custom("interval", string v)

    static member inline interval(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("interval", s.get_Value >> string)

    /// Flyout/tooltip placement (`top` | `bottom` | `left` | `right`).
    static member inline placement(v: Placement) : DomItem =
      attr.custom("placement", string v)

    /// Dropdown button/app bar placement (`top` | `bottom`).
    static member inline placement(v: VerticalPlacement) : DomItem =
      attr.custom("placement", string v)

    /// Reactive placement, any enum flavor.
    static member inline placement(s: WithGetValue<'s, 'a>) : DomItem =
      attr.custom("placement", s.get_Value >> string)

    /// Stack/wrap/variable-sized-wrap grid orientation.
    static member inline orientation(v: Orientation) : DomItem =
      attr.custom("orientation", string v)

    static member inline orientation
      (s: WithGetValue<'s, Orientation>)
      : DomItem =
      attr.custom("orientation", s.get_Value >> string)

    /// Icon/person/progress sizing.
    static member inline size(v: IconSize) : DomItem =
      attr.custom("size", string v)

    /// Tile sizing.
    static member inline size(v: TileSize) : DomItem =
      attr.custom("size", string v)

    /// Reactive size, any enum flavor.
    static member inline size(s: WithGetValue<'s, 'a>) : DomItem =
      attr.custom("size", s.get_Value >> string)

    /// Show a close affordance (content dialog, info bar).
    static member inline closable(v: bool) : DomItem =
      Base.booleanAttribute "closable" v

    static member inline closable(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "closable" s.get_Value

    /// Expanded state (app bar, expander).
    static member inline expanded(v: bool) : DomItem =
      Base.booleanAttribute "expanded" v

    static member inline expanded(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "expanded" s.get_Value

    /// Viewbox/image scaling mode.
    static member inline stretch(v: Stretch) : DomItem =
      attr.custom("stretch", string v)

    static member inline stretch(s: WithGetValue<'s, Stretch>) : DomItem =
      attr.custom("stretch", s.get_Value >> string)

  (*
        Events shared by several components - declared once, here.
    *)

  type on with

    /// Flyout/dialog/menu shown (`show`).
    static member inline show(h: unit -> unit) : DomItem =
      on.event("show", fun _ -> h())

    /// Flyout/dialog/menu hidden (`hide`).
    static member inline hide(h: unit -> unit) : DomItem =
      on.event("hide", fun _ -> h())

    /// Flyout/dialog closed (`close`).
    static member inline close(h: unit -> unit) : DomItem =
      on.event("close", fun _ -> h())

    /// Flyout opened (`open`).
    static member inline open'(h: unit -> unit) : DomItem =
      on.event("open", fun _ -> h())

    /// Text-valued components committed a value (`change` detail `{ value }`).
    static member inline valueChanged(h: TextValueChange -> unit) : DomItem =
      Internal.onCustom "change" h

    /// Toggle state changed (`change` detail `{ checked }`).
    static member inline checkedChanged(h: CheckedChange -> unit) : DomItem =
      Internal.onCustom "change" h

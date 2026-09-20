namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Primitives =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-border` (root export - no dedicated subpath).
  [<Import("registerMetroBorder", "@angelmunoz/metrino")>]
  let registerMetroBorder: unit -> unit = jsNative

  /// Register `metro-context-menu` (root export - no dedicated subpath).
  [<Import("registerMetroContextMenu", "@angelmunoz/metrino")>]
  let registerMetroContextMenu: unit -> unit = jsNative

  /// Register `metro-expander` (`@angelmunoz/metrino/expander`).
  [<Import("registerMetroExpander", "@angelmunoz/metrino/expander")>]
  let registerMetroExpander: unit -> unit = jsNative

  /// Register `metro-icon` (`@angelmunoz/metrino/icon`).
  [<Import("registerMetroIcon", "@angelmunoz/metrino/icon")>]
  let registerMetroIcon: unit -> unit = jsNative

  /// Register `metro-image` (root export - no dedicated subpath).
  [<Import("registerMetroImage", "@angelmunoz/metrino")>]
  let registerMetroImage: unit -> unit = jsNative

  /// Register `metro-info-bar` (`@angelmunoz/metrino/info-bar`).
  [<Import("registerMetroInfoBar", "@angelmunoz/metrino/info-bar")>]
  let registerMetroInfoBar: unit -> unit = jsNative

  /// Register `metro-media-element` (root export - no dedicated subpath).
  [<Import("registerMetroMediaElement", "@angelmunoz/metrino")>]
  let registerMetroMediaElement: unit -> unit = jsNative

  /// Register `metro-menu-flyout` (`@angelmunoz/metrino/menu-flyout`).
  [<Import("registerMetroMenuFlyout", "@angelmunoz/metrino/menu-flyout")>]
  let registerMetroMenuFlyout: unit -> unit = jsNative

  /// Dynamic-import variant of `registerMetroMenuFlyout`: the component loads
  /// in its own chunk instead of the entry bundle. Resolves when the custom
  /// element is registered. The emit is the arrow itself: the call site
  /// invokes it.
  [<Emit("() => import('@angelmunoz/metrino/menu-flyout').then((m) => m.registerMetroMenuFlyout())")>]
  let registerMetroMenuFlyoutDynamic: unit -> JS.Promise<unit> = jsNative

  /// Register `metro-person-picture` (`@angelmunoz/metrino/person-picture`).
  [<Import("registerMetroPersonPicture", "@angelmunoz/metrino/person-picture")>]
  let registerMetroPersonPicture: unit -> unit = jsNative

  /// Register `metro-rich-text-block` (root export - no dedicated subpath).
  [<Import("registerMetroRichTextBlock", "@angelmunoz/metrino")>]
  let registerMetroRichTextBlock: unit -> unit = jsNative

  /// Register `metro-text-block` (`@angelmunoz/metrino/text-block`).
  [<Import("registerMetroTextBlock", "@angelmunoz/metrino/text-block")>]
  let registerMetroTextBlock: unit -> unit = jsNative

  /// Register `metro-toast` (`@angelmunoz/metrino/toast`).
  [<Import("registerMetroToast", "@angelmunoz/metrino/toast")>]
  let registerMetroToast: unit -> unit = jsNative

  /// Register `metro-tooltip` (`@angelmunoz/metrino/tooltip`).
  [<Import("registerMetroTooltip", "@angelmunoz/metrino/tooltip")>]
  let registerMetroTooltip: unit -> unit = jsNative

  (* Global toast API. metrino 0.5 removed the module-level `showToast` /
     `hideToast` in favor of the exported `ToastHost` controller: each
     instance lazily attaches its own `metro-toast` to `document.body` on
     the first `show` and can be `dispose`d independently. *)

  /// metrino `ToastHost` (`@angelmunoz/metrino` root export) - the official
  /// global toast API since 0.5.0. Construct with `ToastHost()`.
  [<AllowNullLiteral; Import("ToastHost", "@angelmunoz/metrino")>]
  type ToastHost() =
    /// Show a toast; attaches the host element on first call. Returns the
    /// toast id. Throws for unknown severity values.
    member _.show(options: ToastOptions) : string = jsNative
    member _.hide(id: string) : unit = jsNative
    member _.clearAll() : unit = jsNative
    /// Remove the host element; the next `show` creates a fresh one.
    member _.dispose() : unit = jsNative

  type Html with

    static member inline metroBorder(args: DomItem list) : DomItem =
      Html.elem "metro-border" args

    static member inline metroContextMenu(args: DomItem list) : DomItem =
      Html.elem "metro-context-menu" args

    static member inline metroExpander(args: DomItem list) : DomItem =
      Html.elem "metro-expander" args

    static member inline metroIcon(args: DomItem list) : DomItem =
      Html.elem "metro-icon" args

    static member inline metroImage(args: DomItem list) : DomItem =
      Html.elem "metro-image" args

    static member inline metroInfoBar(args: DomItem list) : DomItem =
      Html.elem "metro-info-bar" args

    static member inline metroMediaElement(args: DomItem list) : DomItem =
      Html.elem "metro-media-element" args

    static member inline metroMenuFlyout(args: DomItem list) : DomItem =
      Html.elem "metro-menu-flyout" args

    static member inline metroPersonPicture(args: DomItem list) : DomItem =
      Html.elem "metro-person-picture" args

    static member inline metroRichTextBlock(args: DomItem list) : DomItem =
      Html.elem "metro-rich-text-block" args

    static member inline metroTextBlock(args: DomItem list) : DomItem =
      Html.elem "metro-text-block" args

    static member inline metroToast(args: DomItem list) : DomItem =
      Html.elem "metro-toast" args

    static member inline metroTooltip(args: DomItem list) : DomItem =
      Html.elem "metro-tooltip" args

  type attr with

    /// Border width (metro border).
    static member inline borderThickness(v: string) : DomItem =
      attr.custom("border-thickness", v)

    static member inline borderThickness(s: WithGetValueString<'s>) : DomItem =
      attr.custom("border-thickness", s.get_Value)

    /// Border color (metro border).
    static member inline borderColor(v: string) : DomItem =
      attr.custom("border-color", v)

    static member inline borderColor(s: WithGetValueString<'s>) : DomItem =
      attr.custom("border-color", s.get_Value)

    /// Fill color (metro border).
    static member inline background(v: string) : DomItem =
      attr.custom("background", v)

    static member inline background(s: WithGetValueString<'s>) : DomItem =
      attr.custom("background", s.get_Value)

    /// Corner rounding, px (metro border).
    static member inline cornerRadius(v: float) : DomItem =
      attr.custom("corner-radius", string v)

    static member inline cornerRadius(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("corner-radius", s.get_Value >> string)

    /// Inner padding (metro border).
    static member inline padding(v: string) : DomItem =
      attr.custom("padding", v)

    static member inline padding(s: WithGetValueString<'s>) : DomItem =
      attr.custom("padding", s.get_Value)

    /// Text emphasis flags (text block).
    static member inline bold(v: bool) : DomItem =
      Base.booleanAttribute "bold" v

    static member inline bold(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "bold" s.get_Value

    static member inline italic(v: bool) : DomItem =
      Base.booleanAttribute "italic" v

    static member inline italic(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "italic" s.get_Value

    static member inline underline(v: bool) : DomItem =
      Base.booleanAttribute "underline" v

    static member inline underline(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "underline" s.get_Value

    static member inline strikethrough(v: bool) : DomItem =
      Base.booleanAttribute "strikethrough" v

    static member inline strikethrough(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "strikethrough" s.get_Value

    /// Wrap long lines (text block).
    static member inline wrap(v: bool) : DomItem =
      Base.booleanAttribute "wrap" v

    static member inline wrap(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "wrap" s.get_Value

    /// Overflow behavior (rich text block).
    static member inline textTrimming(v: TextTrimming) : DomItem =
      attr.custom("text-trimming", string v)

    static member inline textTrimming
      (s: WithGetValue<'s, TextTrimming>)
      : DomItem =
      attr.custom("text-trimming", s.get_Value >> string)

    /// Line cap (rich text block).
    static member inline maxLines(v: float) : DomItem =
      attr.custom("max-lines", string v)

    static member inline maxLines(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("max-lines", s.get_Value >> string)

    /// Presence badge (person picture).
    static member inline presence(v: Presence) : DomItem =
      attr.custom("presence", string v)

    static member inline presence(s: WithGetValue<'s, Presence>) : DomItem =
      attr.custom("presence", s.get_Value >> string)

    /// Full name fallback (person picture).
    static member inline displayName(v: string) : DomItem =
      attr.custom("display-name", v)

    static member inline displayName(s: WithGetValueString<'s>) : DomItem =
      attr.custom("display-name", s.get_Value)

    /// Initials fallback (person picture).
    static member inline initials(v: string) : DomItem =
      attr.custom("initials", v)

    static member inline initials(s: WithGetValueString<'s>) : DomItem =
      attr.custom("initials", s.get_Value)

    /// Shown while `src` loads (metro image).
    static member inline fallback(v: string) : DomItem =
      attr.custom("fallback", v)

    static member inline fallback(s: WithGetValueString<'s>) : DomItem =
      attr.custom("fallback", s.get_Value)

    /// Tooltip text (metro tooltip).
    static member inline text(v: string) : DomItem = attr.custom("text", v)

    static member inline text(s: WithGetValueString<'s>) : DomItem =
      attr.custom("text", s.get_Value)

    /// Media kind: audio or video (`metro-media-element`).
    static member inline type'(v: MediaType) : DomItem =
      attr.custom("type", string v)

    static member inline type'(s: WithGetValue<'s, MediaType>) : DomItem =
      attr.custom("type", s.get_Value >> string)

  (* src/alt/poster -> base; autoplay/controls/muted/loop -> base boolean attrs;
       icon/size/delay/placement/presence-adjacent shared attrs are in Types;
       content (rich text block) and title (expander/info-bar) are base. *)

  type on with

    /// `metro-expander`: fold state changed (`expanded` with `{ expanded }`).
    static member inline expandedChanged(h: ExpandedChange -> unit) : DomItem =
      Internal.onCustom "expanded" h

    /// `metro-context-menu`: a slotted menu item was clicked (`itemclick`).
    static member inline menuItemClick(h: MenuItemClick -> unit) : DomItem =
      Internal.onCustom "itemclick" h

    /// `metro-media-element`: playback started (custom `play`).
    static member inline mediaPlay(h: MediaPlaybackEvent -> unit) : DomItem =
      Internal.onCustom "play" h

    /// `metro-media-element`: playback paused (custom `pause`).
    static member inline mediaPause(h: MediaPlaybackEvent -> unit) : DomItem =
      Internal.onCustom "pause" h

    /// `metro-media-element`: playback finished (custom `ended`).
    static member inline mediaEnded(h: MediaPlaybackEvent -> unit) : DomItem =
      Internal.onCustom "ended" h

    /// `metro-media-element`: playhead moved (custom `timeupdate`).
    static member inline mediaTimeUpdate(h: MediaTimeUpdate -> unit) : DomItem =
      Internal.onCustom "timeupdate" h

    /// `metro-media-element`: volume/mute changed (custom `volumechange`).
    static member inline mediaVolumeChange
      (h: MediaVolumeChange -> unit)
      : DomItem =
      Internal.onCustom "volumechange" h

  (* metro-image `load`/`error` are plain Events - use base `on.load`/`on.error`.
       Tile `click` events are `on.click`. Toast lifecycle is imperative API. *)

namespace Metrino.Ripple

[<AutoOpen>]
module Progress =

  open Fable.Core
  open Fable.Ripple.Dom

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-progress-bar` (`@angelmunoz/metrino/progress-bar`).
  [<Import("registerMetroProgressBar", "@angelmunoz/metrino/progress-bar")>]
  let registerMetroProgressBar: unit -> unit = jsNative

  /// Register `metro-progress-ring` (`@angelmunoz/metrino/progress-ring`).
  [<Import("registerMetroProgressRing", "@angelmunoz/metrino/progress-ring")>]
  let registerMetroProgressRing: unit -> unit = jsNative

  type Html with

    static member inline metroProgressBar(args: DomItem list) : DomItem =
      Html.elem "metro-progress-bar" args

    static member inline metroProgressRing(args: DomItem list) : DomItem =
      Html.elem "metro-progress-ring" args

  type attr with

    /// Upper bound of the progress bar (its attribute is `maximum`).
    static member inline maximum(v: float) : DomItem =
      attr.custom("maximum", string v)

    static member inline maximum(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("maximum", s.get_Value >> string)

    /// Indeterminate animation.
    static member inline indeterminate(v: bool) : DomItem =
      Base.booleanAttribute "indeterminate" v

    static member inline indeterminate(s: WithGetValueBool<'s>) : DomItem =
      Base.bindBooleanAttribute "indeterminate" s.get_Value

    /// Render the numeric label (`showLabel` attribute - camelCase, set as property).
    static member inline showLabel(v: bool) : DomItem =
      Base.property "showLabel" v

    static member inline showLabel(s: WithGetValueBool<'s>) : DomItem =
      Base.bindProperty "showLabel" s.get_Value

  (* value: float -> base `attr.value`; size -> shared `attr.size` (Types). *)

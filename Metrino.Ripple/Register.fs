namespace Metrino.Ripple

/// The root-level registration API of `@angelmunoz/metrino`, bound 1:1.
/// Per-component `registerMetro<X>` functions live next to their bindings in the
/// category modules; this module carries the aggregate functions and `iconMap`.
/// Registration is always explicit - nothing registers by itself.
[<AutoOpen>]
module Register =

  open Fable.Core

  /// Register every metrino component.
  [<Import("registerAllComponents", "@angelmunoz/metrino")>]
  let registerAllComponents: unit -> unit = jsNative

  [<Import("registerButtons", "@angelmunoz/metrino")>]
  let registerButtons: unit -> unit = jsNative

  [<Import("registerDatetime", "@angelmunoz/metrino")>]
  let registerDatetime: unit -> unit = jsNative

  [<Import("registerDialogs", "@angelmunoz/metrino")>]
  let registerDialogs: unit -> unit = jsNative

  [<Import("registerInputs", "@angelmunoz/metrino")>]
  let registerInputs: unit -> unit = jsNative

  [<Import("registerLayout", "@angelmunoz/metrino")>]
  let registerLayout: unit -> unit = jsNative

  [<Import("registerNavigation", "@angelmunoz/metrino")>]
  let registerNavigation: unit -> unit = jsNative

  [<Import("registerPrimitives", "@angelmunoz/metrino")>]
  let registerPrimitives: unit -> unit = jsNative

  [<Import("registerProgress", "@angelmunoz/metrino")>]
  let registerProgress: unit -> unit = jsNative

  [<Import("registerSelection", "@angelmunoz/metrino")>]
  let registerSelection: unit -> unit = jsNative

  [<Import("registerTiles", "@angelmunoz/metrino")>]
  let registerTiles: unit -> unit = jsNative

  /// `Record<string, string>` of semantic icon name to MDI path data
  /// (the names `metro-icon` accepts in its `icon` attribute).
  [<Import("iconMap", "@angelmunoz/metrino")>]
  let iconMap: obj = jsNative

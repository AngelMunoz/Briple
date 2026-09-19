namespace Metrino.Ripple

type WithGetValue<'T, 'U when 'T: (member get_Value: unit -> 'U)> = 'T
type WithGetValueString<'T when 'T: (member get_Value: unit -> string)> = 'T
type WithGetValueFloat<'T when 'T: (member get_Value: unit -> float)> = 'T
type WithGetValueBool<'T when 'T: (member get_Value: unit -> bool)> = 'T



/// Low-level interop for the metrino bindings: the package stylesheet import and
/// the typed custom-event primitive every `on` binding below is built from.
[<AutoOpen>]
module Interop =

  open Browser.Types
  open Fable.Core
  open Fable.Ripple.Dom

  module Internal =

    /// Attach a listener for a metrino custom event and hand the handler its
    /// unwrapped `detail` payload, typed as `'detail`. Native (non-custom)
    /// events are ignored, so re-emitted native event names stay unambiguous.
    let inline onCustom<'Detail>
      (name: string)
      (handler: 'Detail -> unit)
      : DomItem =
      Base.onEvent name (fun (ev: Event) ->
        match ev with
        | :? CustomEvent as custom -> handler(unbox<'Detail> custom.detail)
        | _ -> ())

  type on with

    /// Listen to any metrino custom event by name with a typed `detail` payload.
    static member inline custom<'Detail>
      (name: string, h: 'Detail -> unit)
      : DomItem =
      Internal.onCustom name h

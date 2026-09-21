module App.Chrome

// Shared UI components: the back header, the badge section heading, the
// accent notice card, and the anchor field. Pages compose these; the pieces
// depend on State and Locale, never on a page module.

open System
open Fable.Ripple
open Fable.Ripple.Dom
open Briple.Store
open Plan.Types
open App.State

val inline backHeader: title: string -> DomItem

/// Section heading in the badge typography.
val inline heading: text: string -> DomItem

/// The exercise scheme line: reps, RIR, and rest, verbatim and " · " apart;
/// a "-" field drops, and a line with nothing left drops. The Today card and
/// the session detail render the same line.
val inline schemeLine: exercise: Ejercicio -> string option

/// The accent-bordered notice surface: a title line and quiet detail lines.
val inline noticeCard: title: string -> lines: string list -> DomItem

/// Field + inline roller for a DateOnly. The field shows the localized date;
/// a tap expands the roller. Roller changes write the var and call `onChanged`.
val inline anchorField:
  anchor: Var<DateOnly> -> onChanged: (DateOnly -> unit) -> DomItem

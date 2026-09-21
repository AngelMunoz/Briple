module App.SessionDetail

// Session detail (storyboard 4.4): the routine the Today card shows flat,
// rendered as a plan. The header carries the date caption, the session
// title, and the plan week with the mesociclo RIR badge; the body groups
// the exercises by circuit with the A1/B1 order idiom, round counts in the
// circuit headers, verbatim schemes with notes, and one guía trailer line
// per relevant extras key. Opened from the Today app bar's info command;
// the back chevron returns to Day or Week. The shell owns the app bar and
// the scroll region; the page always shows the variant it was opened from.

open System
open Fable.Ripple.Dom
open Plan.Types

/// One exercise row of the detail: the circuit-order idiom ("A1"), the
/// name, the scheme line, and the note when the file carries one.
type DetailExercise = {
  Idiom: string
  Name: string
  Scheme: string option
  Notes: string option
}

/// One circuit of the dia: the letter, the shared round count, and the
/// exercises in file order. Groups appear in order of first exercise.
type CircuitGroup = {
  Circuito: string
  Vueltas: string
  Exercises: DetailExercise list
}

/// Groups the dia's exercises by circuit. Vueltas is the group's first
/// value; the format keeps every exercise of a circuit on the same count.
val detailGroups: dia: Dia -> CircuitGroup list

/// The guide trailer: the first descansos line and the first
/// regla-circuito line, in that order; the full lists stay on the Plan
/// page's guide. Empty when the file carries neither key.
val guideTrailer: plan: Plan -> string list

val view: unit -> DomItem

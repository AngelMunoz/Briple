module App.Today

// Today screen: date block, empty state with the import paths, and the
// Day pivot. The pivot is the enclosing component; its Day item holds the
// day hub - one section per day of the week - and its chevrons.

open System
open Plan.Types

type CardExercise = { Name: string; Scheme: string option }

type CardModel = {
  Title: string
  Badge: string
  Exercises: CardExercise list
}

val cardModel:
  dia: Dia -> weekdayName: string -> badge: (int -> int -> string) -> CardModel

/// True when the selected date has a session in the viewed variant: the
/// app bar's session-detail command shows only then.
val hasSelectedSession: unit -> bool

val view: unit -> Fable.Ripple.Dom.DomItem

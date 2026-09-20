module App.Today

// Today screen : date block, empty state with
// the import paths, and the Day pivot holding the day strip and the
// session card.


open System
open Plan.Types

type StripDay = {
  Date: DateOnly
  Letter: string
  IsSession: bool
  IsSelected: bool
}

type CardExercise = { Name: string; Scheme: string option }

type CardModel = {
  Title: string
  Badge: string
  Exercises: CardExercise list
  More: (int * int) option
}

val weekDates: selected: DateOnly -> DateOnly list

val stripModel:
  plan: Plan option ->
  genero: Genero * opcionId: string ->
    anchor: DateOnly option ->
    selected: DateOnly ->
    letters: string list ->
      StripDay list

val cardModel:
  dia: Dia -> weekdayName: string -> badge: (int -> int -> string) -> CardModel

val view: unit -> Fable.Ripple.Dom.DomItem

module App.Locale

// Chrome strings (es/en) and Intl date helpers. Weekday and date
// text comes from the device Intl, never from the string table; the file's
// day names never leak to the UI as-is.

open System

type Lang =
  | Es
  | En

type Strings = {
  EmptyTitle: string
  EmptyBody: string
  LoadPlan: string
  TrySample: string
  Back: string
  PlanWeek: int -> int -> string
  WeekQuarter: int -> int -> string
  RestNext: string -> string -> string
  BeforePlan: string
  PlanDone: string
  ToastLoaded: int -> int -> string
  ExercisesBadge: int -> int -> string
  PlanMenu: string
  SettingsMenu: string
  PagePlan: string
  PageSettings: string
  ImportFailed: string
  ImportFailedDetail: int -> string -> string
}

val esStrings: Strings

val enStrings: Strings

val current: unit -> Lang

val strings: unit -> Strings

val toDateTime: date: DateOnly -> DateTime

val locale: unit -> string

val weekdayLong: locale: string * date: DateTime -> string

val weekdayNarrow: locale: string * date: DateTime -> string

val weekdayShort: locale: string * date: DateTime -> string

val dayMonth: locale: string * date: DateTime -> string

/// ISO-8601 week number (weeks start Monday; week 1 holds the first Thursday).
/// Returns the ISO week-numbering year and the 1-based week.
val isoWeek: date: DateOnly -> int * int

val quarter: date: DateOnly -> int

module App.Locale

// Chrome strings (es/en) and Intl date helpers. Weekday and date
// text comes from the device Intl, never from the string table; the file's
// day names never leak to the UI as-is.

open System
open Plan.Types
open App.Theme

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
  StartsOn: string -> string
  Hoy: string
  SessionsSummary: int -> string
  RestRow: string
  ExerciseCount: int -> string
  PlanMenu: string
  SettingsMenu: string
  PagePlan: string
  PageSettings: string
  ImportFailed: string
  ImportFailedDetail: int -> string -> string
  GeneroWord: Genero -> string
  DiasUnit: string
  PreviewTitle: string
  Commit: string
  Cancel: string
  StartHeading: string
  StartsLabel: string
  WeeksCount: int -> string
  WarningsLine: int -> string
  WarningLine: string -> int -> string
  VariantsHeading: string
  AnchorLabel: string
  GuiaHeading: string
  ImportsHeading: string
  ExportMenu: string
  RemoveMenu: string
  ThemeHeading: string
  ThemeSystem: string
  ThemeLight: string
  ThemeDark: string
  AccentHeading: string
  AccentName: Accent -> string
  ResetHeading: string
  ResetBody: string
  ResetToDefaults: string
  ResetConfirmTitle: string
  ResetConfirmBody: string
  ResetConfirmAccept: string
  InstallHeading: string
  InstallBody: string
  InstallApp: string
  ToastReset: string
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

val dayNumber: locale: string * date: DateTime -> string

val monthLong: locale: string * date: DateTime -> string

val dayMonth: locale: string * date: DateTime -> string

/// ISO-8601 week number (weeks start Monday; week 1 holds the first Thursday).
/// Returns the ISO week-numbering year and the 1-based week.
val isoWeek: date: DateOnly -> int * int

val quarter: date: DateOnly -> int

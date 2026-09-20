module App.Locale

// Chrome strings (es/en) and Intl date helpers. Weekday and date
// text comes from the device Intl, never from the string table; the file's
// day names never leak to the UI as-is.

open System
open Browser
open Fable.Core

type Lang =
  | Es
  | En

let current() : Lang =
  match navigator.language with
  | Some tag when tag.StartsWith "es" -> Es
  | _ -> En

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

let esStrings = {
  EmptyTitle = "Tu calendario está vacío."
  EmptyBody = "Carga un archivo de plan para ver tus sesiones aquí."
  LoadPlan = "Cargar un plan"
  TrySample = "Probar el plan de ejemplo"
  Back = "Atrás"
  PlanWeek = fun semana total -> $"Semana {semana} de {total}"
  WeekQuarter = fun semana quarter -> $"Semana {semana} · T{quarter}"
  RestNext =
    fun weekday title -> $"Descanso · próxima sesión: {weekday}, {title}"
  BeforePlan = "El plan aún no empieza."
  PlanDone = "Plan completado"
  ToastLoaded =
    fun variantes semanas ->
      $"Plan cargado · {variantes} variantes · {semanas} semanas"
  ExercisesBadge =
    fun ejercicios circuitos ->
      $"{ejercicios} ejercicios · {circuitos} circuitos"
  PlanMenu = "Plan de entrenamiento"
  SettingsMenu = "Ajustes"
  PagePlan = "Plan"
  PageSettings = "Ajustes"
  ImportFailed = "No se pudo cargar el plan"
  ImportFailedDetail = fun linea mensaje -> $"Línea {linea}: {mensaje}"
}

let enStrings = {
  EmptyTitle = "Your calendar is empty."
  EmptyBody = "Load a training plan file to see your sessions here."
  LoadPlan = "Load a plan"
  TrySample = "Try the sample plan"
  Back = "Back"
  PlanWeek = fun week total -> $"Week {week} of {total}"
  WeekQuarter = fun week quarter -> $"Week {week} · Q{quarter}"
  RestNext = fun weekday title -> $"Rest · next session: {weekday}, {title}"
  BeforePlan = "The plan has not started yet."
  PlanDone = "Plan completed"
  ToastLoaded =
    fun variants weeks -> $"Plan loaded · {variants} variants · {weeks} weeks"
  ExercisesBadge =
    fun exercises circuits -> $"{exercises} exercises · {circuits} circuits"
  PlanMenu = "Training plan"
  SettingsMenu = "Settings"
  PagePlan = "Plan"
  PageSettings = "Settings"
  ImportFailed = "Could not load the plan"
  ImportFailedDetail = fun line message -> $"Line {line}: {message}"
}

let strings() : Strings =
  match current() with
  | Es -> esStrings
  | En -> enStrings

let toDateTime(date: DateOnly) : DateTime =
  DateTime(date.Year, date.Month, date.Day)

let locale() : string =
  navigator.language |> Option.defaultValue "en"

// The raw Intl emits live in a nested module that Locale.fsi does not
// declare: Fable drops [<Emit>] bindings that a signature file exposes, so
// the public surface below wraps them in plain functions.
module Intl =

  [<Emit("new Intl.DateTimeFormat($0, { weekday: 'long' }).format($1)")>]
  let weekdayLong(locale: string, date: DateTime) : string = jsNative

  [<Emit("new Intl.DateTimeFormat($0, { weekday: 'narrow' }).format($1)")>]
  let weekdayNarrow(locale: string, date: DateTime) : string = jsNative

  [<Emit("new Intl.DateTimeFormat($0, { weekday: 'short' }).format($1)")>]
  let weekdayShort(locale: string, date: DateTime) : string = jsNative

  [<Emit("new Intl.DateTimeFormat($0, { day: 'numeric', month: 'long' }).format($1)")>]
  let dayMonth(locale: string, date: DateTime) : string = jsNative

let weekdayLong(locale: string, date: DateTime) : string =
  Intl.weekdayLong(locale, date)

let weekdayNarrow(locale: string, date: DateTime) : string =
  Intl.weekdayNarrow(locale, date)

let weekdayShort(locale: string, date: DateTime) : string =
  Intl.weekdayShort(locale, date)

let dayMonth(locale: string, date: DateTime) : string =
  Intl.dayMonth(locale, date)

let private isLeapYear(year: int) =
  year % 4 = 0 && (year % 100 <> 0 || year % 400 = 0)

let dayOfYear(date: DateOnly) =
  let cumulative = [| 0; 31; 59; 90; 120; 151; 181; 212; 243; 273; 304; 334 |]
  let leap = if date.Month > 2 && isLeapYear date.Year then 1 else 0
  cumulative.[date.Month - 1] + date.Day + leap

/// ISO-8601 week number (weeks start Monday; week 1 holds the first Thursday).
/// Returns the ISO week-numbering year and the 1-based week.
let isoWeek(date: DateOnly) : int * int =
  let dow =
    match (toDateTime date).DayOfWeek with
    | DayOfWeek.Sunday -> 7
    | day -> int day

  let thursday = (toDateTime date).AddDays(float(4 - dow))
  let thursdayDate = DateOnly(thursday.Year, thursday.Month, thursday.Day)
  let week = (dayOfYear thursdayDate - 1) / 7 + 1
  (thursdayDate.Year, week)

let quarter(date: DateOnly) : int = (date.Month - 1) / 3 + 1

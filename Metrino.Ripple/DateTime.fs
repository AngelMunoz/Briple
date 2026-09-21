namespace Metrino.Ripple

open Metrino.Ripple

[<AutoOpen>]
module Datetime =

  open Fable.Core
  open Fable.Ripple.Dom

  /// Clock convention of `metro-time-picker-roller`'s `hour-format` attribute:
  /// `"12"` or `"24"`.
  [<StringEnum(CaseRules.LowerFirst)>]
  type HourFormat =
    | [<CompiledName "12">] TwelveHour
    | [<CompiledName "24">] TwentyFourHour

  (*
        Registration - call explicitly before creating elements.
    *)

  /// Register `metro-calendar` (root export - no dedicated subpath).
  [<Import("registerMetroCalendar", "@angelmunoz/metrino")>]
  let registerMetroCalendar: unit -> unit = jsNative

  /// Register `metro-calendar-date-picker` (root export - no dedicated subpath).
  [<Import("registerMetroCalendarDatePicker", "@angelmunoz/metrino")>]
  let registerMetroCalendarDatePicker: unit -> unit = jsNative

  /// Register `metro-date-picker` (`@angelmunoz/metrino/date-picker`).
  [<Import("registerMetroDatePicker", "@angelmunoz/metrino/date-picker")>]
  let registerMetroDatePicker: unit -> unit = jsNative

  /// Register `metro-date-picker-roller` (`@angelmunoz/metrino/date-picker-roller`).
  [<Import("registerMetroDatePickerRoller",
           "@angelmunoz/metrino/date-picker-roller")>]
  let registerMetroDatePickerRoller: unit -> unit = jsNative

  /// Dynamic-import variant of `registerMetroDatePickerRoller`: the component
  /// loads in its own chunk instead of the entry bundle. Resolves when the
  /// custom element is registered. The emit is the arrow itself: the call
  /// site invokes it.
  [<Emit("() => import('@angelmunoz/metrino/date-picker-roller').then((m) => m.registerMetroDatePickerRoller())")>]
  let registerMetroDatePickerRollerDynamic: unit -> JS.Promise<unit> = jsNative

  /// Register `metro-time-picker` (`@angelmunoz/metrino/time-picker`).
  [<Import("registerMetroTimePicker", "@angelmunoz/metrino/time-picker")>]
  let registerMetroTimePicker: unit -> unit = jsNative

  /// Register `metro-time-picker-roller` (`@angelmunoz/metrino/time-picker-roller`).
  [<Import("registerMetroTimePickerRoller",
           "@angelmunoz/metrino/time-picker-roller")>]
  let registerMetroTimePickerRoller: unit -> unit = jsNative

  type Html with

    static member inline metroCalendar(args: DomItem list) : DomItem =
      Html.elem "metro-calendar" args

    static member inline metroCalendarDatePicker(args: DomItem list) : DomItem =
      Html.elem "metro-calendar-date-picker" args

    static member inline metroDatePicker(args: DomItem list) : DomItem =
      Html.elem "metro-date-picker" args

    static member inline metroDatePickerRoller(args: DomItem list) : DomItem =
      Html.elem "metro-date-picker-roller" args

    static member inline metroTimePicker(args: DomItem list) : DomItem =
      Html.elem "metro-time-picker" args

    static member inline metroTimePickerRoller(args: DomItem list) : DomItem =
      Html.elem "metro-time-picker-roller" args

  type attr with

    /// ISO date selection (`metro-calendar`).
    static member inline selectedDate(v: string) : DomItem =
      attr.custom("selected-date", v)

    static member inline selectedDate(s: WithGetValueString<'s>) : DomItem =
      attr.custom("selected-date", s.get_Value)

    /// Earliest selectable date (calendar, calendar date picker).
    static member inline minDate(v: string) : DomItem =
      attr.custom("min-date", v)

    static member inline minDate(s: WithGetValueString<'s>) : DomItem =
      attr.custom("min-date", s.get_Value)

    /// Latest selectable date (calendar, calendar date picker).
    static member inline maxDate(v: string) : DomItem =
      attr.custom("max-date", v)

    static member inline maxDate(s: WithGetValueString<'s>) : DomItem =
      attr.custom("max-date", s.get_Value)

    /// Month the calendar displays.
    static member inline displayDate(v: string) : DomItem =
      attr.custom("display-date", v)

    static member inline displayDate(s: WithGetValueString<'s>) : DomItem =
      attr.custom("display-date", s.get_Value)

    /// First day of week, 0 = Sunday (calendar).
    static member inline firstDayOfWeek(v: float) : DomItem =
      attr.custom("first-day-of-week", string v)

    static member inline firstDayOfWeek(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("first-day-of-week", s.get_Value >> string)

    /// Roller year range start (date picker roller).
    static member inline minYear(v: float) : DomItem =
      attr.custom("min-year", string v)

    static member inline minYear(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("min-year", s.get_Value >> string)

    /// Roller year range end (date picker roller).
    static member inline maxYear(v: float) : DomItem =
      attr.custom("max-year", string v)

    static member inline maxYear(s: WithGetValueFloat<'s>) : DomItem =
      attr.custom("max-year", s.get_Value >> string)

    /// `"12"` or `"24"` (time picker roller).
    static member inline hourFormat(v: string) : DomItem =
      attr.custom("hour-format", v)

    /// Strongly typed hour format. Inline so it erases beside the string
    /// overload (the enum compiles to `"12"` / `"24"`).
    static member inline hourFormat(v: HourFormat) : DomItem =
      attr.custom("hour-format", string v)

    static member inline hourFormat(s: WithGetValueString<'s>) : DomItem =
      attr.custom("hour-format", s.get_Value)

    /// Output format, e.g. `YYYY-MM-DD` (calendar date picker).
    static member inline format(v: string) : DomItem = attr.custom("format", v)

    static member inline format(s: WithGetValueString<'s>) : DomItem =
      attr.custom("format", s.get_Value)

  type on with

    /// `metro-calendar`: a date was picked (`dateselected`).
    static member inline dateSelected(h: DateSelected -> unit) : DomItem =
      Internal.onCustom "dateselected" h

    /// `metro-calendar`: visible month changed (`displaymonthchanged`).
    static member inline displayMonthChanged
      (h: MonthChanged -> unit)
      : DomItem =
      Internal.onCustom "displaymonthchanged" h

    /// `metro-calendar-date-picker`: committed value + date (`change`).
    static member inline dateChanged(h: DateValueChange -> unit) : DomItem =
      Internal.onCustom "change" h

    (* date/time (roller) pickers emit `change` with `{ value: string }` -
           use the shared `on.valueChanged`. *)

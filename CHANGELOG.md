# Changelog

## [Unreleased]

### Added

- **Shell:** the app is installable and works offline: a service worker caches the app shell after the first visit, a web app manifest names it for the home screen, and updates download in the background and apply on the next launch.
- **Session detail:** a command on the Today app bar that opens the selected day's routine as a plan: the plan week with its RIR target, circuit groups with their round counts and the A1/A2 exercise order, exercise notes, and the guide's rest and circuit-rule lines; back returns to Today.
- **Settings:** theme (system, light, or dark) and accent color pickers that restyle the app instantly; the choices are remembered on the device.
- **Settings:** a reset command, behind a confirmation, that restores the default theme and accent and clears every plan and setting stored on the device.
- **Settings:** an install-app placeholder, disabled until install support arrives.

### Fixed

- **Week:** tapping a week row keeps the selected day; the day view could lose the selection to a stale day-hub notification while the view switched.

## [0.1.0] - 2026-09-20

First release: import a training plan file and browse it as a calendar. Everything runs on the device; no account or server is involved.

### Added

- **Shell:** bottom app bar whose menu reaches Plan and Settings; back chevrons and the browser back gesture return to Today.
- **Today:** date block, the selected day's full routine, a plan-week line with the mesociclo RIR target, and quiet rest-day, before-plan, and completed states.
- **Today:** day navigator with one section per day of the week; panning, the chevrons, or a day header tap selects the day.
- **Week:** locale week range, plan-week chip, week chevrons, and a jump to today; the seven rows show each day's session or rest, mark today, and summarize the sessions; tapping a row opens that day.
- **View chip:** lists every variant in the file, grouped by gender, and switches the calendar instantly; the choice is remembered across launches and falls back to the first variant when the saved one is missing.
- **Import:** full-screen preview that lists all variants, reports unrecognized days or blocks as warnings, and offers a localized start date for adjustment before committing; cancelling leaves the store untouched.
- **Import:** plans can be loaded from a file picker or the bundled four-week sample plan.
- **Plan:** file title and origin, the variant list with tap-to-view, the start date with re-anchoring, the plan guide notes grouped by key, import history with one-tap re-load, export of the original file, and remove.
- **Settings:** placeholder page with back navigation.
- **Data:** all data is stored on the device; the raw plan file remains the source of truth, and the active plan, viewed variant, and selected date persist across reloads.
- **Locale:** Spanish and English strings, with dates and weekday names taken from the device locale.

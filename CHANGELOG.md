# Changelog

## [Unreleased]

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

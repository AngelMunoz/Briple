module App.Shell

// Page shell: Today is home; Plan, Settings, the import preview, and the
// session detail are spokes with back chevrons. The shell owns the fixed
// app bar and the scrolling content region, so no page can hide its last
// section behind the bar. The app bar holds commands, never navigation:
// Today's bar carries the session-detail command plus the Plan / Settings
// menu items.

val view: unit -> Fable.Ripple.Dom.DomItem

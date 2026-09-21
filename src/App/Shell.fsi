module App.Shell

// Page shell: Today is home; Plan, Settings, and the import preview are
// spokes with back chevrons. The shell owns the fixed app bar and the
// scrolling content region, so no page can hide its last section behind
// the bar. The app bar holds commands, never navigation: the menu carries
// the Plan / Settings items.

val view: unit -> Fable.Ripple.Dom.DomItem

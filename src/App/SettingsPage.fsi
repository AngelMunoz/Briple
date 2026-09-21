module App.SettingsPage

// The Settings page: the theme choice (system, light, or dark), the accent
// picker with the 21 Metro colors, the reset command with its destructive
// confirm, and the install command, which appears only while the browser
// holds a prompt to offer. The page owns no app bar: the
// shell gives it the whole viewport, and the back chevron lives in the
// header.

val view: unit -> Fable.Ripple.Dom.DomItem

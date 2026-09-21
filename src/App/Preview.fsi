module App.Preview

// Import preview (storyboard 4.6, S5b): a full-bleed page that shows
// everything the file contains - variants as facts, not radios - plus the
// start-date decision and the parse warnings. The store stays untouched
// until the commit button runs.

open Fable.Ripple.Dom

val view: unit -> DomItem

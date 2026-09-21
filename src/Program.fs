open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple

// The metrino stylesheet: referencing it pulls it into the bundle.
JsInterop.importSideEffects "@angelmunoz/metrino/styles.css"

// Registration is explicit: metrino components are bound 1:1, but the element
// only exists once its register function ran. Register what this app uses.
registerMetroButton()
registerMetroHyperlinkButton()
registerMetroPivot()
registerMetroPivotItem()
registerMetroAppBar()
registerMetroAppBarButton()
registerMetroHub()
registerMetroHubSection()
registerMetroIcon()
registerMetroToast()

// The lazily registered components (flyout, date roller, expander, the
// Settings page's radio buttons and confirm dialog) are not part of the
// first paint: their imports load in their own chunks, and a tag defined
// after its node exists upgrades that node in place.
Promise.allSettled [
  registerMetroMenuFlyoutDynamic()
  registerMetroDatePickerRollerDynamic()
  registerMetroExpanderDynamic()
  registerMetroRadioButtonDynamic()
  registerMetroMessageDialogDynamic()
]
|> Promise.start

promise {
  // The theme applies before anything mounts, so the first paint already
  // carries the stored choice.
  let! storedTheme = Briple.Store.getStateRaw Briple.Store.ThemeKey
  and! storedAccent = Briple.Store.getStateRaw Briple.Store.AccentKey
  App.Theme.init storedTheme storedAccent

  let! stored = Briple.Store.getActiveImport()
  and! storedView = Briple.Store.getViewState()
  and! storedDate = Briple.Store.getStateRaw "selectedDate"
  App.State.init stored storedView storedDate
  Html.mount "root" (App.Shell.view())
}
|> Promise.start

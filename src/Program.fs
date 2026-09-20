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

promise {
  let! stored = Briple.Store.getActiveImport()
  let! storedView = Briple.Store.getViewState()
  let! storedDate = Briple.Store.getStateRaw "selectedDate"
  App.State.init stored storedView storedDate
  Html.mount "root" (App.Shell.view())
}
|> Promise.start

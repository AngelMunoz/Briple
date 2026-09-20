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
registerMetroIcon()
registerMetroToast()

promise {
  let! stored = Briple.Store.getActiveImport()
  let! storedView = Briple.Store.getViewState()
  App.State.init stored storedView
  Html.mount "root" (App.Shell.view())
}
|> Promise.start

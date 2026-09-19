open Fable.Core
open Fable.Ripple
open Fable.Ripple.Dom
open Metrino.Ripple

// Same for the design tokens stylesheet - referencing it pulls it into the bundle.
JsInterop.importSideEffects "@angelmunoz/metrino/styles.css"

// Registration is explicit: metrino components are bound 1:1, but the element
// only exists once its register function ran. Register what this app uses.
registerMetroButton()
registerMetroTextBox()
registerMetroCheckBox()
registerMetroContentDialog()
registerMetroListView()


/// Accent color, driven by the text box input.
let accent = Var.create "blue"

/// Click counter.
let clicks = Var.create 0

/// Toggle state, driven by the check box.
let subscribed = Var.create false

/// Reference to the dialog, captured from the element being built.
let mutable contentDialog: MetroContentDialog =
  Unchecked.defaultof<MetroContentDialog>

let view =
  Html.div [
    attr.style
      "display:flex;flex-direction:column;gap:12px;max-width:480px;padding:24px"

    Html.metroButton [
      attr.accent accent
      on.click(fun _ -> clicks.Value <- clicks.Value + 1)
      Html.text "Metro button"
    ]

    Html.output [ Html.text(fun () -> $"{clicks.Value} clicks") ]

    Html.metroTextBox [
      attr.label "Accent color"
      attr.placeholder "blue, red, green..."
      on.valueInput(fun d -> accent.Value <- d.value)
    ]

    Html.metroCheckBox [
      attr.checked' subscribed
      on.checkedChanged(fun d -> subscribed.Value <- d.``checked``)
      Html.text "Subscribe to updates"
    ]
    Html.show(
      (fun () -> subscribed.Value),
      (fun () -> Html.metroTextBlock [ Html.text "Thanks!" ])
    )

    Html.metroButton [
      on.click(fun _ -> contentDialog.show() |> ignore)
      Html.text "Open dialog"
    ]

    Html.metroContentDialog [
      attr.title "Hello from Metrino"
      attr.ref(fun el -> contentDialog <- el :?> MetroContentDialog)
      Html.text "Bound with Metrino.Ripple."
    ]

    Html.metroListView [
      attr.selectionMode SelectionMode.Single
      attr.items [| box "Item one"; box "Item two"; box "Item three" |]
      on.itemClick(fun d -> printfn "clicked %O at %i" d.item d.index)
    ]
  ]

Html.mount "root" view

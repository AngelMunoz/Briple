module App.Pwa

open Fable.Core
open Fable.Ripple

// Implementation quirks only; the surface is documented in the signature.

/// The slice of BeforeInstallPromptEvent the app needs. The event is
/// structural at runtime: the cast reads the members off the object itself.
type private InstallPromptEvent =
  abstract prompt: unit -> JS.Promise<unit>

[<Emit("window.addEventListener('beforeinstallprompt', (ev) => $0(ev))")>]
let private onInstallPrompt(handler: InstallPromptEvent -> unit) : unit =
  jsNative

[<Emit("window.addEventListener('appinstalled', () => $0())")>]
let private onAppInstalled(handler: unit -> unit) : unit = jsNative

[<Emit("window.matchMedia('(display-mode: standalone)').matches")>]
let private isStandalone() : bool = jsNative

let installAvailable: Var<bool> = Var.create false

// The captured prompt. The browser fires it once per visit and `prompt()`
// works exactly once per event, so the option is consumed, not reused.
let mutable private deferred: InstallPromptEvent option = None

let promptInstall() : unit =
  match deferred with
  | None -> ()
  | Some ev ->
    deferred <- None
    installAvailable.Value <- false
    ev.prompt() |> ignore

let init() : unit =
  if isStandalone() then
    // Installed already: the browser fires no prompt, and the section must
    // stay hidden.
    ()
  else
    onInstallPrompt(fun ev ->
      deferred <- Some ev
      installAvailable.Value <- true)

    // An install through the browser's own suggestion clears the section too.
    onAppInstalled(fun () ->
      deferred <- None
      installAvailable.Value <- false)

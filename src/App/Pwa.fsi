module App.Pwa

// The browser install flow. Chromium browsers raise `beforeinstallprompt` on
// pages that meet the install criteria (manifest, service worker, secure
// context) and open their own install banner with it. The module keeps a
// reference to that event, so the Settings page can also trigger the same
// native install dialog through its own command; the browser's banner stays
// untouched. An install through either door fires `appinstalled`, which
// clears the app's offer. Safari has no equivalent event, so the command
// stays hidden there and installation remains manual through the share menu.

open Fable.Ripple

/// True while a captured prompt can still be handed to the browser. Drives
/// the Settings install section: false hides it (nothing to offer, or the app
/// already runs installed).
val installAvailable: Var<bool>

/// Hands the captured prompt to the browser, which opens its native install
/// dialog. One shot per prompt: the section hides right after, and the
/// browser may offer a fresh prompt on a later visit.
val promptInstall: unit -> unit

/// Arms the install listeners. Call once at boot, before anything can await,
/// so an early prompt cannot slip past.
val init: unit -> unit

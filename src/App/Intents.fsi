module App.Intents

// OS bridges: opening .briple files, web+bripple deep links, the share
// target handoff, and the Web Share sheet. Every inbound path stages through
// the import preview; nothing here touches the store. The APIs without
// fable-browser bindings (launchQueue, Cache Storage) are typed [<Global>]
// interfaces; emits exist only for idioms no package binds.

open System
open Fable.Core
open Browser.Types

/// How a share attempt ended. `Shared` is the sheet's own success: the OS
/// showed its feedback. `Copied` and `Downloaded` are fallback successes
/// the caller answers with a toast or nothing. `Cancelled` is the sheet's
/// dismiss. `Failed` carries the error text for the toast.
type ShareResult =
  | Shared
  | Copied
  | Downloaded
  | Cancelled
  | Failed of string

/// Arms the launch-queue consumer, routes the web+bripple deep link, and
/// reads the share handoff. Call once at boot, after State.init: the deep
/// link routes first, shared content owns the screen when a launch carries
/// both. Launches buffer until the consumer is set, so boot order is safe.
val init: unit -> unit

/// Downloads one text file through the browser's download path. The Plan
/// page's export command and the file-share fallback share this path.
val downloadText: name: string -> text: string -> unit

/// Opens the share sheet with one text file. `Cancelled` on the sheet's
/// cancel; a missing sheet API, a payload the platform cannot carry, or any
/// other failure falls back to the download (`Downloaded`).
val shareFile:
  title: string -> name: string -> text: string -> JS.Promise<ShareResult>

/// Opens the share sheet with one text payload. `Cancelled` on the sheet's
/// cancel; a missing sheet API or any other failure falls back to the
/// clipboard (`Copied`), whose own failure is `Failed`.
val shareText: title: string -> text: string -> JS.Promise<ShareResult>

module App.Intents

// Implementation quirks only; the surface is documented in the signature.

open System
open Fable.Core
open Browser
open Browser.Types
open Fetch
open App.State

// --- Typed bindings over the APIs the fable-browser packages do not carry ---
// File System Access launch types (File Handling API). Desktop Chromium only;
// the probe below mirrors the documented feature check.
[<AllowNullLiteral>]
type FileSystemFileHandle =
  abstract getFile: unit -> JS.Promise<File>

[<AllowNullLiteral>]
type LaunchParams =
  abstract files: FileSystemFileHandle array option

[<AllowNullLiteral>]
type LaunchQueue =
  abstract setConsumer: consumer: (LaunchParams -> unit) -> unit

[<Global("window.launchQueue")>]
let launchQueue: LaunchQueue = jsNative

[<Emit("'launchQueue' in window && (typeof LaunchParams === 'undefined' || 'files' in LaunchParams.prototype)")>]
let launchHandlingSupported() : bool = jsNative

// Cache Storage, kept to the handoff surface the service worker writes.
// `open` and `match` are F# keywords, so the members carry their JS names
// through Emit attributes; Fable does not strip the primes.
[<AllowNullLiteral>]
type HandoffCache =
  [<Emit("$0.match($1)")>]
  abstract match': key: string -> JS.Promise<Response option>

  abstract delete: key: string -> JS.Promise<bool>

[<AllowNullLiteral>]
type HandoffCacheStorage =
  [<Emit("$0.open($1)")>]
  abstract open': name: string -> JS.Promise<HandoffCache>

[<Global("caches")>]
let caches: HandoffCacheStorage option = jsNative

// Web Share Level 2: the Navigator package binds share, canShare, and
// clipboard, but its canShare takes no payload, so the file probe needs the
// data-carrying call. canShare shipped after share (Safari 12-14 carry
// share without it); a missing probe answers false so the download
// fallback owns the path instead of a TypeError.
[<Emit("typeof navigator.canShare === 'function' && navigator.canShare($0)")>]
let canSharePayload(payload: obj) : bool = jsNative

[<Emit("'share' in navigator")>]
let shareAvailable() : bool = jsNative

// The sheet's cancel rejects with an AbortError; every other rejection is a
// failure the fallbacks answer.
[<Emit("$0 != null && $0.name === 'AbortError'")>]
let isAbort(error: exn) : bool = jsNative

// The download idiom: one object URL, one synthetic anchor, one click.
[<Emit("(() => { const blob = new Blob([$1], { type: 'text/plain' }); const url = URL.createObjectURL(blob); const a = document.createElement('a'); a.href = url; a.download = $0; document.body.appendChild(a); a.click(); a.remove(); URL.revokeObjectURL(url); })()")>]
let downloadAnchor (name: string) (text: string) : unit = jsNative

// The handoff entry is one text response: the first line is the file name,
// the remainder is the plan text. Keep the format and both literals in sync
// with src/sw.ts.
let handoffCacheName = "briple-handoff"

let handoffKey = "/__pending-share"

/// How a share attempt ended; the signature carries the contract.
type ShareResult =
  | Shared
  | Copied
  | Downloaded
  | Cancelled
  | Failed of string

let downloadText (name: string) (text: string) : unit = downloadAnchor name text

// --- File launches (H1) -----------------------------------------------------

let armLaunchConsumer() : unit =
  if launchHandlingSupported() then
    launchQueue.setConsumer(fun launchParams ->
      match launchParams.files with
      | Some files when files.Length > 0 ->
        promise {
          let! file = files.[0].getFile()
          do! importFile file
        }
        |> Promise.start
      | _ -> ())

// --- Deep links (H2) --------------------------------------------------------
// The handled link arrives as the `link` query value (`./?link=%s` in the
// manifest entry). The host names the destination; `import` carries optional
// `src` and `name` parameters. The param strip runs first, so a reload never
// re-enters a link.

let defaultNameFor(src: string) : string =
  try
    let url = URL.Create src

    url.pathname.Split '/'
    |> Array.rev
    |> Array.tryFind(fun segment -> segment <> "")
    |> Option.defaultValue "plan.briple"
  with _ ->
    "plan.briple"

let importFromSrc (src: string) (name: string option) : unit =
  promise {
    let! response = Fetch.fetch src []
    let! text = response.text()

    let fileName = name |> Option.defaultWith(fun () -> defaultNameFor src)

    stageImport fileName text
  }
  |> Promise.catch(fun error -> importError.Value <- Some(string error))
  |> Promise.start

let routeLink(value: string) : unit =
  // A deep link must never break the boot: every failure degrades to the
  // plain start. The path guard covers hosts that report '//' as the
  // pathname, which replaceState would read as a protocol-relative URL.
  try
    let path = window.location.pathname
    history.replaceState(null, "", (if path = "//" then "/" else path))

    let url = URL.Create value

    if url.protocol = "web+bripple:" then
      match url.host with
      | "today" -> goTo TodayPage
      | "plan" -> goTo PlanPage
      | "session" ->
        // The detail renders nothing without a plan; Today degrades instead.
        if parsed.Value.IsSome && activeImport.Value.IsSome then
          goTo SessionPage
      | "import" ->
        match url.searchParams.get "src" with
        | Some src ->
          if src.StartsWith "http://" || src.StartsWith "https://" then
            importFromSrc src (url.searchParams.get "name")
        | None ->
          // The import intent: the load command's page when a plan is
          // loaded, Today's empty state (the load buttons) otherwise.
          if activeImport.Value.IsSome then
            goTo PlanPage
      | _ -> ()
  with _ ->
    ()

// --- Share handoff (H3) -----------------------------------------------------

let ofHandoff(entry: string) : (string * string) option =
  match entry.IndexOf '\n' with
  | -1 -> None
  | index ->
    let name = entry.Substring(0, index).Trim()
    let text = entry.Substring(index + 1)
    if name = "" || text = "" then None else Some(name, text)

let readHandoff() : unit =
  promise {
    match caches with
    | None -> ()
    | Some storage ->
      let! cache = storage.open' handoffCacheName
      let! entry = cache.match' handoffKey

      match entry with
      | None -> ()
      | Some response ->
        let! text = response.text()
        do! cache.delete handoffKey |> Promise.map ignore

        match ofHandoff text with
        | Some(name, planText) -> stageImport name planText
        | None -> ()
  }
  |> Promise.catch ignore
  |> Promise.start

// --- Boot -------------------------------------------------------------------

let init() : unit =
  match URLSearchParams.Create(window.location.search).get "link" with
  | Some value -> routeLink value
  | None -> ()

  readHandoff()
  armLaunchConsumer()

// --- Share sheet (H4, H5) ---------------------------------------------------
// The payloads are structural objects against the Navigator package's typed
// `share`; the text payload rides ShareData, the file payload adds `files`.

let sharePayloadFile (title: string) (file: File) : ShareData =
  unbox {| title = title; files = [| file |] |}

let sharePayloadText (title: string) (text: string) : ShareData =
  unbox {| title = title; text = text |}

let copyFallback(text: string) : JS.Promise<ShareResult> =
  match navigator.clipboard with
  | Some clipboard -> promise {
      try
        do! clipboard.writeText text
        return Copied
      with error ->
        return Failed(string error)
    }
  | None -> Promise.lift(Failed "")

let shareText (title: string) (text: string) : JS.Promise<ShareResult> = promise {
  if not(shareAvailable()) then
    return! copyFallback text
  else
    try
      do! navigator.share(sharePayloadText title text)
      return Shared
    with error ->
      if isAbort error then
        return Cancelled
      else
        return! copyFallback text
}

let shareFile
  (title: string)
  (name: string)
  (text: string)
  : JS.Promise<ShareResult> = promise {
  let file =
    File.Create([| box text |], name, unbox {| ``type`` = "text/plain" |})

  let downloadFallback() = promise {
    downloadText name text
    return Downloaded
  }

  if
    not(shareAvailable()) || not(canSharePayload(sharePayloadFile title file))
  then
    return! downloadFallback()
  else
    try
      do! navigator.share(sharePayloadFile title file)
      return Shared
    with error ->
      if isAbort error then
        return Cancelled
      else
        return! downloadFallback()
}

# Implementation Plan - File handling, deep links, share target, share (H1 to H5)

> Status: implemented. Gates at review: builds clean, 31 browser tests, 30 e2e scenarios, 4 service-worker scenarios, manifest carries the four new members, fantomas clean.
> Style: Simplified Technical English (ASD-STE100). Procedural sentences: 20 words or fewer. Descriptive sentences: 25 words or fewer.

## 1. Slice H1 - `.briple` file handling

### 1.1 Files

- Edit `vite.config.ts`: add `file_handlers` and `launch_handler` to the manifest (1.2).
- New `src/App/Intents.fsi` + `Intents.fs`: the module of section 1.3; H1 implements `init` up to the launch consumer.
- Edit `src/Program.fs`: call `App.Intents.init()` after `App.State.init`, before `Html.mount`.
- Edit `src/Briple.fsproj`: `App\Intents.fsi` + `Intents.fs` between `App\Pwa.fs` and `App\Chrome.fsi`.
- Edit `src/App/Today.fs` and `src/App/PlanPage.fs`: both file inputs take `accept=".txt,.briple,text/plain"`.

### 1.2 Manifest

```ts
launch_handler: { client_mode: 'focus-existing' },
file_handlers: [
  { action: './', accept: { 'text/plain': ['.briple'] } },
],
```

### 1.3 The `App.Intents` module

The module grows across the slices; H1 lays it out once. The signature file declares the surface below; every `[<Emit>]` binding lives in a nested `Dom` module the signature does not declare, because Fable drops emits that a signature exposes (the `Locale.Intl` pattern). Typed F# first: `navigator.share`, `navigator.clipboard`, and `ShareData` come from `Fable.Browser.Navigator`; `File.Create` from `Fable.Browser.Dom`; `URL` and `URLSearchParams` from `Fable.Browser.Url`; `Fetch.fetch` from `Fable.Fetch`. The launch-queue and Cache Storage types are typed `[<Global>]` interfaces in the module; the emits are only the platform probes, the data-carrying `canShare`, the AbortError check, and the download idiom. `File.Create`, `URL.Create`, and the package adds join `src/Briple.fsproj`.

```fsharp
module App.Intents

/// How a share attempt ended. `Shared` is the sheet's own success; the OS
/// showed its feedback. `Copied` and `Downloaded` are fallback successes;
/// the caller toasts for `Copied`. `Failed` carries the error text.
type ShareResult =
  | Shared
  | Copied
  | Downloaded
  | Cancelled
  | Failed of string

/// Arms the launch-queue consumer (H1), routes the deep link (H2), and
/// reads the share handoff (H3). Call once at boot, after State.init.
/// Launches buffer until this runs, so the call order is safe.
val init: unit -> unit

/// Downloads one text file through the browser's download path. The
/// Export copy command and the share-file fallback share this path.
val downloadText: name: string -> text: string -> unit

// --- H4 additions -----------------------------------------------------------

/// Opens the share sheet with one text file. `Cancelled` on the sheet's
/// cancel. Any missing API or other failure falls back to the download.
val shareFile: title: string -> name: string -> text: string -> JS.Promise<ShareResult>

/// Opens the share sheet with one text payload. `Cancelled` on the sheet's
/// cancel. Any missing API or other failure falls back to the clipboard,
/// whose success is `Copied`; a clipboard failure too is `Failed`.
val shareText: title: string -> text: string -> JS.Promise<ShareResult>
```

`Dom` members H1 needs:

- `launchQueueExists()`, `setLaunchConsumer(LaunchParams -> unit)`, guarded by `'launchQueue' in window`.
- `launchFiles(LaunchParams)`: the `files` list, empty when the property is absent.
- `fileFromHandle(handle)`: `handle.getFile()`.
- `createTextFile(name, text)`: `new File([text], name, { type: 'text/plain' })`.
- `downloadText` backs the public `downloadText`; the emit moves verbatim from `Shell.Dom`, and `Shell.Dom` drops its copy.

### 1.4 Semantics

1. `Program.fs` calls `Intents.init` after `State.init`; the state vars exist by then, and `stageImport` only needs them plus the router.
2. Without `window.launchQueue`, `init` skips the consumer and does nothing else. The API is absent, not blocked.
3. With the consumer armed: a launch whose `files` list is empty is a plain launch; return with no state change.
4. Take the first handle, `getFile()`, then `State.importFile file`. Drop extra handles: the app holds one active plan.
5. The consumer stays armed for the app's life. `focus-existing` delivers every later launch into the running window.

### 1.5 Acceptance

- On an installed desktop Chromium build, opening a `.briple` file from the file manager lands on the import preview; commit loads the plan.
- The Today and Plan pickers accept `.briple` files.
- Gates green (section 6).

## 2. Slice H2 - `web+bripple` deep links

### 2.1 Manifest

```ts
protocol_handlers: [
  { protocol: 'web+bripple', url: './?link=%s' },
],
```

The `%s` token is mandatory; the handled link arrives as the `link` query value on the start page.

### 2.2 Link grammar

The handled link is a URL; the host names the destination.

| Link | Landing |
|---|---|
| `web+bripple://today` | Today |
| `web+bripple://plan` | The Plan page |
| `web+bripple://session` | The selected day's session detail; Today when no plan is loaded, because the detail renders nothing then |
| `web+bripple://import` | Today's empty state when nothing is loaded; the Plan page, whose load command is there, when a plan is loaded |
| `web+bripple://import?src=<http(s) url>&name=<file name>` | Fetches the plan file at `src` and stages it through the preview; `name` defaults to the address's last path segment, else `plan.briple` |

An unknown host lands on Today, quiet. Every landing strips the `link` param with `history.replaceState` before any other work.

### 2.3 Semantics

1. `init` reads `location.search` once. A missing `link` param ends this path.
2. `new URL` parses the value under a guard; the protocol must be `web+bripple:`. A malformed link degrades to Today with no state change.
3. The param strip runs first, then the route, so a reload never re-enters a link, even while a fetch is in flight.
4. Route mapping follows section 2.2 and goes through `State.goTo`, so browser back behaves as on every spoke.
5. An `import` link with `src` must carry an `http:` or `https:` address; anything else degrades quietly. In the installed app the browser's mixed-content rule blocks insecure fetches on its own. The fetch is the typed `Fetch.fetch` (the package fails non-2xx on its own); `State`'s own fetch emit cannot cross its signature. A failed fetch or a parse error lands on Today's error card, like every import.
6. The preview is the gate for `src`: the fetched text stays staged until the user commits, and nothing reaches the store before that.

### 2.4 Acceptance

- After install, `web+bripple://plan` opens the Plan page; `web+bripple://session` opens the selected day's detail; `web+bripple://today` lands on Today.
- `web+bripple://import?src=<address of a plan file>` stages that file under the link's `name`; commit loads it.
- After any deep link, the address bar carries no `link` param, and a reload does not re-enter the link.
- `web+bripple://session` without a loaded plan lands on Today, not on a blank page.
- Gates green (section 6).

## 3. Slice H3 - Share target and the custom service worker

### 3.1 Files

- Edit `package.json`: add `workbox-precaching` at `7.4.1` to `dependencies` (the version in the lock via vite-plugin-pwa; pnpm's strict layout hides transitive packages).
- Edit `vite.config.ts`: `strategies: 'injectManifest'`, `srcDir: 'src'`, `filename: 'sw.ts'`, and the `share_target` member (3.2).
- New `src/sw.ts`: the worker of section 3.3.
- Edit `src/App/Intents.fs(i)`: the handoff read joins `init`.
- New `tests/sw.mjs` and the `test:sw` script (section 5.2).

### 3.2 Manifest

```ts
share_target: {
  action: './share-target',
  method: 'POST',
  enctype: 'multipart/form-data',
  params: {
    files: [{ name: 'plan', accept: ['text/plain', '.briple', '.txt'] }],
    text: 'text',
    title: 'name',
  },
},
```

### 3.3 Service worker (`src/sw.ts`)

```ts
/// <reference lib="webworker" />
import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching';

precacheAndRoute(self.__WB_MANIFEST);
cleanupOutdatedCaches();
```

1. A `fetch` listener answers `POST` requests whose path ends with `/share-target`; every other request keeps workbox's routing. Declare the listener before the workbox calls read cleanly, but only the POST branch calls `respondWith`.
2. The handler runs `request.formData()`. One `plan` file entry wins; its `text()` is the payload and the file's name travels with it. With no file, the `text` field is the payload and `name` is its file name.
3. The handler stores the payload as the response for `/__pending-share` in the cache named `briple-handoff`: the first line is the file name, the remainder is the plan text, so the read side parses with plain string ops. The handler answers with `Response.redirect` to the registration scope's start URL, status 303. `Response.redirect` needs an absolute URL; build it from `self.registration.scope`.
4. The literals `briple-handoff` and `/__pending-share` appear in `sw.ts` and `Intents.fs`; both carry a keep-in-sync comment, like the sample fixture.

### 3.4 Boot handoff

1. `init` runs the deep link first (section 2.3) and the handoff read last, so shared content owns the screen when a launch carries both.
2. The read: `caches.open('briple-handoff')`, `match('/__pending-share')`, `text()`. A missing entry, a missing `caches` API, or any error in the chain ends the work with no state change. The first line is the file name, the remainder the text; a missing field ends the work.
3. A valid entry runs `State.stageImport name text`, then `cache.delete('/__pending-share')`. A parse error lands on Today's error card, like every import.

### 3.5 Acceptance

- On an installed Android or Windows build, sharing a `.briple` or `.txt` file to Briple opens the app on the preview with the file's content verbatim; commit loads the plan.
- Sharing plain text from another app stages the text under the shared title.
- The offline story is unchanged: `pnpm preview` serves the cached shell with the network cut.
- Gates green (section 6), including `pnpm test:sw`.

## 4. Slices H4 and H5 - Share commands

### 4.1 Files

- Edit `src/App/Intents.fs(i)`: `shareFile`, `shareText`, and the clipboard and sheet emits join `Dom`.
- Edit `src/App/Shell.fsi` + `Shell.fs`: the two share commands; the Export copy command switches to `Intents.downloadText`.
- Edit `src/App/SessionDetail.fsi` + `SessionDetail.fs`: `sessionText` (4.2).
- Edit `src/App/Locale.fsi` + `Locale.fs`: four keys (4.5).

### 4.2 Contracts

```fsharp
// App.SessionDetail, added to the existing signature.

/// The selected day's session as plain text: title, date caption, plan-week
/// badge, circuit groups with round counts and order idioms, indented
/// scheme and note lines, and the guía trailer. No trailing newline.
val sessionText:
  strings: Strings ->
  plan: Plan ->
  anchor: DateOnly ->
  view: Genero * string ->
  date: DateOnly ->
    string
```

`Dom` members H4 needs: `canShareFiles(file)`, `canSharePayload()` (`'share' in navigator`), `share(payload)`, `clipboardWrite(text)` guarded by `'clipboard' in navigator`.

### 4.3 Text shape

The renderer mirrors the session detail page, line for line:

```
Full Body A
Lunes · 14 de septiembre
Semana 1 de 4 · RIR 1-2

Circuito A · 4 vueltas
A1 Sentadilla con barra
   6-8 · 1-2 · 3 min

Circuito B · 3 vueltas
B1 Press militar
   8-12 · 1-2 · 2-3 min

· Descansos: ...
· Regla de circuito: ...
```

Rules: the title falls back to the locale weekday, like the page. The badge line drops when `planWeek` gives None; `tryMesocicloRir` adds the RIR suffix. A circuit without rounds renders `Circuito A` alone. Scheme and note lines indent three spaces and drop when absent. Trailer lines keep the `· ` prefix and `guideTrailer`'s order. Builders: `detailGroups`, `schemeLine`, and `guideTrailer` already exist; `sessionText` composes them into strings.

### 4.4 Commands

No command carries a platform gate; `Html.show` reasons about content only. The sheet is attempted, and the run-time answer picks the path.

1. Today bar, primary area, after the info command: `metro-app-bar-button`, icon `share`, label `s.ShareDay`, `aria-label` the same. `Html.show` gate: `App.Today.hasSelectedSession()` only.
2. On click: `sessionText` from `parsed`, `activeImport`, `view`, and `selectedDate`; then `shareText plan.Titulo text`. `Copied` toasts `ToastCopied`; `Failed` toasts `ShareFailed` with the error text; `Shared` and `Cancelled` stay quiet.
3. Plan bar, `menu` slot, before the delete command: icon `share`, label `s.SharePlan`. The bar itself gates on the active import; no extra gate exists.
4. On click: `shareFile plan.Titulo import.FileName import.Raw`. `Failed` toasts `ShareFailed`; every other result stays quiet.
5. The fallbacks live inside `Intents`: `shareFile` asks `canShareFiles` first and downloads on a negative answer; a sheet error that is not a cancel also downloads. `shareText` writes the clipboard when the sheet is absent or errors; a clipboard failure too is `Failed`. No path dead-ends without feedback.

### 4.5 Locale

| Key | es | en |
|---|---|---|
| `SharePlan` | Compartir plan | Share plan |
| `ShareDay` | Compartir día | Share day |
| `ShareFailed` | No se pudo compartir | Could not share |
| `ToastCopied` | Copiado al portapapeles | Copied to the clipboard |

### 4.6 Acceptance

- On a phone or Windows machine, the Plan page's share command opens the sheet with the plan file; the target app receives the raw text.
- On a platform without file shares, the same command downloads the file; no command is ever absent for a platform.
- On a session day, the Today share command opens the sheet with the session text of the selected date; the text follows section 4.3. Where the sheet is absent, the text lands on the clipboard under one toast.
- Rest days and the quiet states show no share command; a content gate, not a platform gate.
- A cancelled sheet changes nothing; a failed share toasts once.
- Gates green (section 6).

## 5. Tests

`tests/AGENTS.md` rules apply. All new coverage is e2e: the code under test is app shell and worker, which the browser lane keeps out. Stubs drive the real production surface through the standard API boundary; no test-only hooks enter the app.

### 5.1 `tests/e2e.mjs` additions

1. **File launch:** an init script captures `launchQueue.setConsumer` into a stub; after boot, the stub fires the captured consumer with `{ files: [fakeHandle] }`, where `fakeHandle.getFile` resolves a `File` of the fixture. Assert the preview opens, commit lands on Today, and the toast shows.
2. **Deep link routing:** `page.goto(url + '/?link=' + encodeURIComponent(inner))` for three links. `web+bripple://plan` renders the Plan page; `web+bripple://session` after an import renders the session detail; `web+bripple://nosuch` lands on Today. Each landing carries no `link` param, and a reload does not re-enter.
3. **Deep link import:** `web+bripple://import?src=<the dev server's /samples/plan_entrenamiento_4sem.txt>&name=plan.briple` fetches, stages, and opens the preview; commit lands on Today with the plan loaded.
4. **Share day through the sheet:** an init script installs `navigator.canShare` (true) and a capturing `navigator.share`. Import the fixture, open the detail, go back, tap the share command. Assert one call: `title` is the plan title, `text` contains the title line, the date caption, the week badge, an `A1` row, and a trailer line. Pinned clock: 2026-09-14, Full Body A.
5. **Share plan file through the sheet:** the same stubs on the Plan page. Assert `files[0].name` is the fixture name and the file text equals the fixture text.
6. **File-share fallback:** a context without share stubs, so `navigator.share` is absent. Tap the Plan page's share command; assert the download event fires with the fixture's name and content.
7. **Text-share fallback:** a context with the clipboard permissions granted and no share stub. Tap the Today share command; assert the clipboard holds the session text and the `ToastCopied` toast shows.

### 5.2 New `tests/sw.mjs` lane, `pnpm test:sw`

The dev server has no service worker, so this lane owns the worker scenarios. Script: `"test:sw": "dotnet fable src && vite build && node tests/sw.mjs"`. The script serves `dist/` through Vite's `preview` API, then drives Playwright.

1. **Registration:** the page reaches `navigator.serviceWorker.ready`.
2. **File share:** from the controlled page, `fetch('share-target', { method: 'POST', body: formData })` with one `plan` entry holding the fixture as a `File` named `plan.briple`. The worker answers the redirect. Reload, assert the preview shows the fixture's title, commit, and Today shows the plan.
3. **Text share:** the same POST with a `text` field and no file. The preview stages the text under the default name.
4. **Handoff is one-shot:** after the staging above, a plain reload shows Today, not the preview.
5. **Offline:** `context.setOffline(true)`, reload, the cached shell renders.

### 5.3 Browser lane

No new suite. The new F# surface behind the commands is app shell; the e2e drives it.

## 6. Gates

All green before the next slice:

1. `dotnet build`: zero errors, zero warnings, src and tests.
2. `pnpm test:browser`: every existing test green; no new suite.
3. `pnpm test:e2e`: every scenario green, old and new. Zero failures, zero page errors.
4. `pnpm test:sw`: every worker scenario green (from H3 on).
5. `pnpm build`: exit 0, and `dist/manifest.webmanifest` carries `file_handlers`, `protocol_handlers`, `share_target`, and `launch_handler`.
6. fantomas clean on touched files.

## 7. Changelog draft

```markdown
### Added

- **Import:** `.briple` plan files open in Briple from the file manager on
  desktop; the app stages them through the usual preview.
- **Import:** sharing a plan file or plan text to Briple from another app
  opens the preview with the shared content; installed apps on Android and
  Windows list Briple in the share sheet.
- **Shell:** `web+bripple` deep links open Briple on Today, the Plan page,
  or the session detail, and `web+bripple://import?src=...` pulls a plan
  straight from a web address into the preview; an installed app claims the
  scheme.
- **Plan:** a share command next to the export command sends the plan file
  through the device share sheet.
- **Today:** a share command next to the session-detail command shares the
  selected day's session as plain text.
```

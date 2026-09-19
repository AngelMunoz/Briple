# Implementation Plan — Plan Parser (XParsec) + IndexedDB Services

> Prerequisites for V1 of the training PWA (design in `../design/training-pwa-storyboards.md`).
> **Status:** research + implementation plan. No code written yet.
> **Inputs evaluated:** XParsec 1.0.0 (`E:\XParsec`, already referenced in `src/Briple.fsproj`) and Fable.Browser.IndexedDB 2.2.0 (`E:\fable-browser`, already referenced).
> Format under test: `E:\smolmachines\plan_entrenamiento_4sem.txt` (484 lines) + the F# `Plan` types.
> **Test lane:** the Mibo.Fable pattern (`E:\Mibo.Fable`) — QUnit in a real browser via Playwright. No Node test lane.

---

## 0. Verdicts up front

| Question | Verdict |
|---|---|
| Is XParsec a good fit for the format? | **Yes.** Pure F#, Fable-native (compiled with the same `dotnet fable` toolchain), always-backtracking alternatives, `parser { }` CE, formatted errors. Use it for line bodies; see §2 for the line/block split. |
| Are the Fable.Browser.IndexedDB bindings valid? | **Yes — keep them, no custom bindings needed.** v2.2.0 aligns with the current IDB spec and covers everything V1 needs (§4). They are *raw typed bindings* (fable-browser house style): event-based, `result: obj option`, no promise helpers. The risk is **not in the bindings** — it's in our wrapper (transaction auto-commit rule, §5.3) and in the **storage boundary mapping** (§5.4). |
| Is `System.DateOnly` supported? | **Yes.** `fable-library-js.5.17.2/DateOnly.js` is present in `src/fable_modules` — Fable 5.17 ships the runtime. Domain code can use `DateOnly`; the storage boundary still encodes it as an ISO string (§5.4). |
| Do we need new dependencies? | **NuGet: none.** npm: dev-only, and only for the test lane — `qunit` + `playwright` (vite already present), mirroring Mibo.Fable. No `fake-indexeddb`, no Node test runner: store tests run against **real IndexedDB in Chromium**. |

---

## 1. XParsec — what we're working with

- Generic over input collections (`Parser<'Parsed,'T,'State,'Input>`); for us always `string` via `Reader.ofString`.
- `parser { … }` CE is the idiomatic style (`let!`, `do!`, `<|>`, `|>>`, `>>.`, `between`, `manyChars`, `pint32`…).
- **Always-backtracking `<|>`/`choice`** — no `attempt`; alternatives restore position on failure. Simplifies line-body grammars.
- No line tracking by default → we keep line numbers by parsing *per line* (§2), which makes every error carry its line index for free.
- Errors format via `ErrorFormatting.formatStringError input err` (the README's `Ln X, Col Y` tree output) — good enough to embed in `ParseError.Detail`.

## 2. Parser architecture

### 2.1 Strategy: line-classifier + XParsec line bodies + recursive block assembler

The format is line-oriented with balanced `#start <kind> … #end <kind>` blocks, not a token-dense grammar. Two options:

- (a) One char-level XParsec parser over the whole file with newline-aware state — possible, but line numbers and block-nesting errors become our problem inside combinator land.
- (b) **Hybrid (recommended):** split into `Line[]` (index + text), classify each line by prefix, parse each line's *body* with a small XParsec parser, and assemble blocks by recursion with a context stack. Errors get exact line numbers trivially; malformed nesting ("#end dia while inside semana 2") is reported by our own code with domain vocabulary.

### 2.2 Grammar (from the fixture)

| Line form | Fields | Constraints |
|---|---|---|
| `#start plan v1` | version int | root; `v` prefix + int |
| `#meta <key> <value…>` | key word, value = rest of line (spaces, `·`, parens, `/` allowed) | valid in `plan` (`titulo`, `semanas`, `generado`, `origen`) and `dia` (`titulo`) |
| `#start genero <Nombre>` | `Hombre`/`Mujer` (else warning + skip) | 0..n in plan |
| `#start opcion <id>` | id word (`3dias`, `5dias`…) | inside genero; `#meta titulo` allowed inside |
| `#start semana <N>` | int | inside opcion |
| `#start dia <Nombre>` | weekday name | inside semana; `#meta titulo` allowed |
| `#ej A\|4\|1\|Nombre\|Reps\|Rir\|Descanso\|Notas` | exactly 7 `\|` separators, 8 fields; `Orden` int | only inside dia; `Nombre` must not contain `\|` (validate → warning); `-` markers kept verbatim |
| `#x <key> \| <texto…>` | key word, rest of line | plan-level extras (`decision`, `mesociclo`, `deload`, `recuperacion`, `regla-circuito`, `progresion`, `descansos`) |
| `#end <kind>` | must match innermost open block | mismatch = hard error |
| blank / whitespace-only lines | — | tolerated |

Nesting: `plan > genero > opcion > semana > dia`. Unknown `#start` kind → **balanced skip** (count nested starts/ends until the matching `#end`) + warning; the raw file is stored anyway, so skipping is lossless overall. Unknown `#meta`/`#x` keys → warning + preserve nothing structured (raw keeps them).

### 2.3 Types & result model

- `Plan.Types.fs`: the user-supplied types **verbatim** (`Ejercicio`, `Dia`, `Semana`, `Opcion`, `Genero`, `Bloque`, `Plan`) plus:
  - `ParseWarning = { Line: int; Kind: WarningKind; Message: string }` with `WarningKind = UnknownDayName | UnknownBlockSkipped | UnknownMetaKey | BadEjField | DuplicateEntry | EmptyDia | UnsupportedVersion`
  - `ParseError = { Line: int; Message: string; Detail: string option }` (`Detail` = formatted XParsec error when a line body failed)
  - `ParsedPlan = { Plan: Plan; Warnings: ParseWarning list }`
- `Plan.Parser.fs`:
  - `pMeta`, `pEj`, `pStart`, `pEnd`, `pExtra` — XParsec line-body parsers (each runs via `Reader.ofString` on the single line).
  - Context stack assembler: `parsePlan/parseGenero/parseOpcion/parseSemana/parseDia` consume `Line[]` until the matching `#end`; `skipBalanced` for unknown blocks.
  - Day-name table: fixed es/en map ("Lunes"/"Monday"→Monday …). Unmapped `Dia.Id` → `UnknownDayName` warning; the day **stays in the model** and projection skips it (matches design §7).
  - `parsePlan: string -> Result<ParsedPlan, ParseError>`.

### 2.4 Project layout

One app project: the parser lives in `src/Plan/Types.fs` + `src/Plan/Parser.fs`, the store in `src/Store/Db.fs`, all compiled by `src/Briple.fsproj` (no separate library projects — this is an app, not a library). The test project (`tests/Briple.Tests.fsproj`) compiles those same sources directly, keeping the demo app (`Program.fs`, metrino) out of the test lane.

## 3. Fixture & test data

- Copy `plan_entrenamiento_4sem.txt` into `tests/fixtures/` (generic gym plan, no personal data).
- Positive test: parse fixture → assert tree (2 generos; Hombre: 3dias (4 semanas × Lun/Mié/Vie), 5dias (× Lun–Vie); Mujer: 5dias; `#ej` lines verbatim incl. time-based reps "30-45 s" and Rir "-"; extras: **32 lines across 7 keys** — decision 10, mesociclo 5, recuperacion 5, regla-circuito 4, deload 3, progresion 3, descansos 2) and **zero warnings**.
- Negative fixtures (each asserts the right error/warning + line number): `#end` mismatch; unterminated `#start dia`; `#ej` with 6 separators; `#ej` with `|` inside name; unknown `#start semanax` block (balanced skip); duplicate `#start semana 2`; `Dia.Id` "Samedi" (unmapped name → warning; "Sábado" is mapped); `#start plan v2` (UnsupportedVersion warning).

## 4. Fable.Browser.IndexedDB — evaluation

### 4.1 Coverage vs. our V1 needs

| Need (from design §7) | Binding | OK? |
|---|---|---|
| Open DB with version, `onupgradeneeded`, `blocked` | `IDBFactory.open`, `IDBOpenDBRequest.onupgradeneeded: IDBVersionChangeEvent` (has `oldVersion`/`newVersion`) | ✅ |
| Create/delete stores + indexes on upgrade | `IDBDatabase.createObjectStore/deleteObjectStore`, `IDBObjectStore.createIndex` (string/seq/U2 overloads) | ✅ |
| Key-value + keyed records | `put/add/get/getAll/getAllKeys/getKey/delete/clear/count`, `IDBKeyRange.only/bound/lowerBound/upperBound` | ✅ |
| Transactions incl. multi-store | `transaction(storeNames: string # #seq<string>, mode, ?durability)`, `abort`, `commit`, `oncomplete/onerror/onabort` | ✅ |
| Version change between tabs | `IDBDatabase.onversionchange`, `IDBFactory.deleteDatabase`, `databases()` | ✅ |
| Cursors | `openCursor/openKeyCursor`, `IDBCursor(WithValue)` | ✅ (unused in V1) |

Type-nits found (all harmless, handled in the wrapper): `IDBIndex.get/getKey` keys are optional (spec requires them — we always pass one); `IDBObjectStore.getAll/count` accept only `IDBKeyRange` (spec also allows bare keys — not needed by us); `IDBRequest.result: obj option` needs casting; `DatabasesType.version: int64` (only on `databases()`, which we don't use); `IDBTransactionDuarability` (sic, typo in binding name only).

### 4.2 What it deliberately is not

Raw event-based types, no promise wrapper, no migration helper — that's the fable-browser house style (mirror the DOM API). Not a defect; it means the service layer is ours to write (~200 lines).

### 4.3 Verdict

**Keep the bindings; do not write custom ones, do not add `idb` npm.** The wrapper is small, and owning it keeps the transaction semantics explicit (§5.3) and the storage boundary mapped (§5.4) — which is where promise-wrapping libraries actually bite.

## 5. Storage design (`src/Store/`)

### 5.1 Schema — `briple-training`, version 1

- Store `imports`, keyPath `id` (generated: filename stem + short hash), index `importedAt`.
- Store `state`: out-of-line string keys (no keyPath; `put(value, key)`), for `activeImportId`, `viewState { Genero; OpcionId }`, `selectedDate`, theme/dismissed prefs.

### 5.2 Stored record — Raw is the source of truth

The parsed `Plan` is **not persisted** — `Raw` is, and parsing (~500 lines, trivially fast) happens on load into memory. Raw is lossless (unknown keys are never lost), store/display can never diverge, and a future format version "upgrades" by re-parsing.

### 5.3 Service layer — the part that must be right

`Db.fs` (single module is enough for V1):

1. **Promise wrapper**: `requestAsPromise (req: IDBRequest)` — resolve `req.onsuccess` → cast `req.result`, reject `req.onerror`/`req.error`. And `txAsPromise (tx)` for `oncomplete`/`onabort`/`onerror`.
2. **Open + migrate**: `openDb ()` → `indexedDB.open("briple-training", 1)`; in `onupgradeneeded`, `switch e.oldVersion` → v1 creates both stores + index. Cached connection promise; `onversionchange → close()` (three lines, correct multi-tab behavior).
3. **The #1 IndexedDB pitfall, encoded**: **a transaction auto-commits when the microtask queue drains.** Never `await` between requests of the same transaction. Pattern: open tx → issue *all* requests synchronously → return a promise that resolves on `tx.oncomplete`. Multi-store ops (put import + set `activeImportId`) are one `readwrite` transaction over both stores.
4. Typed API (thin, over the raw bindings):
   - `getActiveImport (): StoredImport option Promise`
   - `putImport (StoredImport): unit Promise` (also sets active pointer)
   - `listImports (): StoredImport list Promise` (via index, newest first)
   - `deleteImport (id): unit Promise` (+ clear active pointer if it was active)
   - `getState<T> (key): T option Promise` / `setState (key, value): unit Promise`

### 5.4 Storage boundary — explicit mapping, no record assumptions

Structured clone **strips prototypes**: whatever we `put()` comes back as a plain object literal. F# records under Fable are not plain JS objects we can rely on across that round trip — Fable constructs records with its own representations, and code assuming record semantics (equality, members, type identity) is only guaranteed against objects *Fable built*. Reading back an unmapped blob from IndexedDB is exactly how those assumptions break.

Rule: **no F# value crosses the IDB boundary unmapped.**

- Storage DTOs are **flat anonymous records** (plain object literals, primitive fields only):

```fsharp
// write side
let toStorage (s: StoredImport) =
    {| id = s.Id; fileName = s.FileName; importedAt = s.ImportedAt
       anchor = DateOnly.toIsoString s.Anchor; raw = s.Raw |}
// read side: ofStorage builds a fresh StoredImport from the plain object
```

- `StoredImport` (domain) ↔ `{| … |}` (storage) via explicit `toStorage`/`ofStorage` in `Db.fs`; every field is a string/int, so structured clone is trivially safe and the DTO shape is versionable (additive).
- `DateOnly` domain-wide (runtime confirmed present), encoded as ISO `"yyyy-MM-dd"` at the boundary.
- `state` store: primitives stored as-is; anything structured (e.g. `ViewState`) is written as a JSON string and decoded on read — again through one mapping point, not by casting raw objects into record types.

### 5.5 App-side types (from the design, unchanged)

`ViewState { Genero: Genero; OpcionId: string }` lives in `state` (JSON-encoded per §5.4); `Anchor` lives on the import; projection (`date → session?`) stays a pure function over `(StoredImport, ViewState, date)` — it belongs with `Briple.Plan` or the app core, not with the DB code.

## 6. Risk register

| Risk | Status |
|---|---|
| `System.DateOnly` under Fable 5.17 | **Resolved** — `DateOnly.js` (+ `DateOnlyTemporal.js`) present in `src/fable_modules/fable-library-js.5.17.2/`; use it, encode as ISO string at the boundary. |
| Fable `option` representation in structured clone | **Moot by design** — parsed `Plan` is never persisted (§5.2); nothing with `option` fields crosses the boundary. |
| IDB transaction auto-commit breaking the promise wrapper | Handled as a rule in §5.3.3; the browser store tests cover the multi-request transaction case explicitly. |
| XParsec emit under Fable 5.17 | Low — XParsec's own suite compiles via `dotnet fable`; the browser parser tests confirm on first run. |

## 7. Test strategy — one lane: QUnit in a real browser (Mibo.Fable pattern)

Compile with `dotnet fable tests/Briple.Tests.fsproj --noCache --sourceMaps`, run the suite in Chromium via Playwright. No Node-only lane.

- **Framework + bindings:** QUnit with F# bindings (`QUnit.module'`, `QUnit.test`, `QUnit.testAsync`, `Assert.ok/strictEqual/deepEqual/throws`), adapted from `Mibo.Testing.QUnit` (Perla.Fable.QUnit lineage) — copy the binding file into `tests/`.
- **Harness:** `tests/index.html` — `QUnit.config.autostart = false` *before* QUnit loads, dynamic-import the compiled `*Tests.fs.js` modules, wire `QUnit.on("runEnd")` → `window.__runEnd` (status/counts/failures/runtime).
- **Runner:** `tests/browser.mjs` — programmatic vite server + Playwright Chromium (fallback channels `msedge`/`chrome`), navigate to the harness, await `window.__runEnd`, print failures, exit code. `pnpm test:browser` script; failures logged per test.
- **Parser suites** (pure F#, no DOM needed — same lane keeps one environment): fixture assertions from §3; `strictEqual`/`deepEqual` on tree shapes; warning and error line numbers.
- **Store suites** — the reason this lane exists: **real IndexedDB in Chromium**
  - open + migrate (create stores/index on `onupgradeneeded`),
  - put → get round trip **through the mapping layer**: assert the read-back domain value equals the original (`StoredImport` equality via mapped field comparison),
  - read-back object is a plain literal (assert no prototype assumptions — e.g. mapped access works, direct cast isn't relied upon),
  - two-store transaction (put import + set active pointer) commits atomically,
  - `deleteImport` clears active pointer; `onversionchange` closes the connection.
- **No app/UI tests** in this phase.

## 8. Implementation order (when green-lit)

1. Test lane scaffolding: `tests/Briple.Tests.fsproj` (FABLE_COMPILER) + QUnit bindings + `index.html` + `browser.mjs` + `pnpm test:browser` (dev-deps: `qunit`, `playwright`).
2. `src/Plan/Types.fs` + `src/Plan/Parser.fs` compiled by `src/Briple.fsproj`; fixture copied; `dotnet build` + fantomas clean.
3. Parser suites green in the browser lane (§3).
4. `src/Store/Db.fs` (wrapper + migrations + §5.4 mappings) — store suites green against real Chromium IndexedDB.
5. (Out of scope here) wire into app state / UI.

Estimated size: parser ~300–400 LOC + tests; store ~200 LOC + tests; harness ~150 LOC (adapted from Mibo.Fable). npm dev-deps: `qunit`, `playwright` only.

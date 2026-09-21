# Briple.Tests

Two lanes, both run from the repository root:

```shell
pnpm test:browser   # domain suite: parser, store, projection, variants
pnpm test:e2e       # end-to-end scenarios against the real app
```

`pnpm test:browser` Fable-compiles `tests/Briple.Tests.fsproj` with
`--noCache`, then `tests/browser.mjs` serves `tests/index.html` through Vite
and drives headless Chromium with Playwright. The harness auto-imports every
compiled `*Tests.fs.js`; the store tests need the real IndexedDB, so there is
no Node test runner. A new test module needs one registration: a `Compile`
entry in `tests/Briple.Tests.fsproj`.

`pnpm test:e2e` recompiles `src` first, then drives the real app: real store,
real Intl, and a clock pinned with `page.clock.install`. UI behavior is only
covered there; the browser lane compiles the sources under test directly and
keeps `Program.fs` and the app shell out.

## What a test must be

These rules are not suggestions. A test that breaks them does not join the
suite, even when it is green.

1. **Assert behavior, not plumbing.** Test through the surface that is meant
   to survive: parse real file text with `parsePlan` and assert on the `Plan`
   tree, call the projection with dates and assert on the session, round-trip
   domain values through the typed store API. Never assert on parser
   combinator internals, the IndexedDB request wrapper, or the storage DTO
   shape. A test must not freeze transitional machinery.

2. **Regression tests prove both directions.** A regression test for a bug
   must fail before the fix and pass after it. Run it against the unfixed
   code before you keep it.

3. **Test-owned data only.** The fixture lives in
   `tests/fixtures/plan_entrenamiento_4sem.txt`; negative cases are inline
   strings with known line numbers in `Parser.Tests.fs`. Every expected value
   derives from them. Never assert the bundled app copy under
   `public/samples/` (the two files must stay identical, but the test owns its
   copy). A test that needs a plan shape the fixture does not have authors it
   with the synthetic builders in `Projection.Tests.fs` or an inline string,
   never app boot state.

4. **The API is never shaped for tests.** Do not add flags, getters, or test
   hooks to the parser, store, or projection so a test can assert. Domain
   functions return what the app reads (`Result`, `Plan`,
   `StoredImport option`); a rejected input warns through `ParsedPlan` or
   fails through `ParseError`. Tests accommodate the API, never the reverse.

5. **No dev shortcuts, no test-only hooks.** Name and trigger the behavior
   through the production surface: browser tests call the public module
   function; e2e drives the real app (real clicks, the real file input, the
   real preview commit). Do not add test-only ids, attributes, or direct
   state writes so a test can reach in. The one deliberate exception is the
   boot fallback scenario, which seeds IndexedDB before the app starts to pin
   that state - it still asserts through the rendered UI.

6. **Respect the async lanes.** An IndexedDB transaction auto-commits when
   the microtask queue drains: never `await` between requests of one
   transaction, and write store tests as `QUnit.testAsync` awaiting the typed
   API. Var writes land in the next render: e2e waits for rendered text,
   never sleeps. The e2e clock is pinned, so date-dependent assertions stay
   deterministic.

7. **End to end is the shape for regressions.** A bug in app behavior gets a
   scenario in `tests/e2e.mjs` that drives the real flow; a bug in the
   parser, projection, or store gets a browser-lane test. A mid-level test
   may stay when it pins a distinct contract, but a real bug gets a test
   that drives the real thing.

8. **Read the mapped domain surface, never the raw store.** Structured clone
   strips prototypes, so no F# value crosses the IndexedDB boundary unmapped.
   Assert on values built by `ofStorage` or returned by the typed store API
   (`getActiveImport`, `listImports`, `getViewState`), never on the raw object
   read from IndexedDB. If the value you need is not mapped, the DTO is
   wrong: extend `toStorage`/`ofStorage`.

## Where the helpers live

- `tests/fixtures/plan_entrenamiento_4sem.txt` - the real 4-week plan every
  positive suite parses (identical copy: `public/samples/`; keep in sync).
- `Parser.Tests.fs` - inline negative fixtures; `hasWarning`, `bloque`, and
  `opcion` helpers.
- `Projection.Tests.fs` - synthetic `plan` / `semana` / `dia` / `ej`
  builders; fixture anchor is Monday 2026-09-07.
- `Variants.Tests.fs` - display, group, and `resolveView` tests over the
  fixture.
- `Store.Tests.fs` - real IndexedDB round trips through the typed API.
- `QUnit.fs` / `QUnit.fsi` - the QUnit bindings (`QUnit.module'`, `test`,
  `testAsync`, `Assert`).
- `browser.mjs` and `index.html` - the browser harness that auto-imports
  every `*Tests.fs.js` and reports through `window.__runEnd`.
- `e2e.mjs` - the app scenarios, with the `see`, `commitPreview`, and
  `seeSelectedDay` helpers.

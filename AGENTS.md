# AGENTS.md

Briple is a local-first training PWA: F# compiled to JS with Fable, Metro UI web components from `@angelmunoz/metrino`, pnpm + Vite. No backend; all data lives in IndexedDB. Primary device is a phone.

## Imperatives

1. **NEVER PUSH WITHOUT PERMISSION.** Always ask before pushing to the remote. A previous permission push for one set of work DOES NOT MEAN ALWAYS PUSH AFTER. Permissions are granted per set of work, not for the session.
2. **NEVER FORCE PUSH.** Tell the user they have to force push instead of you.
3. **USE YOUR EDIT TOOL FOR FILE EDITS**: Do not use sed, python, perl or any other scripting language or cli command to write/update files and its contents.
4. **Always run `dotnet fantomas .` before committing code.** Format all F# files before staging.
5. **Prefer `Option.*` (or `ValueOption`) combinators over nested matches.** When threading optional values, chain `Option.map` / `bind` / `filter` / `iter` / `defaultValue` / `orElse` instead of hand-rolling `match ... with | Some x -> ... | None -> ...` ladders. Match only when the two branches carry substantially different logic, not to unwrap and re-wrap.
6. Pull requests made with the `gh` command use a markdown file as the PR body, not inline escaped markdown.
7. New F# files **MUST** be paired with its own signature file for app and library code, tests and samples do not require signature files.
8. Comments go to the signature files. implementation quirk comments may stay in implementation files.
9. Report to me only in ASD-STE100 Simplified Technical English.
10. For non-reports, do not use LLM jargon, phrases that are textual noise without actual value. Speak simple plain, concise and direct english.

## Commands

**DO NOT GO OFF RAILS**: you must use the package.json commands, do not use npx, pnpx or any unauthorized code for project management.

- `pnpm start` - Fable watch of `src` + Vite dev server.
- `pnpm build` - `dotnet fable src` only. It emits `*.fs.js`; it does not bundle.
- `pnpm test:browser` - Fable-compiles `tests/Briple.Tests.fsproj` with `--noCache`, then runs the QUnit suite in headless Chromium (Playwright). There is no Node test runner; the harness auto-imports every `tests/*Tests.fs.js`.
- `pnpm test:e2e` - recompiles `src`, then runs the Playwright scenarios against the real app. `page.clock.install` pins the browser clock, so assertions are deterministic.
- `dotnet build` - builds the solution; the repo gate requires zero errors and zero warnings.
- `dotnet fantomas check <paths>` / `dotnet fantomas <paths>` - format check / format. `dotnet tool restore` runs via `postinstall`.

Gate before calling work done (the gate list in the active `docs/plan/` slice plan): `dotnet build`, `pnpm test:browser`, `pnpm test:e2e`, `pnpm build`, fantomas clean.

## Fable

- Think about the shape of the output code it will be JS code that runs in a browser runtime, we should optimize for that.
  use `let inline <fn name> <args>`, in general to erase the F# generated surface if can help the js runtime avoid indirection.
- Fable translates anonymous records to POJOs or you can use `[<JS.Pojo>]` decorator on records. Keep in mind that pojo decorated records lose any record runtime behavior.
- For further information visit https://fable.io/docs/javascript/features.html

## Fable.Ripple

Fable.Ripple is a Solid.js-like Framework based on signals, state management is done through signals.
Keep solid.js-like guidelines when you author new UI features, this includes composition and state management.

## Metrino

Metrino is a Metro Design Language web component library, it is not UWP, not Windows10+ Design, it is not WinUI or other design language.
The app itself should be treated as a STRICT Metro Design Language (MDL) implementation.

## Testing

Testing has a dedicated agents file [AGENTS.md](./tests/AGENTS.md) make sure to check it before writing/updating any existing test.

## Architecture

- `src/Plan/` - `Types.fs` (domain types), `Parser.fs` (XParsec; line classifier + recursive block assembler), `Projection.fs` (pure date to session). Plan must never reference Store.
- `src/Store/Db.fs` - IndexedDB (`briple-training`). Raw file text is the source of truth; the plan is re-parsed on load, never persisted. No F# value crosses the IDB boundary unmapped: use flat anonymous-record DTOs (`toStorage`/`ofStorage`). Never `await` between requests of one transaction (it auto-commits).
- `src/App/` - state (`State.fs` holds the Vars + persistence) and views. `src/Program.fs` is the boot point: registers metrino elements, reads the store, and mounts `App.Shell.view()`.
- `Metrino.Ripple/` - vendored typed bindings for the metrino web components (project reference, hand-written, not generated). Elements must be registered (`registerMetroX`) before use; some register through dynamic imports at boot (`Program.fs`), so never add static imports of their subpaths. Upstream metrino bugs must be reported to the developer before adding "workarounds".

## Devlog Management

We follow https://github.com/ionide/KeepAChangelog guidelines. The categories are Added, Changed, Deprecated, Removed, Fixed and Security.

The changelog is written for a developer who upgrades to your version, not as a development journal.

1. **One bullet per user-facing change, not one per commit.** Collapse several fixes in one subsystem into one bullet that names each briefly.
2. **Group by concern, not by task.**
3. **Only released code can be Changed or Fixed.** A feature that has never shipped goes in Added, and its design notes belong in that entry. Mark breakage with **Breaking:** or **Breaking (behavioral):**.
4. **Bold-prefix the surface:** `**Shell:**`, `**Settings:**`, `**Today:**`, `**Weekly:**`. Keep breaking changes at the top of their category.
5. **Plain language.** Describe the user-visible effect, not the code diff. No internal file paths, no milestone or phase numbers, no section references.

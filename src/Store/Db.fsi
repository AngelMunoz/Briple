module Briple.Store

open System
open Fable.Core
open Browser.Types
open Plan.Types

module Iso =

  val ofDateOnly: date: DateOnly -> string

  val toDateOnly: value: string -> DateOnly

type StoredImport = {
  Id: string
  FileName: string
  ImportedAt: string // ISO-8601 timestamp
  Anchor: DateOnly
  Raw: string // the plan file, verbatim — the source of truth
}

type ViewState = { Genero: Genero; OpcionId: string }

/// State-lane keys owned by the theme module. The other lanes' keys stay
/// implementation details of this module.
[<Literal>]
val ThemeKey: string = "theme"

[<Literal>]
val AccentKey: string = "accent"

/// Runs work with the (cached) database connection.
val withConnection: work: (IDBDatabase -> JS.Promise<'a>) -> JS.Promise<'a>

/// Closes and forgets the cached connection; the next call reopens.
val closeConnection: unit -> JS.Promise<unit>

/// Stores an import; the row's id is the key, so an identical re-import
/// overwrites in place.
val putImport: import: StoredImport -> JS.Promise<unit>

/// All stored imports, newest first (by the ISO importedAt timestamp).
val listImports: unit -> JS.Promise<StoredImport list>

/// Deletes an import row.
val deleteImport: id: string -> JS.Promise<unit>

val getStateRaw: key: string -> JS.Promise<string option>

val setStateRaw: key: string -> value: string -> JS.Promise<unit>

/// Removes the state entry under `key`; absent keys are not an error.
val deleteStateRaw: key: string -> JS.Promise<unit>

val getViewState: unit -> JS.Promise<ViewState option>

val setViewState: view: ViewState -> JS.Promise<unit>

/// Empties both stores in one transaction: every import and every state
/// entry. The database itself and the schema survive.
val clearAllData: unit -> JS.Promise<unit>

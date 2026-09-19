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

/// Runs work with the (cached) database connection.
val withConnection: work: (IDBDatabase -> JS.Promise<'a>) -> JS.Promise<'a>

/// Closes and forgets the cached connection; the next call reopens.
val closeConnection: unit -> JS.Promise<unit>

/// The import flagged active in `state`, or None when nothing is loaded.
val getActiveImport: unit -> JS.Promise<StoredImport option>

/// Stores an import and flags it active in one two-store transaction.
val putImport: import: StoredImport -> JS.Promise<unit>

/// All stored imports, newest first (by the ISO importedAt timestamp).
val listImports: unit -> JS.Promise<StoredImport list>

/// Deletes an import; clears the active pointer when it pointed at it.
val deleteImport: id: string -> JS.Promise<unit>

val getStateRaw: key: string -> JS.Promise<string option>

val setStateRaw: key: string -> value: string -> JS.Promise<unit>

val getViewState: unit -> JS.Promise<ViewState option>

val setViewState: view: ViewState -> JS.Promise<unit>

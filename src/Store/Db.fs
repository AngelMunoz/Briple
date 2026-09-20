module Briple.Store

// IndexedDB persistence for the training app. The
// Fable.Browser.IndexedDB bindings are raw event-based types; this module
// wraps them into promises, owns the schema migration, and maps every value
// crossing the storage boundary through flat plain-JS DTOs — structured
// clone strips prototypes, so no F# record semantics survive a round trip.
//
// Transaction rule encoded below: an IndexedDB transaction auto-commits when
// the microtask queue drains, so every request of a transaction is issued
// synchronously before anything is awaited; completion is observed on
// tx.oncomplete / tx.onerror.

open System
open Fable.Core
open Browser
open Browser.Types
open Plan.Types

[<Literal>]
let DatabaseName = "briple-training"

[<Literal>]
let DatabaseVersion = 1

[<Literal>]
let ImportsStore = "imports"

[<Literal>]
let StateStore = "state"

[<Literal>]
let ImportedAtIndex = "importedAt"

[<Literal>]
let ActiveKey = "activeImportId"

[<Literal>]
let ViewStateKey = "viewState"

// ISO yyyy-MM-dd encoding for the storage boundary. The app-side domain uses
// System.DateOnly (runtime confirmed present in fable-library-js 5.17).
module Iso =

  let ofDateOnly(date: DateOnly) =
    $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}"

  let toDateOnly(value: string) =
    match value.Split('-') with
    | [| year; month; day |] -> DateOnly(int year, int month, int day)
    | _ -> failwith $"not an ISO date: {value}"

type StoredImport = {
  Id: string
  FileName: string
  ImportedAt: string // ISO-8601 timestamp
  Anchor: DateOnly
  Raw: string
} // the plan file, verbatim — the source of truth

type ViewState = { Genero: Genero; OpcionId: string }

module Json =

  [<Emit("JSON.stringify($0)")>]
  let stringify(value: obj) : string = jsNative

  [<Emit("JSON.parse($0)")>]
  let parse(json: string) : obj = jsNative

// Plain-JS read shapes. Reads unbox into these interfaces (plain property
// access on plain objects); writes build plain object literals. Neither side
// relies on F# record identity or prototypes.
type ImportDto =
  abstract id: string
  abstract fileName: string
  abstract importedAt: string
  abstract anchor: string
  abstract raw: string

type ViewDto =
  abstract genero: string
  abstract opcionId: string

let toImportDto(import: StoredImport) : obj =
  box {|
    id = import.Id
    fileName = import.FileName
    importedAt = import.ImportedAt
    anchor = Iso.ofDateOnly import.Anchor
    raw = import.Raw
  |}

let ofImportObj(value: obj) : StoredImport =
  let dto = unbox<ImportDto> value

  {
    Id = dto.id
    FileName = dto.fileName
    ImportedAt = dto.importedAt
    Anchor = Iso.toDateOnly dto.anchor
    Raw = dto.raw
  }

let viewStateToJson(view: ViewState) : string =
  Json.stringify(
    box {|
      genero = Genero.toString view.Genero
      opcionId = view.OpcionId
    |}
  )

let viewStateOfJson(json: string) : ViewState option =
  try
    let dto = unbox<ViewDto>(Json.parse json)

    Genero.tryParse dto.genero
    |> Option.map(fun genero -> {
      Genero = genero
      OpcionId = dto.opcionId
    })
  with _ ->
    None

let describe(error: DOMException) : string =
  match Option.ofObj error with
  | Some e -> $"IndexedDB error: {e.message} ({e.name})"
  | None -> "IndexedDB error"

// IDBCreateStoreOptions is a bare JS object; build it directly.
[<Emit("({ keyPath: $0 })")>]
let keyPathOptions(keyPath: string) : IDBCreateStoreOptions = jsNative

let mutable connection: JS.Promise<IDBDatabase> option = None

let openDatabase() : JS.Promise<IDBDatabase> =
  Promise.create(fun resolve reject ->
    let request = indexedDB.``open``(DatabaseName, DatabaseVersion)

    request.onupgradeneeded <-
      fun event ->
        let db = unbox<IDBDatabase> request.result.Value
        let versionChange = unbox<IDBVersionChangeEvent> event

        if versionChange.oldVersion < 1L then
          let imports = db.createObjectStore(ImportsStore, keyPathOptions "id")
          imports.createIndex(ImportedAtIndex, ImportedAtIndex) |> ignore
          db.createObjectStore(StateStore) |> ignore

    request.onsuccess <-
      fun _ ->
        let db = unbox<IDBDatabase> request.result.Value
        // Yield the connection when another tab upgrades the DB.
        db.onversionchange <- fun _ -> db.close()
        resolve db

    request.onerror <- fun _ -> reject(exn(describe request.error))
    request.blocked <- fun _ -> ())

let database() : JS.Promise<IDBDatabase> =
  match connection with
  | Some cached -> cached
  | None ->
    // Do not cache failures: the next call retries the open.
    let fresh =
      openDatabase()
      |> Promise.catch(fun err ->
        connection <- None
        raise err)

    connection <- Some fresh
    fresh

// Exposed for tests (and later for the app) that need the raw connection.
let withConnection(work: IDBDatabase -> JS.Promise<'a>) : JS.Promise<'a> =
  database() |> Promise.bind work

let closeConnection() : JS.Promise<unit> =
  match connection with
  | None -> Promise.lift()
  | Some cached ->
    connection <- None
    cached |> Promise.map(fun db -> db.close())

let requestAsPromise(request: IDBRequest) : JS.Promise<obj> =
  Promise.create(fun resolve reject ->
    request.onsuccess <-
      fun _ -> resolve(Option.defaultValue null request.result)

    request.onerror <- fun _ -> reject(exn(describe request.error)))

/// The import flagged active in `state`, or None when nothing is loaded.
let getActiveImport() : JS.Promise<StoredImport option> =
  withConnection(fun db ->
    Promise.create(fun resolve reject ->
      let tx =
        db.transaction(
          [| StateStore; ImportsStore |],
          IDBTransactionMode.Readonly
        )

      let activeRequest = tx.objectStore(StateStore).get(box ActiveKey)

      // Second request is issued inside the first handler: the
      // transaction stays alive because no await happens between
      // the two synchronous requests.
      activeRequest.onsuccess <-
        fun _ ->
          match activeRequest.result with
          | Some id ->
            let importRequest = tx.objectStore(ImportsStore).get(id)

            importRequest.onsuccess <-
              fun _ ->
                match importRequest.result with
                | Some value -> resolve(Some(ofImportObj value))
                | None -> resolve None

            importRequest.onerror <-
              fun _ -> reject(exn(describe importRequest.error))
          | None -> resolve None

      activeRequest.onerror <-
        fun _ -> reject(exn(describe activeRequest.error))

      tx.onerror <- fun _ -> reject(exn(describe tx.error))))

/// Stores an import and flags it active in one two-store transaction.
let putImport(import: StoredImport) : JS.Promise<unit> =
  withConnection(fun db ->
    Promise.create(fun resolve reject ->
      let tx =
        db.transaction(
          [| ImportsStore; StateStore |],
          IDBTransactionMode.Readwrite
        )

      tx.objectStore(ImportsStore).put(toImportDto import) |> ignore
      tx.objectStore(StateStore).put(box import.Id, box ActiveKey) |> ignore
      tx.oncomplete <- fun _ -> resolve()
      tx.onerror <- fun _ -> reject(exn(describe tx.error))))

/// All stored imports, newest first (by the ISO importedAt timestamp).
let listImports() : JS.Promise<StoredImport list> =
  withConnection(fun db ->
    Promise.create(fun resolve reject ->
      let tx = db.transaction([| ImportsStore |], IDBTransactionMode.Readonly)

      let request =
        tx.objectStore(ImportsStore).index(ImportedAtIndex).getAll()

      request.onsuccess <-
        fun _ ->
          let imports =
            match request.result with
            | Some value ->
              unbox<obj array> value
              |> Array.map ofImportObj
              |> Array.sortByDescending(fun import -> import.ImportedAt)
              |> Array.toList
            | None -> []

          resolve imports

      request.onerror <- fun _ -> reject(exn(describe request.error))))

/// Deletes an import; clears the active pointer when it pointed at it.
let deleteImport(id: string) : JS.Promise<unit> =
  withConnection(fun db ->
    Promise.create(fun resolve reject ->
      let tx =
        db.transaction(
          [| ImportsStore; StateStore |],
          IDBTransactionMode.Readwrite
        )

      tx.objectStore(ImportsStore).delete(box id) |> ignore
      let activeRequest = tx.objectStore(StateStore).get(box ActiveKey)

      activeRequest.onsuccess <-
        fun _ ->
          match activeRequest.result with
          | Some current when unbox<string> current = id ->
            tx.objectStore(StateStore).delete(box ActiveKey) |> ignore
          | _ -> ()

      activeRequest.onerror <-
        fun _ -> reject(exn(describe activeRequest.error))

      tx.oncomplete <- fun _ -> resolve()
      tx.onerror <- fun _ -> reject(exn(describe tx.error))))

let getStateRaw(key: string) : JS.Promise<string option> =
  withConnection(fun db ->
    let tx = db.transaction([| StateStore |], IDBTransactionMode.Readonly)

    requestAsPromise(tx.objectStore(StateStore).get(box key))
    |> Promise.map(fun value ->
      if isNull value then None else Some(unbox<string> value)))

let setStateRaw (key: string) (value: string) : JS.Promise<unit> =
  withConnection(fun db ->
    Promise.create(fun resolve reject ->
      let tx = db.transaction([| StateStore |], IDBTransactionMode.Readwrite)
      tx.objectStore(StateStore).put(box value, box key) |> ignore
      tx.oncomplete <- fun _ -> resolve()
      tx.onerror <- fun _ -> reject(exn(describe tx.error))))

let getViewState() : JS.Promise<ViewState option> =
  getStateRaw ViewStateKey
  |> Promise.bind(fun json -> Promise.lift(json |> Option.bind viewStateOfJson))

let setViewState(view: ViewState) : JS.Promise<unit> =
  setStateRaw ViewStateKey (viewStateToJson view)

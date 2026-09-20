module Store.Tests

// Browser-lane tests for the IndexedDB store: these run against the real
// Chromium IndexedDB via the Playwright harness (Mibo.Fable pattern). They
// cover the migration, the DTO mapping round trip, the two-store
// transaction, and reconnection behavior.

open Fable.Core
open System
open Browser.Types
open Briple.Testing.QUnit
open Briple.Store
open Plan.Types

QUnit.``module`` "Plan store"

[<Emit("$0.objectStoreNames.contains($1)")>]
let hasStore (db: IDBDatabase) (name: string) : bool = jsNative

let sample (id: string) (importedAt: string) : StoredImport = {
  Id = id
  FileName = id + ".txt"
  ImportedAt = importedAt
  Anchor = Iso.toDateOnly "2026-09-14"
  Raw = "#start plan v1\n#end plan\n"
}

QUnit.testAsync(
  "opening the database creates the v1 stores",
  fun assert' -> promise {
    let! created =
      withConnection(fun db ->
        Promise.lift(hasStore db "imports" && hasStore db "state"))

    assert'.ok(created, "imports and state stores exist after migration")
  }
)

QUnit.testAsync(
  "put then get active round-trips through the mapping",
  fun assert' -> promise {
    let original = sample "store-roundtrip" "2026-09-19T10:00:00"
    do! putImport original
    let! loaded = getActiveImport()
    assert'.ok(loaded.IsSome, "active import is present")

    match loaded with
    | Some loaded ->
      assert'.equal(loaded.Id, original.Id, "id")
      assert'.equal(loaded.FileName, original.FileName, "fileName")
      assert'.equal(loaded.ImportedAt, original.ImportedAt, "importedAt")

      assert'.equal(
        Iso.ofDateOnly loaded.Anchor,
        "2026-09-14",
        "anchor survives the ISO mapping"
      )

      assert'.equal(loaded.Raw, original.Raw, "raw survives verbatim")
    | None -> ()
  }
)

QUnit.testAsync(
  "listImports returns newest first",
  fun assert' -> promise {
    do! putImport(sample "list-old" "2026-09-01T08:00:00")
    do! putImport(sample "list-new" "2026-09-30T08:00:00")
    let! imports = listImports()

    let indexOf id =
      imports |> List.tryFindIndex(fun import -> import.Id = id)

    match indexOf "list-old", indexOf "list-new" with
    | Some oldIndex, Some newIndex ->
      assert'.ok(newIndex < oldIndex, "the newer import comes first")
    | _ -> assert'.ok(false, "both imports are present in the list")
  }
)

QUnit.testAsync(
  "deleteImport clears the active pointer when it pointed at it",
  fun assert' -> promise {
    do! putImport(sample "delete-me" "2026-09-19T12:00:00")
    let! activeBefore = getActiveImport()

    assert'.equal(
      (activeBefore |> Option.map(fun s -> s.Id)),
      Some "delete-me",
      "active before delete"
    )

    do! deleteImport "delete-me"
    let! activeAfter = getActiveImport()

    assert'.equal(
      (activeAfter |> Option.map(fun s -> s.Id)),
      None,
      "active pointer cleared"
    )

    let! remaining = listImports()

    assert'.ok(
      remaining |> List.forall(fun s -> s.Id <> "delete-me"),
      "deleted import removed"
    )
  }
)

QUnit.testAsync(
  "view state round-trips through its json mapping",
  fun assert' -> promise {
    do! setViewState { Genero = Mujer; OpcionId = "5dias" }
    let! view = getViewState()

    assert'.ok(
      (view = Some { Genero = Mujer; OpcionId = "5dias" }),
      "mujer view state survives"
    )

    do! setViewState { Genero = Hombre; OpcionId = "3dias" }
    let! view = getViewState()

    assert'.ok(
      (view = Some { Genero = Hombre; OpcionId = "3dias" }),
      "hombre view state survives"
    )
  }
)

QUnit.testAsync(
  "reopening the connection keeps stored data",
  fun assert' -> promise {
    do! putImport(sample "reopen" "2026-09-19T14:00:00")
    do! closeConnection()
    let! loaded = getActiveImport()

    assert'.ok(
      loaded |> Option.exists(fun s -> s.Id = "reopen"),
      "data survives a reconnect"
    )
  }
)

QUnit.testAsync(
  "selectedDate round-trips through the raw state lane",
  fun assert' -> promise {
    do! setStateRaw "selectedDate" (Iso.ofDateOnly(DateOnly(2026, 9, 16)))
    let! raw = getStateRaw "selectedDate"

    assert'.equal(raw, Some "2026-09-16", "stored verbatim as ISO")

    let restored = raw |> Option.map Iso.toDateOnly

    assert'.equal(
      restored |> Option.map Iso.ofDateOnly,
      Some "2026-09-16",
      "ISO maps back to the date"
    )
  }
)

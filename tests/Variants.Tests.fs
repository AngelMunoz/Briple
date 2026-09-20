module Variants.Tests

// Browser-lane tests for the variant chrome: the opcion display helper, the
// view-chip group builder, and the boot view resolution with its store
// rewrite flag. They parse the real 4-week plan file.

open Fable.Core
open Briple.Testing.QUnit
open Plan.Types
open Plan.Parser
open Briple.Store
open App.Variants

QUnit.``module`` "Variants"

type Fetch =
  [<Emit("fetch($0).then((response) => response.text())")>]
  static member text(url: string) : JS.Promise<string> = jsNative

QUnit.test(
  "display renders the dias suffix with the unit word",
  fun assert' ->
    assert'.equal(display "días" "3dias", "3 días", "3dias in es")
    assert'.equal(display "días" "5dias", "5 días", "5dias in es")
    assert'.equal(display "days" "3dias", "3 days", "3dias in en")
    // A non-integer prefix, a bare suffix, and a plain id pass through.
    assert'.equal(
      display "días" "alternadias",
      "alternadias",
      "non-integer prefix"
    )

    assert'.equal(display "días" "dias", "dias", "bare suffix")
    assert'.equal(display "días" "hibrida", "hibrida", "id without the suffix")
)

QUnit.testAsync(
  "groups list every bloque in file order with display labels",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"

    match parsePlan text with
    | Error error ->
      assert'.ok(false, $"unexpected parse error: {error.Message}")
    | Ok parsed ->
      let groups' = groups parsed.Plan "días"

      assert'.equal(groups'.Length, 2, "one group per bloque")

      match groups' with
      | [ hombre; mujer ] ->
        assert'.ok(hombre.Genero = Hombre, "first group is Hombre")
        assert'.ok(mujer.Genero = Mujer, "second group is Mujer")

        assert'.equal(
          (hombre.Items |> List.map(fun item -> item.Id) |> String.concat ","),
          "3dias,5dias",
          "Hombre opciones in file order"
        )

        assert'.equal(
          (mujer.Items |> List.map(fun item -> item.Id) |> String.concat ","),
          "5dias",
          "Mujer opciones in file order"
        )

        assert'.equal(
          hombre.Items.Head.Display,
          "3 días",
          "display helper applied to the item"
        )

        assert'.equal(
          hombre.Items.Head.Label,
          "3 días — FULL BODY A / B / C (LUNES · MIÉRCOLES · VIERNES)",
          "label is display plus titulo"
        )

        assert'.equal(
          mujer.Items.Head.Label,
          "5 días — L-X-V TREN INFERIOR · M-J TREN SUPERIOR",
          "Mujer item label"
        )
      | _ -> assert'.ok(false, "expected exactly two groups")
  }
)

QUnit.testAsync(
  "resolveView falls back to the first variant and flags the rewrite",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"

    match parsePlan text with
    | Error error ->
      assert'.ok(false, $"unexpected parse error: {error.Message}")
    | Ok parsed ->
      let plan = parsed.Plan

      // The stored view names a missing variant: first variant wins, and
      // the store value must be rewritten.
      let missing = Some { Genero = Hombre; OpcionId = "7dias" }

      let (genero, opcionId), rewrite = resolveView plan missing

      assert'.ok((genero = Hombre), "fallback genero is the first bloque")

      assert'.equal(
        opcionId,
        "3dias",
        "fallback opcion is the first in file order"
      )

      assert'.ok(rewrite, "missing variant flags a store rewrite")

      // A valid stored view is kept as is.
      let valid = Some { Genero = Mujer; OpcionId = "5dias" }

      let (genero, opcionId), rewrite = resolveView plan valid

      assert'.ok((genero = Mujer), "valid stored genero kept")
      assert'.equal(opcionId, "5dias", "valid stored opcion kept")
      assert'.notOk(rewrite, "valid stored view never rewrites")

      // No stored view: default view, no rewrite.
      let (genero, opcionId), rewrite = resolveView plan None

      assert'.ok((genero = Hombre), "default genero")
      assert'.equal(opcionId, "3dias", "default opcion")
      assert'.notOk(rewrite, "missing stored view does not rewrite")
  }
)

module Parser.Tests

// Browser-lane tests for the plan format parser
// the real 4-week plan file; negative fixtures are inline strings with
// known line numbers.

open Fable.Core
open Briple.Testing.QUnit
open Plan.Types
open Plan.Parser

QUnit.``module`` "Plan parser"

type Fetch =
  [<Emit("fetch($0).then((response) => response.text())")>]
  static member text(url: string) : JS.Promise<string> = jsNative

let parse(text: string) = parsePlan text

let hasWarning (kind: WarningKind) (line: int) (warnings: ParseWarning list) =
  warnings
  |> List.exists(fun warning -> warning.Kind = kind && warning.Line = line)

let bloque (plan: Plan) (genero: Genero) =
  plan.Bloques |> List.find(fun b -> b.Genero = genero)

let opcion (bloque: Bloque) (id: string) =
  bloque.Opciones |> List.find(fun o -> o.Id = id)

let assertSamplePlan (assert': Assert) (text: string) =
  match parse text with
  | Error error ->
    assert'.ok(
      false,
      $"unexpected parse error at line {error.Line}: {error.Message}"
    )
  | Ok parsed ->
    let plan = parsed.Plan

    assert'.ok(
      List.isEmpty parsed.Warnings,
      $"expected no warnings, got {parsed.Warnings}"
    )

    assert'.equal(plan.Version, 1, "version")

    assert'.equal(
      plan.Titulo,
      "Plan de Entrenamiento en Circuito - 4 Semanas",
      "titulo"
    )

    assert'.equal(plan.Semanas, 4, "semanas")
    assert'.equal(plan.Generado, "2026-09-19", "generado")

    assert'.equal(
      plan.Origen,
      "Plan_Entrenamiento_Circuito_4_Semanas (2).xlsx",
      "origen"
    )

    assert'.equal(plan.Bloques.Length, 2, "generos in file")

    let hombre = bloque plan Hombre
    let mujer = bloque plan Mujer

    assert'.deepEqual(
      hombre.Opciones |> List.map(fun o -> o.Id),
      [ "3dias"; "5dias" ],
      "hombre opciones"
    )

    assert'.deepEqual(
      mujer.Opciones |> List.map(fun o -> o.Id),
      [ "5dias" ],
      "mujer opciones"
    )

    let tresDias = opcion hombre "3dias"

    assert'.equal(
      tresDias.Titulo,
      Some "FULL BODY A / B / C (LUNES · MIÉRCOLES · VIERNES)",
      "opcion titulo"
    )

    assert'.deepEqual(
      tresDias.Semanas |> List.map(fun s -> s.Numero),
      [ 1; 2; 3; 4 ],
      "3dias semanas"
    )

    let semana1 = List.head tresDias.Semanas

    assert'.deepEqual(
      semana1.Dias |> List.map(fun d -> d.Id),
      [ "Lunes"; "Miércoles"; "Viernes" ],
      "3dias dias in semana 1"
    )

    let lunes = List.head semana1.Dias
    assert'.equal(lunes.Titulo, Some "Full Body A", "dia titulo")
    assert'.equal(lunes.Ejercicios.Length, 6, "lunes ejercicio count")

    let first = List.head lunes.Ejercicios
    assert'.equal(first.Circuito, "A", "circuit")
    assert'.equal(first.Vueltas, "4", "vueltas (verbatim string)")
    assert'.equal(first.Orden, 1, "orden")
    assert'.equal(first.Nombre, "Sentadilla con barra", "nombre")
    assert'.equal(first.Reps, "6-8", "reps verbatim")
    assert'.equal(first.Rir, "1-2", "rir verbatim")
    assert'.equal(first.Descanso, "3 min", "descanso verbatim")
    assert'.equal(first.Notas, "-", "notas verbatim")

    let viernes = semana1.Dias |> List.find(fun d -> d.Id = "Viernes")

    let plancha =
      viernes.Ejercicios
      |> List.find(fun e -> e.Nombre = "Plancha / rueda abdominal")

    assert'.equal(plancha.Reps, "30-45 s", "time-based reps verbatim")
    assert'.equal(plancha.Rir, "-", "absent rir stays '-'")

    let cincoDias = opcion hombre "5dias"

    assert'.equal(
      cincoDias.Semanas |> List.forall(fun s -> s.Dias.Length = 5),
      true,
      "5dias trains five days per week"
    )

    let mujerSemanas = List.head mujer.Opciones

    assert'.equal(
      mujerSemanas.Semanas |> List.forall(fun s -> s.Dias.Length = 5),
      true,
      "mujer plan trains five days per week"
    )

    assert'.equal(plan.Extras.Length, 32, "extras count")

    assert'.ok(
      plan.Extras.[0].StartsWith "decision | ",
      "first extra keeps its key prefix"
    )

    assert'.equal(
      plan.Extras
      |> List.filter(fun extra -> extra.StartsWith "mesociclo")
      |> List.length,
      5,
      "mesociclo entries"
    )

QUnit.testAsync(
  "the sample plan parses with zero warnings",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"
    assertSamplePlan assert' text
  }
)

QUnit.test(
  "a mismatched #end is a hard error",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#end opcion\n#end plan\n"

    match parse fixture with
    | Error error ->
      assert'.equal(error.Line, 5, "error points at the mismatched line")
    | Ok _ -> assert'.ok(false, "expected a parse error")
)

QUnit.test(
  "an unterminated block is a hard error",
  fun assert' ->
    match
      parse
        "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n"
    with
    | Error error ->
      assert'.ok(
        error.Message.StartsWith "unterminated",
        "reports the unterminated block"
      )
    | Ok _ -> assert'.ok(false, "expected a parse error")
)

QUnit.test(
  "a malformed #ej row warns instead of failing",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#start opcion 3dias\n#start semana 1\n#start dia Lunes\n#ej A|4|Sentadilla|6-8|1-2|3 min\n#end dia\n#end semana\n#end opcion\n#end genero\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.ok(
        hasWarning BadEjField 8 parsed.Warnings,
        "bad row reported at its line"
      )

      assert'.ok(hasWarning EmptyDia 9 parsed.Warnings, "the day ends up empty")
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "an #ej row without a name warns",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#start opcion 3dias\n#start semana 1\n#start dia Lunes\n#ej A|4|1||6-8|1-2|3 min|-\n#end dia\n#end semana\n#end opcion\n#end genero\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.ok(
        hasWarning BadEjField 8 parsed.Warnings,
        "empty name reported at its line"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "an #ej row with a pipe inside the name warns",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#start opcion 3dias\n#start semana 1\n#start dia Lunes\n#ej A|4|1|Na|me|6-8|1-2|3 min|-\n#end dia\n#end semana\n#end opcion\n#end genero\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.ok(
        hasWarning BadEjField 8 parsed.Warnings,
        "pipe in name reported at its line"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "an unrecognized day name warns",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#start opcion 3dias\n#start semana 1\n#start dia Samedi\n#ej A|4|1|X|6-8|1-2|3 min|-\n#end dia\n#end semana\n#end opcion\n#end genero\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.ok(
        hasWarning UnknownDayName 7 parsed.Warnings,
        "unmapped day name reported"
      )

      assert'.equal(
        parsed.Plan.Bloques.[0].Opciones.[0].Semanas.[0].Dias.Length,
        1,
        "day kept in the model"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "an unknown block is skipped in a balanced way",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start semanax 1\n#start dia Lunes\n#end dia\n#end semanax\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.equal(parsed.Warnings.Length, 1, "exactly one warning")

      assert'.ok(
        hasWarning UnknownBlockSkipped 4 parsed.Warnings,
        "skip warning at line 4"
      )

      assert'.equal(
        parsed.Plan.Bloques.Length,
        0,
        "no genero parsed from the unknown block"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "a duplicate semana warns",
  fun assert' ->
    let fixture =
      "#start plan v1\n#meta titulo T\n#meta semanas 1\n#start genero Hombre\n#start opcion 3dias\n#start semana 1\n#end semana\n#start semana 1\n#end semana\n#end opcion\n#end genero\n#end plan\n"

    match parse fixture with
    | Ok parsed ->
      assert'.ok(
        hasWarning DuplicateEntry 8 parsed.Warnings,
        "duplicate reported at second start"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "an unsupported plan version warns but parses",
  fun assert' ->
    match
      parse "#start plan v2\n#meta titulo T\n#meta semanas 1\n#end plan\n"
    with
    | Ok parsed ->
      assert'.ok(
        hasWarning UnsupportedVersion 1 parsed.Warnings,
        "version warning at line 1"
      )
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "missing required meta warns at #end plan",
  fun assert' ->
    match parse "#start plan v1\n#end plan\n" with
    | Ok parsed ->
      assert'.equal(
        parsed.Warnings
        |> List.filter(fun w -> w.Kind = MissingMeta)
        |> List.length,
        2,
        "titulo and semanas both reported"
      )

      assert'.equal(parsed.Plan.Titulo, "", "titulo defaulted")
      assert'.equal(parsed.Plan.Semanas, 0, "semanas defaulted")
    | Error error ->
      assert'.ok(false, $"expected warnings, got error: {error.Message}")
)

QUnit.test(
  "the smallest valid plan parses cleanly",
  fun assert' ->
    match
      parse "#start plan v1\n#meta titulo T\n#meta semanas 1\n#end plan\n"
    with
    | Ok parsed ->
      assert'.ok(
        List.isEmpty parsed.Warnings,
        $"no warnings, got {parsed.Warnings}"
      )

      assert'.equal(parsed.Plan.Bloques.Length, 0, "no bloques")
    | Error error ->
      assert'.ok(false, $"expected a clean parse, got error: {error.Message}")
)

module Projection.Tests

// Browser-lane tests for the projection core. Positive asserts
// run against the real fixture (anchor Monday 2026-09-07)
// edge cases use synthetic plans built inline.

open System
open Fable.Core
open Briple.Testing.QUnit
open Plan.Types
open Plan.Parser
open Plan.Projection

QUnit.``module`` "Projection"

type Fetch =
  [<Emit("fetch($0).then((response) => response.text())")>]
  static member text(url: string) : JS.Promise<string> = jsNative

let anchor = DateOnly(2026, 9, 7) // Monday

// --- Synthetic plan builders -----------------------------------------------

let ej nombre = {
  Circuito = "A"
  Vueltas = "4"
  Orden = 1
  Nombre = nombre
  Reps = "8-10"
  Rir = "2"
  Descanso = "90 s"
  Notas = "-"
}

let dia id titulo dias = {
  Id = id
  Titulo = titulo
  Notas = []
  Ejercicios = dias
}

let semana numero dias = { Numero = numero; Dias = dias }

let opcion id semanas = {
  Id = id
  Titulo = None
  Semanas = semanas
}

let bloque genero opciones = { Genero = genero; Opciones = opciones }

let plan semanas generos = {
  Version = 1
  Titulo = "test plan"
  Semanas = semanas
  Generado = ""
  Origen = ""
  Bloques = generos
  Extras = []
}

// Hombre / 3dias: Lun · Mié · Vie (Full Body A/B/C), one week, for anchor tests
let threeDayWeek weekNumber =
  semana weekNumber [
    dia "Lunes" (Some "Full Body A") [ ej "sentadilla" ]
    dia "Miércoles" (Some "Full Body B") [ ej "peso muerto" ]
    dia "Viernes" (Some "Full Body C") [ ej "remo" ]
  ]

let threeDayPlan =
  plan 4 [ bloque Hombre [ opcion "3dias" [ threeDayWeek 1; threeDayWeek 2 ] ] ]

QUnit.testAsync(
  "fixture: session, week bounds, trainsOn, nextSession, mesociclo",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"

    match parsePlan text with
    | Error error -> assert'.ok(false, $"parse error at line {error.Line}")
    | Ok parsed ->
      let p = parsed.Plan

      // 2026-09-14 falls in plan week 2; 2026-09-16 is Full Body B
      assert'.equal(
        planWeek p anchor (DateOnly(2026, 9, 14)),
        Some 2,
        "week of 09-14"
      )

      match trySession p Hombre "3dias" anchor (DateOnly(2026, 9, 16)) with
      | None -> assert'.ok(false, "expected a session on 09-16")
      | Some dia ->
        assert'.equal(dia.Titulo, Some "Full Body B", "09-16 session")

      // Before and after the plan
      assert'.equal(
        trySession p Hombre "3dias" anchor (DateOnly(2026, 9, 6)),
        None,
        "before anchor"
      )

      assert'.equal(
        trySession p Hombre "3dias" anchor (DateOnly(2026, 10, 5)),
        None,
        "after week 4"
      )

      assert'.equal(
        planWeek p anchor (DateOnly(2026, 10, 5)),
        None,
        "planWeek after plan"
      )

      // 3 días trains Mon / Wed / Fri
      assert'.ok(
        trainsOn p Hombre "3dias" anchor (DateOnly(2026, 9, 14)),
        "trains Monday"
      )

      assert'.notOk(
        trainsOn p Hombre "3dias" anchor (DateOnly(2026, 9, 15)),
        "rest Tuesday"
      )

      assert'.ok(
        trainsOn p Hombre "3dias" anchor (DateOnly(2026, 9, 18)),
        "trains Friday"
      )

      // Rest-day line: next session from Saturday 09-12
      match nextSession p Hombre "3dias" anchor (DateOnly(2026, 9, 12)) with
      | None -> assert'.ok(false, "expected a next session after Saturday")
      | Some(nextDate, dia) ->
        assert'.ok(
          sameDate nextDate (DateOnly(2026, 9, 14)),
          "next session is Monday 09-14"
        )

        assert'.equal(dia.Titulo, Some "Full Body A", "next session title")

      // Mesociclo RIR targets from the #x extras
      assert'.equal(tryMesocicloRir p 1, Some "3", "mesociclo week 1")
      assert'.equal(tryMesocicloRir p 2, Some "2-3", "mesociclo week 2")
      assert'.equal(tryMesocicloRir p 4, Some "1", "mesociclo week 4")
      assert'.equal(tryMesocicloRir p 5, None, "week 5 has no RIR segment")
  }
)

QUnit.test(
  "custom (non-Monday) anchor shifts every window",
  fun assert' ->
    let wednesday = DateOnly(2026, 9, 9) // anchor mid-week

    // Week 1 covers 09-09..09-15, so Monday 09-14 is still week 1
    assert'.equal(
      planWeek threeDayPlan wednesday (DateOnly(2026, 9, 14)),
      Some 1,
      "09-14 in week 1"
    )

    assert'.equal(
      planWeek threeDayPlan wednesday (DateOnly(2026, 9, 16)),
      Some 2,
      "09-16 in week 2"
    )

    // 09-14 (Monday) resolves through week 1's Lunes dia
    match
      trySession threeDayPlan Hombre "3dias" wednesday (DateOnly(2026, 9, 14))
    with
    | None -> assert'.ok(false, "expected a session on 09-14")
    | Some dia ->
      assert'.equal(dia.Titulo, Some "Full Body A", "09-14 session via week 1")
)

QUnit.test(
  "defaultAnchor: this Monday when the variant trains today, else next Monday",
  fun assert' ->
    // Wednesday 09-09: the 3-day variant trains today, so this week's Monday
    assert'.ok(
      sameDate
        (defaultAnchor threeDayPlan Hombre "3dias" (DateOnly(2026, 9, 9)))
        (DateOnly(2026, 9, 7)),
      "trains today -> this Monday"
    )

    // Tuesday 09-08: no session today, so next week's Monday
    assert'.ok(
      sameDate
        (defaultAnchor threeDayPlan Hombre "3dias" (DateOnly(2026, 9, 8)))
        (DateOnly(2026, 9, 14)),
      "rest today -> next Monday"
    )

    // Monday itself counts as "this week's Monday"
    assert'.ok(
      sameDate
        (defaultAnchor threeDayPlan Hombre "3dias" (DateOnly(2026, 9, 7)))
        (DateOnly(2026, 9, 7)),
      "today is Monday"
    )
)

QUnit.test(
  "unmapped Dia.Id never matches a weekday",
  fun assert' ->
    let weirdWeeks =
      semana 1 [
        dia "Samedi" (Some "Weird") [ ej "x" ]
        dia "Lunes" (Some "Normal") [ ej "y" ]
      ]

    let saturdayPlan =
      plan 2 [ bloque Hombre [ opcion "1dia" [ weirdWeeks; weirdWeeks ] ] ]

    // Saturday 09-12: "Samedi" is unmapped, so the date is rest
    assert'.equal(
      trySession saturdayPlan Hombre "1dia" anchor (DateOnly(2026, 9, 12)),
      None,
      "unmapped day skipped"
    )

    // nextSession from Saturday skips Samedi and lands on Monday 09-14
    match
      nextSession saturdayPlan Hombre "1dia" anchor (DateOnly(2026, 9, 12))
    with
    | None -> assert'.ok(false, "expected to find Monday's session")
    | Some(nextDate, dia) ->
      assert'.equal(dia.Titulo, Some "Normal", "skips unmapped day")
)

QUnit.test(
  "short opcion: weeks beyond its Semanas render rest, planWeek still counts",
  fun assert' ->
    let shortPlan =
      plan 4 [
        bloque Hombre [ opcion "3dias" [ threeDayWeek 1; threeDayWeek 2 ] ]
      ]

    // Week 3 (09-21..09-27): the opcion has no week there
    assert'.equal(
      planWeek shortPlan anchor (DateOnly(2026, 9, 21)),
      Some 3,
      "plan still counts week 3"
    )

    assert'.equal(
      trySemana shortPlan Hombre "3dias" anchor (DateOnly(2026, 9, 21)),
      None,
      "opcion has no week 3"
    )

    assert'.equal(
      trySession shortPlan Hombre "3dias" anchor (DateOnly(2026, 9, 21)),
      None,
      "rest in week 3"
    )

    // Inside the opcion's own range it resolves normally
    match trySemana shortPlan Hombre "3dias" anchor (DateOnly(2026, 9, 14)) with
    | None -> assert'.ok(false, "expected week 2 of the opcion")
    | Some semana -> assert'.equal(semana.Numero, 2, "week 2 of the opcion")
)

QUnit.test(
  "malformed mesociclo extras yield None",
  fun assert' ->
    let p = {
      threeDayPlan with
          Extras = [
            "mesociclo | Semana 1: no RIR segment here"
            "mesociclo | Semana 2: bad | RIR"
            "mesociclo | other shape entirely"
            "decision | not a mesociclo"
          ]
    }

    assert'.equal(tryMesocicloRir p 1, None, "no RIR segment")
    assert'.equal(tryMesocicloRir p 2, None, "empty RIR token")
    assert'.equal(tryMesocicloRir p 3, None, "no Semana 3 header")
    assert'.equal(tryMesocicloRir p 4, None, "no mesociclo for week 4")
)

QUnit.testAsync(
  "weekRows: seven rows, three sessions, four rest, today bar",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"

    match parsePlan text with
    | Error error -> assert'.ok(false, $"parse error at line {error.Line}")
    | Ok parsed ->
      let rows =
        weekRows
          parsed.Plan
          Hombre
          "3dias"
          anchor
          (DateOnly(2026, 9, 16))
          (DateOnly(2026, 9, 14))

      assert'.equal(rows.Length, 7, "seven rows")

      assert'.ok(
        rows.[0].Date = DateOnly(2026, 9, 14),
        "first row is the Monday"
      )

      let sessionRows = rows |> List.filter(fun row -> row.Session.IsSome)
      assert'.equal(sessionRows.Length, 3, "three sessions in week 2")
      assert'.equal(rows.Length - sessionRows.Length, 4, "four rest rows")

      assert'.ok(rows.[2].IsToday, "Wednesday carries the today bar")
      assert'.notOk(rows.[1].IsToday, "Tuesday is not today")
      assert'.ok(rows.[1].Session.IsNone, "Tuesday projects as rest")

      match sessionRows.[0].Session with
      | None -> assert'.ok(false, "Monday row carries a dia")
      | Some dia ->
        assert'.equal(dia.Titulo, Some "Full Body A", "Monday title")
  }
)

QUnit.testAsync(
  "planChipState: before, inside, after",
  fun assert' -> promise {
    let! text = Fetch.text "./fixtures/plan_entrenamiento_4sem.txt"

    match parsePlan text with
    | Error error -> assert'.ok(false, $"parse error at line {error.Line}")
    | Ok parsed ->
      let p = parsed.Plan

      assert'.ok(
        planChipState p anchor (DateOnly(2026, 9, 6)) = StartsOn anchor,
        "before the plan names the start date"
      )

      assert'.ok(
        planChipState p anchor (DateOnly(2026, 9, 14)) = Inside(2, 4),
        "inside reads week 2 of 4"
      )

      assert'.ok(
        planChipState p anchor (DateOnly(2026, 10, 5)) = Completed,
        "after the plan is done"
      )
  }
)

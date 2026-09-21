module App.Variants

// Variant chrome: the display forms of the Genero/Opcion pairs, the flyout
// groups of the view chip, and the boot view resolution. Pure over the plan
// and the store's ViewState; every user-facing word arrives as an argument.

open System
open Plan.Types
open Briple.Store

type VariantItem = {
  Id: string
  Display: string
  Label: string
}

type VariantGroup = {
  Genero: Genero
  Items: VariantItem list
}

let display (unitWord: string) (id: string) : string =
  let suffix = "dias"

  if id.EndsWith suffix then
    let digits = id.Substring(0, id.Length - suffix.Length)

    match Int32.TryParse digits with
    | true, n -> $"{n} {unitWord}"
    | _ -> id
  else
    id

let groups (plan: Plan) (unitWord: string) : VariantGroup list =
  plan.Bloques
  |> List.map(fun bloque -> {
    Genero = bloque.Genero
    Items =
      bloque.Opciones
      |> List.map(fun opcion ->
        let shown = display unitWord opcion.Id

        {
          Id = opcion.Id
          Display = shown
          Label =
            opcion.Titulo
            |> Option.map(fun titulo -> $"{shown} — {titulo}")
            |> Option.defaultValue shown
        })
  })

let isValid (plan: Plan) (genero: Genero) (opcionId: string) : bool =
  plan.Bloques
  |> List.exists(fun bloque ->
    bloque.Genero = genero
    && bloque.Opciones |> List.exists(fun opcion -> opcion.Id = opcionId))

let firstVariant(plan: Plan) : Genero * string =
  plan.Bloques
  |> List.tryHead
  |> Option.bind(fun bloque ->
    bloque.Opciones
    |> List.tryHead
    |> Option.map(fun opcion -> (bloque.Genero, opcion.Id)))
  |> Option.defaultValue(Hombre, "3dias")

let resolveView
  (plan: Plan)
  (storedView: ViewState option)
  : (Genero * string) * bool =
  match storedView with
  | Some candidate when isValid plan candidate.Genero candidate.OpcionId ->
    ((candidate.Genero, candidate.OpcionId), false)
  | Some _ -> (firstVariant plan, true)
  | None -> (firstVariant plan, false)

let extraKey(text: string) : string * string =
  match text.IndexOf('|') with
  | -1 -> ("", text.Trim())
  | index -> (text.Substring(0, index).Trim(), text.Substring(index + 1).Trim())

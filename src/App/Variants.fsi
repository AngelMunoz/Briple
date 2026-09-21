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

/// "{n} {unitWord}" when the id ends in "dias" with an integer prefix
/// ("3dias" -> "3 días"); any other id passes through unchanged.
val display: unitWord: string -> id: string -> string

/// One group per Bloque in file order. Item label: "{display} — {titulo}",
/// or the display alone when the opcion has no titulo.
val groups: plan: Plan -> unitWord: string -> VariantGroup list

/// True when the pair names a real variant of the plan.
val isValid: plan: Plan -> genero: Genero -> opcionId: string -> bool

/// First Genero's first Opcion in file order; falls back to (Hombre, "3dias").
val firstVariant: plan: Plan -> Genero * string

/// The boot view: the stored view when it names a real variant, else the
/// first variant in file order. True flags that the stored view named a
/// missing variant, so the resolved view must be written back to the store.
val resolveView:
  plan: Plan -> storedView: ViewState option -> (Genero * string) * bool

/// Splits an `#x` extra ("key | body") into its key and body. Text without
/// the separator carries an empty key.
val extraKey: text: string -> string * string

open Js_of_ocaml

type js_string = Js.js_string Js.t

external dark_targetjs_digest384 : js_string -> js_string = "dark_targetjs_digest384"

let digest384 (input: string) : string =
  input
  |> Js.string
  |> dark_targetjs_digest384
  |> Js.to_string

let digest256 (input: string) : string =
  ""

let date_to_isostring (d: Core_kernel.Time.t) : string =
  ""

let date_of_isostring (str: string) : Core_kernel.Time.t =
  Core_kernel.Time.now ()

let date_to_sqlstring (d: Core_kernel.Time.t) : string =
  ""

let date_of_sqlstring (str: string) : Core_kernel.Time.t =
  Core_kernel.Time.now ()

let regexp_replace ~(pattern: string) ~(replacement: string) (str: string) : string =
  ""

let string_split ~(sep: string) (s: string) : string list =
  []
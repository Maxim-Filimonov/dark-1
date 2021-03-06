open Core_kernel
open Lib
open Types.RuntimeT
module RT = Runtime

(* type coerces one list to another using a function *)
let list_coerce ~(f : dval -> 'a option) (l : dval list) :
    ('a list, dval list * dval) Result.t =
  l
  |> List.map (fun dv ->
         match f dv with Some v -> Result.Ok v | None -> Result.Error (l, dv))
  |> Result.all


let error_result msg = DResult (ResError (Dval.dstr_of_string_exn msg))

let ( >>| ) = Result.( >>| )

let fns : fn list =
  [ { name = fn "" "toString" 0

    ; parameters = [Param.make "v" TAny]
    ; returnType = TStr
    ; description =
        "Returns a string representation of `v`, suitable for displaying to a user. Redacts passwords."
    ; fn =

          (function
          | _, [a] ->
              Dval.dstr_of_string_exn (Dval.to_enduser_readable_text_v0 a)
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = NotDeprecated }
  ; { name = fn "" "toRepr" 0

    ; parameters = [Param.make "v" TAny]
    ; returnType = TStr
    ; description =
        "Returns an adorned string representation of `v`, suitable for internal developer usage. Not designed for sending to end-users, use toString instead. Redacts passwords."
    ; fn =

          (function
          | _, [a] ->
              Dval.dstr_of_string_exn (Dval.to_developer_repr_v0 a)
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = ReplacedBy(fn "" "" 0) }
  ; { name = fn "" "equals" 0
    ; infix_names = ["=="]
    ; parameters = [Param.make "a" TAny; Param.make "b" TAny]
    ; returnType = TBool
    ; description = "Returns true if the two value are equal"
    ; fn =

          (function _, [a; b] -> DBool (equal_dval a b) | args -> incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = NotDeprecated }
  ; { name = fn "" "notEquals" 0
    ; infix_names = ["!="]
    ; parameters = [Param.make "a" TAny; Param.make "b" TAny]
    ; returnType = TBool
    ; description = "Returns true if the two value are not equal"
    ; fn =

          (function
          | _, [a; b] -> DBool (not (equal_dval a b)) | args -> incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = NotDeprecated }
  ; { name = fn "" "assoc" 0

    ; parameters = [Param.make "obj" TObj; Param.make "key" TStr; Param.make "val" TAny]
    ; returnType = TObj
    ; description = "Return a copy of `obj` with the `key` set to `val`."
    ; fn =

          (function
          | _, [DObj o; DStr k; v] ->
              DObj (Map.set o (Unicode_string.to_string k) v)
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = ReplacedBy(fn "" "" 0) }
  ; { name = fn "" "dissoc" 0

    ; parameters = [Param.make "obj" TObj; Param.make "key" TStr]
    ; returnType = TObj
    ; description = "Return a copy of `obj` with `key` unset."
    ; fn =

          (function
          | _, [DObj o; DStr k] ->
              DObj (Map.remove o (Unicode_string.to_string k))
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = ReplacedBy(fn "" "" 0) }
  ; { name = fn "" "toForm" 0

    ; parameters = [Param.make "obj" TObj; Param.make "submit" TStr]
    ; returnType = TStr
    ; description =
        "For demonstration only. Returns a HTML form with the labels and types described in `obj`. `submit` is the form's action."
    ; fn =

          (function
          | _, [DObj o; DStr uri] ->
              let fmt =
                format_of_string
                  "<form action=\"%s\" method=\"post\">\n%s\n<input type=\"submit\" value=\"Save\">\n</form>"
              in
              let to_input (k, v) =
                let label =
                  Printf.sprintf "<label for=\"%s\">%s:</label>" k k
                in
                let input =
                  Printf.sprintf
                    "<input id=\"%s\" type=\"text\" name=\"%s\">"
                    k
                    k
                in
                label ^ "\n" ^ input
              in
              let inputs =
                o
                |> Map.to_alist
                |> List.map to_input
                |> String.concat "\n"
              in
              Dval.dstr_of_string_exn
                (Printf.sprintf fmt (Unicode_string.to_string uri) inputs)
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = ReplacedBy(fn "" "" 0) }
  ; { name = fn "Error" "toString" 0

    ; parameters = [Param.make "err" TError]
    ; returnType = TStr
    ; description = "Return a string representing the error"
    ; fn =

          (function
          | _, [DError (_, err)] ->
              Dval.dstr_of_string_exn err
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = ReplacedBy(fn "" "" 0) }
  ; { name = fn "AWS" "urlencode" 0

    ; parameters = [Param.make "str" TStr]
    ; returnType = TStr
    ; description = "Url encode a string per AWS' requirements"
    ; fn =

          (function
          | _, [DStr str] ->
              str
              |> Unicode_string.to_string
              |> Stdlib_util.AWS.url_encode
              |> Dval.dstr_of_string_exn
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = NotDeprecated }
  ; { name = fn "Twitter" "urlencode" 0

    ; parameters = [Param.make "s" TStr]
    ; returnType = TStr
    ; description = "Url encode a string per Twitter's requirements"
    ; fn =

          (function
          | _, [DStr s] ->
              s
              |> Unicode_string.to_string
              |> Uri.pct_encode `Userinfo
              |> Dval.dstr_of_string_exn
          | args ->
              incorrectArgs ())
    ; sqlSpec = NotYetImplementedTODO
      ; previewable = Pure
    ; deprecated = NotDeprecated } ]

module LibBackend.Serialization

open System.Runtime.InteropServices
open System.Threading.Tasks
open FSharp.Control.Tasks
open FSharpPlus
open Npgsql.FSharp
open Ply
open Npgsql
open Db
open Prelude

open LibExecution.Runtime
open LibExecution.Framework

let gid = Shortcuts.gid


let fetchReleventTLIDsForHTTP (host : string)
                              (canvasID : CanvasID)
                              (path : string)
                              (method : string)
                              : Task<List<tlid>> =

  // The pattern `$2 like name` is deliberate, to leverage the DB's
  // pattern matching to solve our routing.
  Sql.query "SELECT tlid
             FROM toplevel_oplists
             WHERE canvas_id = @canvasID
               AND ((module = 'HTTP'
                     AND @path like name
                     AND modifier = @method)
               OR tipe <> 'handler'::toplevel_type)"
  |> Sql.parameters [ "path", Sql.string path
                      "method", Sql.string method
                      "canvasID", Sql.uuid canvasID ]
  |> Sql.executeAsync (fun read -> read.int64 "tlid")

let canvasIDForCanvas (owner : UserID) (canvasName : string) : Task<CanvasID> =
  if canvasName.Length > 64 then
    failwith $"Canvas name was length {canvasName.Length}, must be <= 64"
  else
    // TODO: we create the canvas if it doesn't exist here, seems like a poor choice
    Sql.query "SELECT canvas_id(@newUUID, @owner, @canvasName)"
    |> Sql.parameters [ "newUUID", Sql.uuid (System.Guid.NewGuid())
                        "owner", Sql.uuid owner
                        "canvasName", Sql.string canvasName ]
    |> Sql.executeRowAsync (fun read -> read.uuid "canvas_id")

// split into owner and canvasName
let ownerNameFromHost (host : string) : string =
  match host.Split [| '-' |] |> Seq.toList with
  | owner :: _rest -> owner
  | _ -> host

let userIDForUsername (user : string) : Task<UserID> =
  let owner = String.toLower user
  if List.contains owner Account.bannedUsernames then
    failwith "Banned username"
  else
    Sql.query "SELECT id
                  FROM accounts
                  WHERE accounts.username = @username"
    |> Sql.parameters [ "username", Sql.string user ]
    |> Sql.executeRowAsync (fun read -> read.uuid "id")


let canvasNameFromCustomDomain host : Task<string> =
  Sql.query "SELECT canvas
                  FROM customer_domains
                  WHERE host = @host"
  |> Sql.parameters [ "host", Sql.string host ]
  |> Sql.executeRowAsync (fun read -> read.string "canvas")



let loadCachedToplevels (host : string)
                        (canvasID : CanvasID)
                        (tlids : List<tlid>)
                        : Task<List<byte array>> =

  Sql.query "SELECT data
                FROM toplevel_oplists
                WHERE canvas_id = @canvasID
                AND tlid = ANY(@tlids)"
  |> Sql.parameters [ "canvasID", Sql.uuid canvasID; "tlids", Sql.tlidArray tlids ]
  |> Sql.executeAsync (fun read -> read.bytea "data")


[<DllImport("../_build/default/backend/libbackend/libbackend.a",
            CallingConvention = CallingConvention.Cdecl)>]
extern string camlLibbackend__Serialize__oplist_of_binary_string_4913(byte[] bytes)


let ocamlRenderedToJson (data : byte array) : string =
  camlLibbackend__Serialize__oplist_of_binary_string_4913 data

let parseOCamlOplistJSON (json : string) : Option<Toplevel> = failwith json

let loadHttpHandlersFromCache (host : string)
                              (canvasID : CanvasID)
                              (owner : UserID)
                              (path : string)
                              (method : string)
                              : Task<List<Toplevel>> =
  task {
    let! tlids = fetchReleventTLIDsForHTTP host canvasID path method
    let! binaryTLs = loadCachedToplevels host canvasID tlids

    let jsonTLs = List.map ocamlRenderedToJson binaryTLs

    let tls = List.choose parseOCamlOplistJSON jsonTLs

    return tls
  }




// FSTODO: if we need speed, switch to transferring using protobufs instead of JSON
// where do we get the protobuf files?
// - 1) generate using ocaml types (maybe deprecated? ocaml-protoc)
// - 2) generate using FSharp types (possible?)
// - 3) hand-write them (converters on both ends)

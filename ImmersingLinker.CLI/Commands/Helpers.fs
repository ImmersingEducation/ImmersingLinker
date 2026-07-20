module ImmersingLinker.CLI.Commands.Helpers

open System.CommandLine
open System.Text.Json

let defaultPort = "5157"

let portOption =
    let opt = Option<string>("--port")
    opt.Aliases.Add("-p")
    opt.Description <- "Server port (default: 5157)"
    opt

let jsonOption =
    let opt = Option<bool>("--json")
    opt.Aliases.Add("-j")
    opt.Description <- "Output result as JSON"
    opt

let getPort (parseResult: ParseResult) : string =
    let value = parseResult.GetValue(portOption)
    if System.String.IsNullOrEmpty(value) then defaultPort else value

let getJson (parseResult: ParseResult) : bool =
    parseResult.GetValue(jsonOption)

let serializeJson (obj: obj) =
    JsonSerializer.Serialize(obj, JsonSerializerOptions(WriteIndented = true))

let printOutput (json: bool) (obj: obj) =
    if json then
        printfn "%s" (serializeJson obj)
    else
        printfn "%A" obj

let printErrorOutput (json: bool) (message: string) =
    if json then
        let error = {| success = false; error = message |}
        printfn "%s" (serializeJson error)
    else
        eprintfn "Error: %s" message

let printSuccess (json: bool) (message: string) =
    if json then
        let result = {| success = true; message = message |}
        printfn "%s" (serializeJson result)
    else
        printfn "%s" message

let printNotFound (json: bool) (resource: string) (identifier: string) =
    printErrorOutput json (resource + " '" + identifier + "' not found.")

let addGlobalOptions (cmd: Command) =
    cmd.Options.Add(portOption)
    cmd.Options.Add(jsonOption)

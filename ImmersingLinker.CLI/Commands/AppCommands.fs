module ImmersingLinker.CLI.Commands.AppCommands

open System.CommandLine
open System.Diagnostics
open System.Threading
open ImmersingLinker.SDK
open ImmersingLinker.CLI.Commands.Helpers

let private testConnectionCommand =
    let cmd = Command("test-connection", "Test connection with Server")
    cmd.Aliases.Add("tc")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = AppServiceClient(port)
            let! connected = client.TestConnection() |> Async.AwaitTask
            if connected then
                printSuccess json $"Server is running on port %s{port}."
                return 0
            else
                printErrorOutput json $"Connection failed. Check Server state or run 'ilinker-cli launch'."
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private statusCommand =
    let cmd = Command("status", "Show Server status information")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = AppServiceClient(port)
            let! connected = client.TestConnection() |> Async.AwaitTask
            if connected then
                let result = {|
                    success = true
                    port = port
                    status = "running"
                |}
                printOutput json result
                return 0
            else
                printErrorOutput json $"Server is not responding on port %s{port}."
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private launchCommand =
    let cmd = Command("launch", "Start the ImmersingLinker Server process")
    cmd.SetAction(fun parseResult ->
        async {
            let json = getJson parseResult
            try
                let psi = ProcessStartInfo(
                    FileName = "dotnet",
                    Arguments = "run --project ImmersingLinker.Server",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                )
                let proc = Process.Start(psi)
                if proc = null then
                    printErrorOutput json "Failed to start Server process."
                    return 1
                else
                    printSuccess json $"Server process started (PID: %d{proc.Id})."
                    return 0
            with
            | ex ->
                printErrorOutput json $"Failed to launch Server: %s{ex.Message}"
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let appCommand =
    let cmd = Command("app", "Application service commands")
    addGlobalOptions cmd
    cmd.Subcommands.Add(testConnectionCommand)
    cmd.Subcommands.Add(statusCommand)
    cmd.Subcommands.Add(launchCommand)
    cmd

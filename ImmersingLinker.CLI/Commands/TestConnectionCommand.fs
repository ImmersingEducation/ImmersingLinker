module ImmersingLinker.CLI.Commands.TestConnectionCommand

open System.CommandLine
open ImmersingLinker.SDK
open ImmersingLinker.CLI.Commands.Helpers

let testConnectionCommand =
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
                printErrorOutput json $"Connection failed. Check Server state or run 'ilinker-cli app launch'."
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

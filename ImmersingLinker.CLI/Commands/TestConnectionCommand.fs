module ImmersingLinker.CLI.Commands.TestConnectionCommand

open System.CommandLine
open ImmersingLinker.SDK

let testConnectionCommand =
    let cmd = Command("tc", "Test connection")

    cmd.SetAction(fun parseResult ->
        let client = AppServiceClient "5157"
        let connected = client.TestConnection().Result

        if connected then
            printfn "ImmersingLinker Server is running normally on port 5157."
            0
        else
            printfn "Connection test failed. Please check the Server's running state or use `ilinker-cli launch`."
            1)

    cmd

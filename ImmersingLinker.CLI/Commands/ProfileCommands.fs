module ImmersingLinker.CLI.Commands.ProfileCommands

open System.CommandLine
open ImmersingLinker.SDK
open ImmersingLinker.CLI.Commands.Helpers

let private showCommand =
    let cmd = Command("show", "Show current profile information")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            try
                let! path = client.GetCurrentProfilePathAsync() |> Async.AwaitTask
                let! trusted = client.GetIsCurrentProfileTrustedAsync() |> Async.AwaitTask
                let result = {|
                    path = path
                    trusted = trusted
                |}
                printOutput json result
                return 0
            with
            | ex ->
                printErrorOutput json $"Failed to get profile: %s{ex.Message}"
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let profileCommand =
    let cmd = Command("profile", "Profile information commands")
    addGlobalOptions cmd
    cmd.Subcommands.Add(showCommand)
    cmd

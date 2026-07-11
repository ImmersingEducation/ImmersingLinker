namespace ImmersingLinker.CLI

open System.CommandLine
open ImmersingLinker.CLI.Commands

module Program =
    [<EntryPoint>]
    let main args =
        let rootCmd = RootCommand "ImmersingLinker CLI"

        rootCmd.Subcommands.Add TestConnectionCommand.testConnectionCommand

        let parserResult = rootCmd.Parse args
        parserResult.Invoke()

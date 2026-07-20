namespace ImmersingLinker.CLI

open System
open System.CommandLine
open System.CommandLine.Parsing
open ImmersingLinker.CLI.Commands

module Program =
    [<EntryPoint>]
    let main args =
        let rootCmd = RootCommand("ImmersingLinker CLI - Class information integration platform")
        rootCmd.Description <- "Class information integration platform for classroom teaching"

        rootCmd.Subcommands.Add(TestConnectionCommand.testConnectionCommand)
        rootCmd.Subcommands.Add(AppCommands.appCommand)
        rootCmd.Subcommands.Add(LessonCommands.lessonCommand)
        rootCmd.Subcommands.Add(ClassCommands.classCommand)
        rootCmd.Subcommands.Add(AutomationCommands.automationCommand)
        rootCmd.Subcommands.Add(ProfileCommands.profileCommand)

        let result = rootCmd.Parse(args)
        result.Invoke()

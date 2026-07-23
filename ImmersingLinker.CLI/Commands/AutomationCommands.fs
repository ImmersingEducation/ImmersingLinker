module ImmersingLinker.CLI.Commands.AutomationCommands

open System
open System.CommandLine
open System.Text.Json
open ImmersingLinker.Core.Models.Automation
open ImmersingLinker.SDK
open ImmersingLinker.SDK.JsonConverters
open ImmersingLinker.CLI.Commands.Helpers

let private emptyTriggerDto = TriggerDto("", Nullable<JsonElement>())
let private emptyActions = System.Collections.Generic.List<ActionDto>()

let private listCommand =
    let cmd = Command("list", "List all automation plans")
    cmd.Aliases.Add("ls")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = AutomationServiceClient(port)
            let! plans = client.GetAllPlanInfosAsync() |> Async.AwaitTask
            if plans.Count = 0 then
                printSuccess json "No automation plans found."
                return 0
            else
                let result = plans |> Seq.map (fun p ->
                    {| guid = string p.Guid; name = p.Name |}) |> Seq.toList
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private triggerCommand =
    let cmd = Command("trigger", "Manually trigger an automation plan")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Plan GUID"
    cmd.Arguments.Add(guidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let client = AutomationServiceClient(port)
            try
                do! client.TriggerPlanAsync(guid) |> Async.AwaitTask
                printSuccess json ("Plan '" + guid + "' triggered.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private invokeCommand =
    let cmd = Command("invoke", "Invoke a URL trigger by tag")
    let tagArg = Argument<string>("tag")
    tagArg.Description <- "Trigger tag"
    cmd.Arguments.Add(tagArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let tag = parseResult.GetValue(tagArg)
            let client = AutomationServiceClient(port)
            try
                do! client.InvokeUrlTriggerAsync(tag) |> Async.AwaitTask
                printSuccess json ("URL trigger '" + tag + "' invoked.")
                return 0
            with
            | ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private createCommand =
    let cmd = Command("create", "Create an automation plan from JSON")
    let jsonArg = Argument<string>("json")
    jsonArg.Description <- "Plan JSON string"
    cmd.Arguments.Add(jsonArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let jsonInput = parseResult.GetValue(jsonArg)
            let client = AutomationServiceClient(port)
            try
                let request = JsonSerializer.Deserialize<CreateAutomationPlanRequest>(jsonInput, AutomationJsonOptions.Default)
                let! plan = client.CreatePlanAsync(request) |> Async.AwaitTask
                let result = {| success = true; guid = string plan.Guid; name = plan.Name |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
            | :? JsonException as ex ->
                printErrorOutput json ("Invalid JSON: " + ex.Message)
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private updateCommand =
    let cmd = Command("update", "Update an automation plan from JSON")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Plan GUID"
    let jsonArg = Argument<string>("json")
    jsonArg.Description <- "Updated plan JSON string"
    cmd.Arguments.Add(guidArg)
    cmd.Arguments.Add(jsonArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let jsonInput = parseResult.GetValue(jsonArg)
            let client = AutomationServiceClient(port)
            try
                let request = JsonSerializer.Deserialize<UpdateAutomationPlanRequest>(jsonInput, AutomationJsonOptions.Default)
                let! plan = client.UpdatePlanAsync(guid, request) |> Async.AwaitTask
                let result = {| success = true; guid = string plan.Guid; name = plan.Name |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
            | :? JsonException as ex ->
                printErrorOutput json ("Invalid JSON: " + ex.Message)
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private deleteCommand =
    let cmd = Command("delete", "Delete an automation plan")
    cmd.Aliases.Add("rm")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Plan GUID"
    cmd.Arguments.Add(guidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let client = AutomationServiceClient(port)
            try
                do! client.DeletePlanAsync(guid) |> Async.AwaitTask
                printSuccess json ("Plan '" + guid + "' deleted.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let automationCommand =
    let cmd = Command("automation", "Automation plan management commands")
    cmd.Aliases.Add("auto")
    addGlobalOptions cmd
    cmd.Subcommands.Add(listCommand)
    cmd.Subcommands.Add(triggerCommand)
    cmd.Subcommands.Add(invokeCommand)
    cmd.Subcommands.Add(createCommand)
    cmd.Subcommands.Add(updateCommand)
    cmd.Subcommands.Add(deleteCommand)
    cmd

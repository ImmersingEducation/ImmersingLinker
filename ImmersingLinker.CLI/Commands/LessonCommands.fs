module ImmersingLinker.CLI.Commands.LessonCommands

open System.CommandLine
open ClassIsland.Shared.Enums
open ClassIsland.Shared.Models.Profile
open ImmersingLinker.SDK
open ImmersingLinker.CLI.Commands.Helpers

let private subjectName (s: Subject) =
    if System.String.IsNullOrEmpty(s.Name) then s.Initial else s.Name

let private timeStateName = function
    | TimeState.None -> "None"
    | TimeState.OnClass -> "OnClass"
    | TimeState.PrepareOnClass -> "PrepareOnClass"
    | TimeState.Breaking -> "Breaking"
    | TimeState.AfterSchool -> "AfterSchool"
    | other -> string other

let private timeLayoutItemDisplay (item: TimeLayoutItem) =
    sprintf "%s [%s - %s]" (item.BreakNameText) (string item.StartTime) (string item.EndTime)

let private formatTimeSpan (ts: System.TimeSpan) =
    sprintf "%02d:%02d:%02d" ts.Hours ts.Minutes ts.Seconds

let private currentSubjectCommand =
    let cmd = Command("current", "Show current lesson subject")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! subject = client.GetCurrentSubjectAsync() |> Async.AwaitTask
            match subject with
            | null -> printNotFound json "Lesson" "current"; return 1
            | s ->
                let result = {| subject = subjectName s; initial = s.Initial; teacher = s.TeacherName |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private nextSubjectCommand =
    let cmd = Command("next", "Show next lesson subject")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! subject = client.GetNextClassSubjectAsync() |> Async.AwaitTask
            let result = {| subject = subjectName subject; initial = subject.Initial; teacher = subject.TeacherName |}
            printOutput json result
            return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private previousSubjectCommand =
    let cmd = Command("previous", "Show previous lesson subject")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! subject = client.GetPreviousClassSubjectAsync() |> Async.AwaitTask
            match subject with
            | null -> printNotFound json "Lesson" "previous"; return 1
            | s ->
                let result = {| subject = subjectName s; initial = s.Initial; teacher = s.TeacherName |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private timerCommand =
    let cmd = Command("timer", "Show current lesson timer information")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! classLeft = client.GetOnClassLeftTimeAsync() |> Async.AwaitTask
            let! breakingLeft = client.GetOnBreakingLeftTimeAsync() |> Async.AwaitTask
            let! elapsedAny = client.GetElapsedSincePreviousAnyAsync() |> Async.AwaitTask
            let result = {|
                onClassLeft = formatTimeSpan classLeft
                onBreakingLeft = formatTimeSpan breakingLeft
                elapsedSincePreviousAny = formatTimeSpan elapsedAny
            |}
            printOutput json result
            return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private planCommand =
    let cmd = Command("plan", "Show current class plan")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! plan = client.GetCurrentClassPlanAsync() |> Async.AwaitTask
            match plan with
            | null -> printNotFound json "ClassPlan" "current"; return 1
            | p ->
                let layouts =
                    match p.TimeLayout with
                    | null -> []
                    | tl ->
                        tl.Layouts
                        |> Seq.map (fun t ->
                            {| display = timeLayoutItemDisplay t
                               startTime = string t.StartTime
                               endTime = string t.EndTime
                               duration = string t.Last
                               timeType = t.TimeType |})
                        |> Seq.toList
                let result = {|
                    name = p.Name
                    timeLayout = layouts
                |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private stateCommand =
    let cmd = Command("state", "Show current time state")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = LessonServiceClient(port)
            let! state = client.GetCurrentStateAsync() |> Async.AwaitTask
            let result = {| state = timeStateName state |}
            printOutput json result
            return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let lessonCommand =
    let cmd = Command("lesson", "Lesson information commands")
    addGlobalOptions cmd
    cmd.Subcommands.Add(currentSubjectCommand)
    cmd.Subcommands.Add(nextSubjectCommand)
    cmd.Subcommands.Add(previousSubjectCommand)
    cmd.Subcommands.Add(timerCommand)
    cmd.Subcommands.Add(planCommand)
    cmd.Subcommands.Add(stateCommand)
    cmd

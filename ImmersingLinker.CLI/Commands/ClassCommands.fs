module ImmersingLinker.CLI.Commands.ClassCommands

open System.CommandLine
open ImmersingLinker.Core.Models.Class
open ImmersingLinker.SDK
open ImmersingLinker.CLI.Commands.Helpers

let private genderName = function
    | Gender.Male -> "Male"
    | Gender.Female -> "Female"
    | other -> string other

let private parseGender (s: string) =
    match s.ToLowerInvariant() with
    | "male" | "m" -> Gender.Male
    | "female" | "f" -> Gender.Female
    | _ -> failwithf "Invalid gender: %s. Use Male or Female." s

let private classGuidArg =
    let arg = Argument<string>("class-guid")
    arg.Description <- "Class GUID"
    arg

let private studentIdArg =
    let arg = Argument<int>("student-id")
    arg.Description <- "Student ID in class"
    arg

let private appIdArg =
    let arg = Argument<string>("app-id")
    arg.Description <- "Application ID"
    arg

let private propNameArg =
    let arg = Argument<string>("name")
    arg.Description <- "Property name"
    arg

let private propValueArg =
    let arg = Argument<string>("value")
    arg.Description <- "Property value (JSON)"
    arg

// ── class extra ──

let private classExtraListCommand =
    let cmd = Command("list", "List extra properties of a class")
    cmd.Aliases.Add("ls")
    let appIdOpt = Option<string>("--app-id")
    appIdOpt.Description <- "Filter by application ID"
    cmd.Options.Add(appIdOpt)
    cmd.Arguments.Add(classGuidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let appId = parseResult.GetValue(appIdOpt)
            let client = ClassServiceClient(port)
            let! props =
                if System.String.IsNullOrEmpty(appId) then
                    client.GetExtraPropertiesByClassGuidAsync(classGuid) |> Async.AwaitTask
                else
                    client.GetExtraPropertiesByAppIdInClassAsync(classGuid, appId) |> Async.AwaitTask
            if props.Count = 0 then
                printSuccess json "No extra properties found."
                return 0
            else
                let result = props |> Seq.map (fun p ->
                    {| app = p.Application.UniqueId; name = p.Name; value = p.Value |}) |> Seq.toList
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private classExtraGetCommand =
    let cmd = Command("get", "Get a single extra property from a class")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let client = ClassServiceClient(port)
            let! prop = client.GetExtraPropertyByAppIdAndNameInClassAsync(classGuid, appId, name) |> Async.AwaitTask
            match prop with
            | null ->
                printNotFound json "ExtraProperty" (appId + "/" + name)
                return 1
            | p ->
                let result = {| app = p.Application.UniqueId; name = p.Name; value = p.Value |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private classExtraAddCommand =
    let cmd = Command("add", "Add an extra property to a class")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.Arguments.Add(propValueArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let valueRaw = parseResult.GetValue(propValueArg)
            let value = if System.String.IsNullOrEmpty(valueRaw) then null else box valueRaw
            let client = ClassServiceClient(port)
            try
                let! prop = client.AddClassExtraPropertyAsync(classGuid, CreateExtraPropertyRequest(appId, name, value)) |> Async.AwaitTask
                let result = {| success = true; app = prop.Application.UniqueId; name = prop.Name; value = prop.Value |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private classExtraUpdateCommand =
    let cmd = Command("update", "Update an extra property of a class")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.Arguments.Add(propValueArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let valueRaw = parseResult.GetValue(propValueArg)
            let value = if System.String.IsNullOrEmpty(valueRaw) then null else box valueRaw
            let client = ClassServiceClient(port)
            try
                let! prop = client.UpdateClassExtraPropertyAsync(classGuid, appId, name, UpdateExtraPropertyRequest(value)) |> Async.AwaitTask
                let result = {| success = true; app = prop.Application.UniqueId; name = prop.Name; value = prop.Value |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private classExtraRemoveCommand =
    let cmd = Command("remove", "Remove an extra property from a class")
    cmd.Aliases.Add("rm")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let client = ClassServiceClient(port)
            try
                do! client.DeleteClassExtraPropertyAsync(classGuid, appId, name) |> Async.AwaitTask
                printSuccess json ("Extra property '" + appId + "/" + name + "' deleted from class.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private classExtraCommand =
    let cmd = Command("extra", "Class extra property management")
    cmd.Aliases.Add("ex")
    cmd.Subcommands.Add(classExtraListCommand)
    cmd.Subcommands.Add(classExtraGetCommand)
    cmd.Subcommands.Add(classExtraAddCommand)
    cmd.Subcommands.Add(classExtraUpdateCommand)
    cmd.Subcommands.Add(classExtraRemoveCommand)
    cmd

// ── student ──

let private studentListCommand =
    let cmd = Command("list", "List students in a class")
    cmd.Aliases.Add("ls")
    cmd.Arguments.Add(classGuidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let client = ClassServiceClient(port)
            let! students = client.GetStudentsByClassGuidAsync(classGuid) |> Async.AwaitTask
            if students.Count = 0 then
                printSuccess json ("No students in class '" + classGuid + "'.")
                return 0
            else
                let result = students |> Seq.map (fun s ->
                    {| studentId = s.StudentIdInClass
                       name = s.Name
                       gender = genderName s.Gender |}) |> Seq.toList
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentAddCommand =
    let cmd = Command("add", "Add a student to a class")
    let nameArg = Argument<string>("name")
    nameArg.Description <- "Student name"
    let genderArg = Argument<string>("gender")
    genderArg.Description <- "Gender (Male/Female)"
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(nameArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(genderArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let name = parseResult.GetValue(nameArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let gender = parseGender (parseResult.GetValue(genderArg))
            let client = ClassServiceClient(port)
            try
                let! student = client.AddStudentAsync(classGuid, CreateStudentRequest(name, studentId, gender)) |> Async.AwaitTask
                let result = {|
                    success = true
                    studentId = student.StudentIdInClass
                    name = student.Name
                    gender = genderName student.Gender
                |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentRemoveCommand =
    let cmd = Command("remove", "Remove a student from a class")
    cmd.Aliases.Add("rm")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let client = ClassServiceClient(port)
            try
                do! client.DeleteStudentAsync(classGuid, studentId) |> Async.AwaitTask
                printSuccess json ("Student " + string studentId + " removed from class '" + classGuid + "'.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentUpdateCommand =
    let cmd = Command("update", "Update student information")
    let nameArg = Argument<string>("name")
    nameArg.Description <- "New student name"
    let genderArg = Argument<string>("gender")
    genderArg.Description <- "New gender (Male/Female)"
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(nameArg)
    cmd.Arguments.Add(genderArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let name = parseResult.GetValue(nameArg)
            let gender = parseGender (parseResult.GetValue(genderArg))
            let client = ClassServiceClient(port)
            try
                let! student = client.UpdateStudentAsync(classGuid, studentId, UpdateStudentRequest(name, gender, "")) |> Async.AwaitTask
                let result = {|
                    success = true
                    studentId = student.StudentIdInClass
                    name = student.Name
                    gender = genderName student.Gender
                |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

// ── student extra ──

let private studentExtraListCommand =
    let cmd = Command("list", "List extra properties of a student")
    cmd.Aliases.Add("ls")
    let appIdOpt = Option<string>("--app-id")
    appIdOpt.Description <- "Filter by application ID"
    cmd.Options.Add(appIdOpt)
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let appId = parseResult.GetValue(appIdOpt)
            let client = ClassServiceClient(port)
            let! props =
                if System.String.IsNullOrEmpty(appId) then
                    client.GetExtraPropertiesByStudentIdInClassAsync(classGuid, studentId) |> Async.AwaitTask
                else
                    client.GetExtraPropertiesByStudentIdAndAppIdInClassAsync(classGuid, studentId, appId) |> Async.AwaitTask
            if props.Count = 0 then
                printSuccess json "No extra properties found."
                return 0
            else
                let result = props |> Seq.map (fun p ->
                    {| app = p.Application.UniqueId; name = p.Name; value = p.Value |}) |> Seq.toList
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentExtraGetCommand =
    let cmd = Command("get", "Get a single extra property from a student")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let client = ClassServiceClient(port)
            let! prop = client.GetExtraPropertyByNameAndStudentIdInClassAsync(classGuid, studentId, appId, name) |> Async.AwaitTask
            match prop with
            | null ->
                printNotFound json "ExtraProperty" (appId + "/" + name)
                return 1
            | p ->
                let result = {| app = p.Application.UniqueId; name = p.Name; value = p.Value |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentExtraAddCommand =
    let cmd = Command("add", "Add an extra property to a student")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.Arguments.Add(propValueArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let valueRaw = parseResult.GetValue(propValueArg)
            let value = if System.String.IsNullOrEmpty(valueRaw) then null else box valueRaw
            let client = ClassServiceClient(port)
            try
                let! prop = client.AddStudentExtraPropertyAsync(classGuid, studentId, CreateExtraPropertyRequest(appId, name, value)) |> Async.AwaitTask
                let result = {| success = true; app = prop.Application.UniqueId; name = prop.Name; value = prop.Value |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentExtraUpdateCommand =
    let cmd = Command("update", "Update an extra property of a student")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.Arguments.Add(propValueArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let valueRaw = parseResult.GetValue(propValueArg)
            let value = if System.String.IsNullOrEmpty(valueRaw) then null else box valueRaw
            let client = ClassServiceClient(port)
            try
                let! prop = client.UpdateStudentExtraPropertyAsync(classGuid, studentId, appId, name, UpdateExtraPropertyRequest(value)) |> Async.AwaitTask
                let result = {| success = true; app = prop.Application.UniqueId; name = prop.Name; value = prop.Value |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentExtraRemoveCommand =
    let cmd = Command("remove", "Remove an extra property from a student")
    cmd.Aliases.Add("rm")
    cmd.Arguments.Add(classGuidArg)
    cmd.Arguments.Add(studentIdArg)
    cmd.Arguments.Add(appIdArg)
    cmd.Arguments.Add(propNameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let classGuid = parseResult.GetValue(classGuidArg)
            let studentId = parseResult.GetValue(studentIdArg)
            let appId = parseResult.GetValue(appIdArg)
            let name = parseResult.GetValue(propNameArg)
            let client = ClassServiceClient(port)
            try
                do! client.DeleteStudentExtraPropertyAsync(classGuid, studentId, appId, name) |> Async.AwaitTask
                printSuccess json ("Extra property '" + appId + "/" + name + "' deleted from student.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private studentExtraCommand =
    let cmd = Command("extra", "Student extra property management")
    cmd.Aliases.Add("ex")
    cmd.Subcommands.Add(studentExtraListCommand)
    cmd.Subcommands.Add(studentExtraGetCommand)
    cmd.Subcommands.Add(studentExtraAddCommand)
    cmd.Subcommands.Add(studentExtraUpdateCommand)
    cmd.Subcommands.Add(studentExtraRemoveCommand)
    cmd

let private studentCommand =
    let cmd = Command("student", "Student management within a class")
    cmd.Aliases.Add("stu")
    cmd.Subcommands.Add(studentListCommand)
    cmd.Subcommands.Add(studentAddCommand)
    cmd.Subcommands.Add(studentRemoveCommand)
    cmd.Subcommands.Add(studentUpdateCommand)
    cmd.Subcommands.Add(studentExtraCommand)
    cmd

// ── class ──

let private listCommand =
    let cmd = Command("list", "List all classes")
    cmd.Aliases.Add("ls")
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let client = ClassServiceClient(port)
            let! classes = client.GetAllClassInfosAsync() |> Async.AwaitTask
            if classes.Count = 0 then
                printSuccess json "No classes found."
                return 0
            else
                let result = classes |> Seq.map (fun c ->
                    {| guid = string c.Guid; name = c.Name |}) |> Seq.toList
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private infoCommand =
    let cmd = Command("info", "Show class details")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Class GUID"
    cmd.Arguments.Add(guidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let client = ClassServiceClient(port)
            let! cls = client.GetClassByGuidAsync(guid) |> Async.AwaitTask
            match cls with
            | null -> printNotFound json "Class" guid; return 1
            | c ->
                let students = c.Students |> Seq.map (fun s ->
                    {| studentId = s.StudentIdInClass
                       name = s.Name
                       gender = genderName s.Gender |}) |> Seq.toList
                let props = c.ExtraProperties |> Seq.map (fun p ->
                    {| app = p.Application.UniqueId; name = p.Name; value = p.Value |}) |> Seq.toList
                let result = {|
                    guid = string c.Guid
                    name = c.Name
                    studentCount = c.Students.Count
                    students = students
                    extraProperties = props
                |}
                printOutput json result
                return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private createCommand =
    let cmd = Command("create", "Create a new class")
    let nameArg = Argument<string>("name")
    nameArg.Description <- "Class name"
    cmd.Arguments.Add(nameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let name = parseResult.GetValue(nameArg)
            let client = ClassServiceClient(port)
            let! cls = client.CreateClassAsync(CreateClassRequest(name)) |> Async.AwaitTask
            let result = {| success = true; guid = string cls.Guid; name = cls.Name |}
            printOutput json result
            return 0
        } |> Async.StartAsTask |> _.Result)
    cmd

let private updateCommand =
    let cmd = Command("update", "Update a class name")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Class GUID"
    let nameArg = Argument<string>("name")
    nameArg.Description <- "New class name"
    cmd.Arguments.Add(guidArg)
    cmd.Arguments.Add(nameArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let name = parseResult.GetValue(nameArg)
            let client = ClassServiceClient(port)
            try
                let! cls = client.UpdateClassAsync(guid, UpdateClassRequest(name)) |> Async.AwaitTask
                let result = {| success = true; guid = string cls.Guid; name = cls.Name |}
                printOutput json result
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let private deleteCommand =
    let cmd = Command("delete", "Delete a class")
    cmd.Aliases.Add("rm")
    let guidArg = Argument<string>("guid")
    guidArg.Description <- "Class GUID"
    cmd.Arguments.Add(guidArg)
    cmd.SetAction(fun parseResult ->
        async {
            let port = getPort parseResult
            let json = getJson parseResult
            let guid = parseResult.GetValue(guidArg)
            let client = ClassServiceClient(port)
            try
                do! client.DeleteClassAsync(guid) |> Async.AwaitTask
                printSuccess json ("Class '" + guid + "' deleted.")
                return 0
            with
            | :? System.InvalidOperationException as ex ->
                printErrorOutput json ex.Message
                return 1
        } |> Async.StartAsTask |> _.Result)
    cmd

let classCommand =
    let cmd = Command("class", "Class and student management commands")
    cmd.Aliases.Add("cls")
    addGlobalOptions cmd
    cmd.Subcommands.Add(listCommand)
    cmd.Subcommands.Add(infoCommand)
    cmd.Subcommands.Add(createCommand)
    cmd.Subcommands.Add(updateCommand)
    cmd.Subcommands.Add(deleteCommand)
    cmd.Subcommands.Add(classExtraCommand)
    cmd.Subcommands.Add(studentCommand)
    cmd

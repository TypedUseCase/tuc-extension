// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

open System
open System.IO
open Fake.Core
open Fake.JavaScript
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open Fake.Core.TargetOperators
open Fake.Tools.Git
open Fake.Api

type ToolDir =
    /// Global tool dir must be in PATH - ${PATH}:/root/.dotnet/tools
    | Global
    /// Just a dir name, the location will be used as: ./{LocalDirName}
    | Local of string

// ========================================================================================================
// === F# / VS Code Extension fake build ========================================================== 1.0.0 =
// --------------------------------------------------------------------------------------------------------
// Options:
//  - no-lint    - lint will be executed, but the result is not validated
// --------------------------------------------------------------------------------------------------------
// Table of contents:
//      1. Information about project, configuration
//      2. Utilities, DotnetCore functions
//      3. FAKE targets
//      4. FAKE targets hierarchy
// ========================================================================================================

// --------------------------------------------------------------------------------------------------------
// 1. Information about the project to be used at NuGet and in AssemblyInfo files and other FAKE configuration
// --------------------------------------------------------------------------------------------------------

// Git configuration (used for publishing documentation in gh-pages branch)
// The profile where the project is posted
let gitOwner = "TypedUseCase"
let gitHome = "https://github.com/" + gitOwner

// The name of the project on GitHub
let gitName = "tuc-extension"

// let languageServerDir = "../language-server"    // local dir for developing
let languageServerDir = "paket-files/github.com/TypedUseCase/language-server"
let archiveDir = "dist"

// Read additional information from the release notes document
let release = ReleaseNotes.parse (System.IO.File.ReadAllLines "CHANGELOG.md" |> Seq.filter ((<>) "## Unreleased"))

let toolsDir = Global

// --------------------------------------------------------------------------------------------------------
// 2. Utilities, DotnetCore functions, etc.
// --------------------------------------------------------------------------------------------------------

[<AutoOpen>]
module private Utils =
    let tee f a =
        f a
        a

    let skipOn option action p =
        if p.Context.Arguments |> Seq.contains option
        then Trace.tracefn "Skipped ..."
        else action p

module private DotnetCore =
    let run cmd workingDir =
        let options =
            DotNet.Options.withWorkingDirectory workingDir
            >> DotNet.Options.withRedirectOutput true

        DotNet.exec options cmd ""

    let runOrFail cmd workingDir =
        run cmd workingDir
        |> tee (fun result ->
            if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir
        )
        |> ignore

    let runInRoot cmd = run cmd "."
    let runInRootOrFail cmd = runOrFail cmd "."

    let installOrUpdateTool toolDir tool =
        let toolCommand action =
            match toolDir with
            | Global -> sprintf "tool %s --global %s" action tool
            | Local dir -> sprintf "tool %s --tool-path ./%s %s" action dir tool

        match runInRoot (toolCommand "install") with
        | { ExitCode = code } when code <> 0 ->
            match runInRoot (toolCommand "update") with
            | { ExitCode = code } when code <> 0 -> Trace.tracefn "Warning: Install and update of %A has failed." tool
            | _ -> ()
        | _ -> ()

    let execute command args (dir: string) =
        let cmd =
            sprintf "%s/%s"
                (dir.TrimEnd('/'))
                command

        let processInfo = System.Diagnostics.ProcessStartInfo(cmd)
        processInfo.RedirectStandardOutput <- true
        processInfo.RedirectStandardError <- true
        processInfo.UseShellExecute <- false
        processInfo.CreateNoWindow <- true
        processInfo.Arguments <- args |> String.concat " "

        use proc =
            new System.Diagnostics.Process(
                StartInfo = processInfo
            )
        if proc.Start() |> not then failwith "Process was not started."
        proc.WaitForExit()

        if proc.ExitCode <> 0 then failwithf "Command '%s' failed in %s." command dir
        (proc.StandardOutput.ReadToEnd(), proc.StandardError.ReadToEnd())

let stringToOption = function
    | null | "" -> None
    | string -> Some string

let run cmd args dir =
    let parms = { ExecParams.Empty with Program = cmd; WorkingDir = dir; CommandLine = args }
    if Process.shellExec parms <> 0 then
        failwithf "Error while running '%s' with args: %s" cmd args


let platformTool tool path =
    match Environment.isUnix with
    | true -> tool
    | _ ->  match ProcessUtils.tryFindFileOnPath path with
            | None -> failwithf "can't find tool %s on PATH" tool
            | Some v -> v

let npmTool =
    platformTool "npm"  "npm.cmd"

let vsceTool = lazy (platformTool "vsce" "vsce.cmd")

let runFable additionalArgs =
    let cmd = "webpack " + additionalArgs
    Yarn.exec cmd id

let copyLanguageServer releaseBin languageServerBin =
    Directory.ensure releaseBin
    Shell.cleanDir releaseBin
    Shell.copyDir releaseBin languageServerBin (fun _ -> true)

let copyGrammar grammarDir grammarRelease =
    Directory.ensure grammarRelease
    Shell.cleanDir grammarRelease
    Shell.copyFiles grammarRelease [
        grammarDir </> "tuc.tmLanguage.json"
    ]

let copySchemas fsschemaDir fsschemaRelease =
    Directory.ensure fsschemaRelease
    Shell.cleanDir fsschemaRelease
    Shell.copyFiles fsschemaRelease [
        fsschemaDir </> "fableconfig.json"
        fsschemaDir </> "wsconfig.json"
    ]

(* let copyLib libDir releaseDir =
    Directory.ensure releaseDir
    Shell.copyDir (releaseDir </> "x64") (libDir </> "x64") (fun _ -> true)
    Shell.copyDir (releaseDir </> "x86") (libDir </> "x86") (fun _ -> true)
    Shell.copyFiles releaseDir [
        libDir </> "libe_sqlite3.so"
        libDir </> "libe_sqlite3.dylib"
    ] *)

let buildPackage dir =
    Process.killAllByName "vsce"
    run vsceTool.Value "package" dir
    !! (sprintf "%s/*.vsix" dir)
    |> Seq.iter(Shell.moveFile "./temp/")

let setPackageJsonField name value releaseDir =
    let fileName = sprintf "./%s/package.json" releaseDir
    let lines =
        File.ReadAllLines fileName
        |> Seq.map (fun line ->
            if line.TrimStart().StartsWith(sprintf "\"%s\":" name) then
                let indent = line.Substring(0,line.IndexOf("\""))
                sprintf "%s\"%s\": %s," indent name value
            else line)
    File.WriteAllLines(fileName,lines)

let setVersion (release: ReleaseNotes.ReleaseNotes) releaseDir =
    let versionString = sprintf "\"%O\"" release.NugetVersion
    setPackageJsonField "version" versionString releaseDir

let publishToGallery releaseDir =
    let token =
        match Environment.environVarOrDefault "vsce-token" "" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "VSCE Token: "

    Process.killAllByName "vsce"
    run vsceTool.Value (sprintf "publish --pat %s" token) releaseDir

let ensureGitUser user email =
    match Fake.Tools.Git.CommandHelper.runGitCommand "." "config user.name" with
    | true, [username], _ when username = user -> ()
    | _, _, _ ->
        Fake.Tools.Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "config user.name %s" user)
        Fake.Tools.Git.CommandHelper.directRunGitCommandAndFail "." (sprintf "config user.email %s" email)

let releaseGithub (release: ReleaseNotes.ReleaseNotes) =
    let user =
        match Environment.environVarOrDefault "github-user" "" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserInput "Username: "
    let email =
        match Environment.environVarOrDefault "user-email" "" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserInput "Email: "
    let pw =
        match Environment.environVarOrDefault "github-pw" "" with
        | s when not (String.IsNullOrWhiteSpace s) -> s
        | _ -> UserInput.getUserPassword "Password: "
    let remote =
        CommandHelper.getGitResult "" "remote -v"
        |> Seq.filter (fun (s: string) -> s.EndsWith("(push)"))
        |> Seq.tryFind (fun (s: string) -> s.Contains(gitOwner + "/" + gitName))
        |> function None -> gitHome + "/" + gitName | Some (s: string) -> s.Split().[0]

    Staging.stageAll ""
    ensureGitUser user email
    Commit.exec "." (sprintf "Version %s" release.NugetVersion)
    Branches.pushBranch "" remote "master"
    Branches.tag "" release.NugetVersion
    Branches.pushTag "" remote release.NugetVersion

    let files = !! ("./temp" </> "*.vsix")

    // release on github
    let cl =
        GitHub.createClient user pw
        |> GitHub.draftNewRelease gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes

    (cl,files)
    ||> Seq.fold (fun acc e -> acc |> GitHub.uploadFile e)
    |> GitHub.publishDraft//releaseDraft
    |> Async.RunSynchronously

let releaseLocal archiveDir =
    Directory.ensure archiveDir

    !! ("./temp" </> "*.vsix")
    |> Seq.iter (Shell.moveFile archiveDir)

// --------------------------------------------------------------------------------------------------------
// 3. Targets for FAKE
// --------------------------------------------------------------------------------------------------------

Target.initEnvironment ()

Target.create "Clean" (fun _ ->
    Shell.cleanDir "./temp"
    Shell.copyFiles "release" ["README.md"; "LICENSE"]
    Shell.copyFile "release/CHANGELOG.md" "CHANGELOG.md"
)

Target.create "Lint" <| skipOn "no-lint" (fun _ ->
    let version = " --version 0.16.5"    // todo - remove when .net5.0 is used
    DotnetCore.installOrUpdateTool toolsDir ("dotnet-fsharplint" + version)

    let checkResult (messages: string list) =
        let rec check: string list -> unit = function
            | [] -> failwithf "Lint does not yield a summary."
            | head :: rest ->
                if head.Contains "Summary" then
                    match head.Replace("= ", "").Replace(" =", "").Replace("=", "").Replace("Summary: ", "") with
                    | "0 warnings" -> Trace.tracefn "Lint: OK"
                    | warnings -> failwithf "Lint ends up with %s." warnings
                else check rest
        messages
        |> List.rev
        |> check

    !! "**/*.*proj"
    -- "example/**/*.*proj"
    -- "paket-files/**/*.*proj"
    -- ".fable/**/*.*proj"
    |> Seq.map (fun fsproj ->
        match toolsDir with
        | Global ->
            DotnetCore.runInRoot (sprintf "fsharplint lint %s" fsproj)
            |> fun (result: ProcessResult) -> result.Messages
        | Local dir ->
            DotnetCore.execute "dotnet-fsharplint" ["lint"; fsproj] dir
            |> fst
            |> tee (Trace.tracefn "%s")
            |> String.split '\n'
            |> Seq.toList
    )
    |> Seq.iter checkResult
)

Target.create "YarnInstall" <| fun _ ->
    Yarn.install id

Target.create "DotNetRestore" <| fun _ ->
    DotNet.restore id "src"

Target.create "Watch" (fun _ ->
    runFable "--mode development --watch"
)

Target.create "InstallVSCE" ( fun _ ->
    Process.killAllByName "npm"
    run npmTool "install -g vsce" ""
)

Target.create "CopyDocs" (fun _ ->
    Shell.copyFiles "release" ["README.md"; "LICENSE"]
    Shell.copyFile "release/CHANGELOG.md" "CHANGELOG.md"
)

Target.create "RunScript" (fun _ ->
    runFable "--mode production"
)

Target.create "RunDevScript" (fun _ ->
    runFable "--mode development"
)

Target.create "CopyLanguageServer" (fun _ ->
    let languageServerBin = sprintf "%s/bin/Release" languageServerDir
    let releaseBin = "release/bin"
    copyLanguageServer releaseBin languageServerBin
)

Target.create "CopyGrammar" (fun _ ->
    let fsgrammarDir = "grammars"
    let fsgrammarRelease = "release/syntaxes"

    copyGrammar fsgrammarDir fsgrammarRelease
)

Target.create "CopySchemas" (fun _ ->
    let fsschemaDir = "schemas"
    let fsschemaRelease = "release/schemas"

    copySchemas fsschemaDir fsschemaRelease
)

(* Target.create "CopyLib" (fun _ ->
    let libDir = "lib"
    let releaseDir = "release/bin"

    copyLib libDir releaseDir
) *)

Target.create "BuildPackage" ( fun _ ->
    buildPackage "release"
)

Target.create "SetVersion" (fun _ ->
    setVersion release "release"
)

Target.create "PublishToGallery" ( fun _ ->
    publishToGallery "release"
)

Target.create "PublishToGalleryLocal" ( fun _ ->
    // this task is same as "PublishToGallery", but im not sure about task dependencies atm, so it is here only to able to run "ReleaseLocal" without a "ReleaseGitHub"
    publishToGallery "release"
)

Target.create "ReleaseGitHub" (fun _ ->
    releaseGithub release
)

Target.create "ReleaseLocal" (fun _ ->
    releaseLocal archiveDir
)

// --------------------------------------------------------------------------------------------------------
// 4. FAKE targets hierarchy
// --------------------------------------------------------------------------------------------------------

Target.create "Default" ignore
Target.create "Build" ignore
Target.create "BuildDev" ignore
Target.create "BuildExp" ignore
Target.create "Release" ignore
Target.create "BuildPackages" ignore
Target.create "Tests" ignore

"CopyGrammar" ==> "RunScript"
"YarnInstall" ==> "RunScript"
"DotNetRestore" ==> "RunScript"

"Clean"
==> "RunScript"
==> "Default"

"Clean"
==> "RunScript"
==> "CopyDocs"
==> "CopyLanguageServer"
==> "CopySchemas"
//==> "CopyLib"
==> "Build"


"YarnInstall" ==> "Build"
"DotNetRestore" ==> "Build"

"Lint" ==> "Tests"

"Build"
==> "Tests"
==> "SetVersion"
==> "BuildPackage"
==> "ReleaseGitHub"
==> "PublishToGallery"
==> "Release"

"Build"
==> "Tests"
==> "SetVersion"
==> "BuildPackage"
==> "PublishToGalleryLocal"
==> "ReleaseLocal"

"CopyGrammar" ==> "Watch"
"YarnInstall" ==> "Watch"
"DotNetRestore" ==> "Watch"

"CopyGrammar" ==> "RunDevScript"
"YarnInstall" ==> "RunDevScript"
"DotNetRestore" ==> "RunDevScript"

"RunDevScript"
==> "BuildDev"

Target.runOrDefault "Default"

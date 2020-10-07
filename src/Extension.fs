[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MF.Tuc.Extension

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.vscode
open Ionide.VSCode.Helpers
open MF.Tuc.Extension
open global.Node.ChildProcess
//open Debugger - ?

(* type Api =
    { ProjectLoadedEvent : Event<DTO.Project>
      BuildProject : DTO.Project -> Fable.Import.JS.Promise<string>
      BuildProjectFast : DTO.Project -> Fable.Import.JS.Promise<string>
      GetProjectLauncher : OutputChannel -> DTO.Project -> (string -> Fable.Import.JS.Promise<ChildProcess>) option
      DebugProject : DTO.Project -> string [] -> Fable.Import.JS.Promise<unit> } *)

let activate (context : ExtensionContext) = // : JS.Promise<Api> =

    (* let resolve = "FSharp.resolveNamespaces" |> Configuration.get false
    let solutionExplorer = "FSharp.enableTreeView" |> Configuration.get true

    let analyzers = "FSharp.enableAnalyzers" |> Configuration.get false
    let showExplorer = "FSharp.showExplorerOnStartup" |> Configuration.get true *)

    let init = DateTime.Now

    printfn "Fable extension is starting at %A!" init

    LanguageService.start context
    |> Promise.onSuccess (fun _ ->
        printfn "LanguageServices started at %A ..." DateTime.Now
    )
    |> Promise.catch ignore

    (* LanguageService.start context
    |> Promise.onSuccess (fun _ ->
        let progressOpts = createEmpty<ProgressOptions>
        progressOpts.location <- ProgressLocation.Window
        window.withProgress(progressOpts, (fun p ->
            let pm = createEmpty<ProgressMessage>
            pm.message <- "Loading projects"
            p.report pm

            Project.activate context
            |> Promise.onSuccess(fun _ -> QuickInfoProject.activate context )
            |> Promise.onSuccess (fun _ ->
                if showExplorer then
                    commands.executeCommand(VSCodeExtension.workbenchViewId ())
                    |> ignore)
            |> Promise.onSuccess (fun _ ->
                LanguageService.loadAnalyzers ()
                |> ignore
            )
        ))
        |> ignore
    )
    |> Promise.catch (fun error -> promise { () }) // prevent unhandled rejected promises
    |> Promise.map (fun _ ->
        if solutionExplorer then SolutionExplorer.activate context
        Diagnostics.activate context
        LineLens.activate context
        QuickInfo.activate context
        Help.activate context
        MSBuild.activate context
        SignatureData.activate context
        Debugger.activate context
        Fsdn.activate context
        Forge.activate context
        Fsi.activate context
        ScriptRunner.activate context
        LanguageConfiguration.activate context
        HtmlConverter.activate context
        InfoPanel.activate context
        CodeLensHelpers.activate context
        FakeTargetsOutline.activate context
        Gitignore.activate context
        HighlightingProvider.activate context
        FSharpLiterate.activate context
        PipelineHints.activate context

        let buildProject project = promise {
            let! exit = MSBuild.buildProjectPath "Build" project
            match exit.Code with
            | Some code -> return code.ToString()
            | None -> return ""
        }

        let buildProjectFast project = promise {
            let! exit = MSBuild.buildProjectPathFast project
            match exit.Code with
            | Some code -> return code.ToString()
            | None -> return ""
        }

        let event = Fable.Import.vscode.EventEmitter<DTO.Project>()
        Project.projectLoaded.Invoke(fun n ->
            !!(setTimeout (fun _ -> event.fire n) 500.)
        ) |> ignore

        { ProjectLoadedEvent = event.event
          BuildProject = buildProject
          BuildProjectFast = buildProjectFast
          GetProjectLauncher = Project.getLauncher
          DebugProject = debugProject }
    ) *)
(*
let deactivate(disposables : Disposable[]) =
    LanguageService.stop ()
 *)

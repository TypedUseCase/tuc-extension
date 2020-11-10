[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Tuc.Extension

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.vscode
open Ionide.VSCode.Helpers
open Tuc.Extension
open global.Node.ChildProcess

let activate (context : ExtensionContext) = // : JS.Promise<Api> =
    printfn "[TUC] Extension is starting at %A!" DateTime.Now

    LanguageService.start Project.update context
    |> Promise.onSuccess (fun _ ->
        printfn "[TUC] LanguageServices started at %A ..." DateTime.Now

        let progressOpts = createEmpty<ProgressOptions>
        progressOpts.location <- ProgressLocation.Window

        window.withProgress(progressOpts, (fun p ->
            let pm = createEmpty<ProgressMessage>
            pm.message <- "[TUC] Loading ..."
            p.report pm

            Project.activate context LanguageService.tucInfo
        ))
        |> ignore

    )
    |> Promise.catch ignore
    |> Promise.map (fun _ ->
        KeywordsCompletion.activate context
    )

let deactivate(disposables : Disposable[]) =
    LanguageService.stop ()

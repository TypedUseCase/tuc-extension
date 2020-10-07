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
    printfn "Fable extension is starting at %A!" DateTime.Now

    LanguageService.start context
    |> Promise.onSuccess (fun _ ->
        printfn "LanguageServices started at %A ..." DateTime.Now
    )
    |> Promise.catch ignore
    |> Promise.map (fun _ ->
        KeywordsCompletion.activate context
    )

let deactivate(disposables : Disposable[]) =
    LanguageService.stop ()

namespace MF.Tuc.Extension

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.vscode
open Fable.Import.Node
open Ionide.VSCode.Helpers

open DTO
open LanguageServer

module Notifications =
    type DocumentParsedEvent =
        { fileName : string
          version : float
          /// BEWARE: Live object, might have changed since the parsing
          document : TextDocument }


    let onDocumentParsedEmitter = EventEmitter<DocumentParsedEvent>()
    let onDocumentParsed = onDocumentParsedEmitter.event

    let private tooltipRequestedEmitter = EventEmitter<Position>()
    let tooltipRequested = tooltipRequestedEmitter.event

    let mutable notifyWorkspaceHandler : Option<Choice<ProjectResult,ProjectLoadingResult,(string * ErrorData),string> -> unit> = None

module LanguageService =
    let [<Literal>] private TucLanguageShortName = "tuc"
    let [<Literal>] private TucLanguageName = "TypedUseCase"

    module Types =
        type PlainNotification= { content: string }

        type ConfigValue<'a> =
        | UserSpecified of 'a
        | Implied of 'a

        type [<RequireQualifiedAccess>] FSACTargetRuntime =
        | NET
        | NetcoreFdd

        /// Position in a text document expressed as zero-based line and zero-based character offset.
        /// A position is between two characters like an ‘insert’ cursor in a editor.
        type Position = {
            /// Line position in a document (zero-based).
            Line: int

            /// Character offset on a line in a document (zero-based). Assuming that the line is
            /// represented as a string, the `character` value represents the gap between the
            /// `character` and `character + 1`.
            ///
            /// If the character value is greater than the line length it defaults back to the
            /// line length.
            Character: int
        }

        type DocumentUri = string

        type TextDocumentIdentifier = {Uri: DocumentUri }

        type TextDocumentPositionParams = {
            TextDocument: TextDocumentIdentifier
            Position: Position
        }

        type FileParams = {
            Project: TextDocumentIdentifier
        }

        type WorkspaceLoadParms = {
            TextDocuments: TextDocumentIdentifier[]
        }

        type HighlightingRequest = {FileName : string; }
        type FSharpLiterateRequest = {FileName: string}
        type FSharpPieplineHintsRequest = {FileName: string}

    let mutable client : LanguageClient option = None

    let private createClient opts =
        let options =
            createObj [
                "run" ==> opts
                "debug" ==> opts
                ] |> unbox<ServerOptions>

        let fileDeletedWatcher = workspace.createFileSystemWatcher("**/*.{tuc}", true, true, false)  // todo - add fs,fsx ?

        let clientOpts =
            let opts = createEmpty<Client.LanguageClientOptions>
            let selector =
                createObj [
                    "language" ==> TucLanguageShortName
                ] |> unbox<Client.DocumentSelector>

            let initOpts =
                createObj [
                    "AutomaticWorkspaceInit" ==> false
                ]

            let synch = createEmpty<Client.SynchronizeOptions>
            synch.configurationSection <- Some !^TucLanguageName
            synch.fileEvents <- Some( !^ ResizeArray([fileDeletedWatcher]))

            opts.documentSelector <- Some !^selector
            opts.synchronize <- Some synch
            opts.revealOutputChannelOn <- Some Client.RevealOutputChannelOn.Never


            opts.initializationOptions <- Some !^(Some initOpts)

            opts

        let cl = LanguageClient(TucLanguageName, TucLanguageShortName, options, clientOpts, false)
        client <- Some cl
        cl

    (* let private readyClient (cl: LanguageClient) =
        cl.onReady ()
        |> Promise.onSuccess (fun _ ->
            cl.onNotification("fsharp/notifyWorkspace", (fun (a: Types.PlainNotification) ->
                match Notifications.notifyWorkspaceHandler with
                | None -> ()
                | Some cb ->
                    let onMessage res =
                        match res?Kind |> unbox with
                        | "project" ->
                            res |> unbox<ProjectResult> |> deserializeProjectResult |> Choice1Of4 |> cb
                        | "projectLoading" ->
                            res |> unbox<ProjectLoadingResult> |> Choice2Of4 |> cb
                        | "error" ->
                            res?Data |> parseError |> Choice3Of4 |> cb
                        | "workspaceLoad" ->
                            res?Data?Status |> unbox<string> |> Choice4Of4 |> cb
                        | _ ->
                            ()
                    let res = a.content |> ofJson<obj>
                    onMessage res
            ))

            cl.onNotification("fsharp/fileParsed", (fun (a: Types.PlainNotification) ->
                let fn = a.content
                let te = window.visibleTextEditors |> Seq.find (fun n -> path.normalize(n.document.fileName).ToLower() = path.normalize(fn).ToLower())

                let ev = {Notifications.fileName = a.content; Notifications.version = te.document.version; Notifications.document = te.document }

                Notifications.onDocumentParsedEmitter.fire ev

                ()
            ))
        ) *)

    let provideKeyWords () =
        let selector =
            createObj [
                "language" ==> TucLanguageShortName
            ] |> unbox<DocumentSelector>

        let provider =
            { new CompletionItemProvider with
                member this.provideCompletionItems(document, position, token) =
                    let item = CompletionItem("hello-world")
                    //item.label <- "hello-world"
                    item.kind <- CompletionItemKind.Property
                    item.insertText <- (U2.Case2 <| SnippetString "Hello ${1:name}!")
                    item.detail <- "Hello world from F#!"

                    let x: ResizeArray<CompletionItem> =
                        ResizeArray([
                            item
                        ])
                    x
                    |> U2.Case1
                    // |> unbox<ResizeArray<CompletionItem>>

                member this.resolveCompletionItem(item, token) = item |> U2.Case1
            }

        let tucKeywordProvider = vscode.languages.registerCompletionItemProvider(selector, provider)
        tucKeywordProvider

    let start (c : ExtensionContext) =
        promise {
            (* let! startOpts = getOptions ()
            let cl = createClient startOpts
            c.subscriptions.Add (cl.start ())
            let! _ = readyClient cl *)

            let tucKeywordProvider = provideKeyWords()
            c.subscriptions.Add(tucKeywordProvider)

            return ()
        }

    let stop () =
        promise {
            match client with
            | Some cl -> return! cl.stop()
            | None -> return ()
        }

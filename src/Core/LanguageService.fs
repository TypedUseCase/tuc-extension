namespace Tuc.Extension

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.vscode
open global.Node
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
    module Types =
        type PlainNotification= { content: string }


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

    let private handleUntitled (fn : string) = if fn.EndsWith ".fsx" then fn else (fn + ".fsx")

    let private createClient opts =
        let options =
            createObj [
                "run" ==> opts
                "debug" ==> opts
                ] |> unbox<ServerOptions>

        let fileWatcher = workspace.createFileSystemWatcher("**/*.{tuc}", false, true, false)

        let clientOpts =
            let opts = createEmpty<Client.LanguageClientOptions>
            let selector =
                [
                    Tuc.LanguageShortName
                ]
                |> ResizeArray

            let initOpts =
                createObj [
                    "AutomaticWorkspaceInit" ==> false
                ]

            let synch = createEmpty<Client.SynchronizeOptions>
            synch.configurationSection <- Some !^Tuc.LanguageName
            synch.fileEvents <- Some( !^ ResizeArray([fileWatcher]))

            opts.documentSelector <- Some !^selector
            opts.synchronize <- Some synch
            opts.revealOutputChannelOn <- Some Client.RevealOutputChannelOn.Never    // Shows the error output only | Use `.Info` for local development logs


            opts.initializationOptions <- Some !^(Some initOpts)

            opts

        let cl = LanguageClient(Tuc.LanguageName, Tuc.LanguageShortName, options, clientOpts, false)
        cl.initializeResult |> Option.iter (fun r -> printfn "client.init: %A" r)
        client <- Some cl
        cl

    let getOptions () =

        let dotnetNotFound () = promise {
            let msg = """
            Cannot start .NET Core language services because `dotnet` was not found.
            Consider:
            * setting the `FSharp.dotnetRoot` settings key to a directory with a `dotnet` binary,
            * including `dotnet` in your PATH, or
            * installing .NET Core into one of the default locations.
            """
            let! result = vscode.window.showErrorMessage(msg)
            return failwith "no `dotnet` binary found"
        }

        // let backgroundSymbolCache = "FSharp.enableBackgroundServices" |> Configuration.get true
        let languageServerPath =
            match "TUC.languageServer.path" |> Configuration.get "" |> String.trim with
            | String.IsEmpty -> None
            | path -> Some path

        let verbosity = "TUC.languageServer.verbosity" |> Configuration.get "" |> String.trim

        let startServer languageServerPath = promise {
            let args =
                [
                    yield "ls:start"
                    //if backgroundSymbolCache then yield "--background-service-enabled"

                    match verbosity |> String.toLower with
                    | "verbose" | "v" -> yield "-v"
                    | "veryverbose" | "vv" -> yield "-vv"
                    | "debug" | "vvv" -> yield "-vvv"
                    | _ -> ()
                ]

            printfn "[TUC.LS] start with %A" [
                "languageServerPath", languageServerPath
                "verbosity", verbosity
            ]

            match languageServerPath with
            | languageServerPath when languageServerPath |> String.endWith ".dll" ->
                let! dotnet = Environment.dotnet

                match dotnet with
                | Some dotnet ->
                    printfn "[TUC.LS] dotnet path: %A" dotnet

                    return
                        [
                            "command" ==> dotnet
                            "args" ==> (languageServerPath :: args |> ResizeArray)
                            "transport" ==> 0
                        ]
                        |> tee (printfn "[TUC.LS] Start by dotnet with: %A")
                        |> createObj
                | None ->
                    return! dotnetNotFound ()

            | languageServerPathExecutable ->
                return
                    [
                        "command" ==> languageServerPathExecutable
                        "args" ==> (args |> ResizeArray)
                        "transport" ==> 0
                    ]
                    |> tee (printfn "[TUC.LS] Start with: %A")
                    |> createObj
        }

        match languageServerPath, VSCodeExtension.pluginPath() with
        | Some languageServerPath, _ -> languageServerPath
        | _, Some pluginPath -> pluginPath + "/bin/LanguageServer.dll"
        | _ -> "release/bin/net5.0/LanguageServer.dll"  // to use this, you would probably need a full system path, you should use it in TUC.languageServer.path configuration
        |> tee (printfn "[TUC.LS] Path: '%s'")
        |> startServer

    let tucInfo path =
        match client with
        | None -> Promise.empty
        | Some client ->
            let req : Types.PlainNotification = {
                content = path
            }

            client.sendRequest("tuc/info", req)
            |> Promise.map (fun (res: Types.PlainNotification) ->
                res.content
            )

    let private readyClient projectUpdate (cl: LanguageClient) =
        cl.onReady ()
        |> Promise.onSuccess (fun _ ->

            cl.onNotification("tuc/domainResolved", (fun (a: Types.PlainNotification) ->
                printfn "[TUC] On tuc/domainResolved %A -> update project ..." a.content
                projectUpdate DomainResolved
            ))

            cl.onNotification("tuc/fileParsed", (fun (a: Types.PlainNotification) ->
                printfn "[TUC] On tuc/fileParsed %A -> update project ..." a.content
                projectUpdate (TucParsed a.content)
            ))

            printfn "[TUC] Ready"
        )

    let start projectUpdate (c : ExtensionContext) =
        promise {
            let! startOpts = getOptions ()
            let cl = createClient startOpts
            c.subscriptions.Add (cl.start ())
            let! _ = readyClient projectUpdate cl

            return ()
        }

    let stop () =
        promise {
            match client with
            | Some cl -> return! cl.stop()
            | None -> return ()
        }

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

        let fileWatcher = workspace.createFileSystemWatcher("**/*.{tuc,fsx}", false, true, false)

        let clientOpts =
            let opts = createEmpty<Client.LanguageClientOptions>
            let selector =
                [
                    Tuc.LanguageShortName
                    "fsharp"    // todo tohle tady asi byt nesmi, jinak nebude fungoat ionide a watch na domenu musi bezet jen uvnitr LS
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
            opts.revealOutputChannelOn <- Some Client.RevealOutputChannelOn.Info    // Shows the error output only


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
        let languageServerPath = "TUC.languageServer.path" |> Configuration.get ""
        let verbosity = "TUC.languageServer.verbosity" |> Configuration.get ""

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

            printfn "[LS] start with %A" [
                "languageServerPath", languageServerPath
                "verbosity", verbosity
            ]

            match languageServerPath with
            | languageServerPath when languageServerPath |> String.endWith ".dll" ->
                let! dotnet = Environment.dotnet

                match dotnet with
                | Some dotnet ->
                    printfn "[LS] dotnet path: %A" dotnet

                    return
                        [
                            "command" ==> dotnet
                            "args" ==> (languageServerPath :: args |> ResizeArray)
                            "transport" ==> 0
                        ]
                        |> tee (printfn "[LS] Start by dotnet with: %A")
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
                    |> tee (printfn "[LS] Start with: %A")
                    |> createObj
        }

        match languageServerPath with
        | String.IsEmpty ->
            match VSCodeExtension.pluginPath() with
            | Some path -> path + "/bin/LanguageServer.dll"
            | _ -> "release/bin/netcoreapp3.1/LanguageServer.dll"
        | languageServerPath -> languageServerPath
        |> tee (printfn "LanguageServer: '%s'")
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

            cl.onNotification("tuc/fileParsed", (fun (a: Types.PlainNotification) ->
                printfn "[TUC] On tuc/fileParsed %A -> update project ..." a.content
                projectUpdate a.content
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

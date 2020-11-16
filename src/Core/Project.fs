namespace Tuc.Extension

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.vscode
open global.Node
open Ionide.VSCode.Helpers

open DTO
open System.Collections.Generic
module node = Node.Api


module Project =

    let private statusUpdatedEmitter = EventEmitter<unit>()
    let statusUpdated = statusUpdatedEmitter.event

    module internal ProjectStatus =
        let mutable timer = None
        let mutable path = ""

        let clearTimer () =
            match timer with
            | Some t ->
                clearTimeout t
                timer <- None
            | _ -> ()

        let mutable item : StatusBarItem option = None
        let private hideItem () = item |> Option.iter (fun n -> n.hide ())

        let private showItem (text : string) =
            match item with
            | Some item ->
                item.text <- sprintf "[TUC] %s" text
                item.show()
            | _ -> ()

        let update tucInfo () =
            promise {
                let! info = tucInfo path
                printfn "[TUC] Info: %s" info

                showItem info
            }
            |> Promise.start

        let statusUpdateHandler tucInfo () =
            clearTimer()
            timer <- Some (setTimeout (fun () -> update tucInfo ()) 1000.)

    let update =
        (function
            | DomainResolved -> ProjectStatus.path <- ""
            | TucParsed filePath -> ProjectStatus.path <- filePath
        )
        >> tee (statusUpdatedEmitter.fire)

    let activate (context : ExtensionContext) tucInfo =
        printfn "[TUC] Project activate ..."

        ProjectStatus.item <- Some (window.createStatusBarItem (StatusBarAlignment.Right, 9000. ))
        statusUpdated.Invoke(!!ProjectStatus.statusUpdateHandler tucInfo) |> context.subscriptions.Add

        Promise.empty

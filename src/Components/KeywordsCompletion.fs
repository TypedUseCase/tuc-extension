namespace Tuc.Extension

open Fable.Import
open Fable.Import.vscode

module KeywordsCompletion =

    open Fable.Core
    open Fable.Core.JsInterop

    type private Keyword = {
        Label: string
        Snippet: string
        Detail: string option
        Documentation: string option
    }

    let private keywords =
        [
            // tuc name
            { Label = "tuc"; Snippet = "tuc"; Detail = Some "Tuc name"; Documentation = Some "Tuc name is a start of a tuc definition." }
            { Label = "tuc"; Snippet = "tuc ${1:name}"; Detail = Some "Tuc name"; Documentation = Some "Tuc name is a start of a tuc definition." }

            // participants
            { Label = "participants"; Snippet = "participants"; Detail = Some "Participants"; Documentation = None }
            { Label = "participants"; Snippet = "participants\n\t${0}"; Detail = Some "Participants"; Documentation = None }
            { Label = "participant-component"; Snippet = "${1:ComponentType} ${2:Domain}\n\t"; Detail = Some "Component Participant"; Documentation = None }
            { Label = "participant-component-participant"; Snippet = "${1:ParticipantType}"; Detail = Some "Participant of a Component"; Documentation = None }
            { Label = "participant-component-participant-alias"; Snippet = "${1:ParticipantType} as \"${2:Alias}\""; Detail = Some "Participant of a Component with Alias"; Documentation = None }
            { Label = "participant-active"; Snippet = "${1:ParticipantType} ${2:Domain}"; Detail = Some "Active Participant"; Documentation = None }
            { Label = "participant-active-alias"; Snippet = "${1:ParticipantType} ${2:Domain} as \"${3:Alias}\""; Detail = Some "Active Participant with Alias"; Documentation = None }

            // parts
            { Label = "section"; Snippet = "section"; Detail = Some "Section name"; Documentation = None }
            { Label = "section"; Snippet = "section ${1:section name}"; Detail = Some "Section name"; Documentation = None }
            { Label = "read-data"; Snippet = "[${1:dataObject}] -> ${2:data}"; Detail = Some "Read data from data object"; Documentation = None }
            { Label = "post-data"; Snippet = "${1:data} -> [${2:dataObject}]"; Detail = Some "Post data to data object"; Documentation = None }
            { Label = "post-event"; Snippet = "${1:event} -> [${2:streamName}Stream]"; Detail = Some "Read event from stream"; Documentation = None }
            { Label = "read-event"; Snippet = "[${1:streamName}Stream] -> ${2:event}"; Detail = Some "Post event to stream"; Documentation = None }
            { Label = "group"; Snippet = "group"; Detail = Some "Group name"; Documentation = None }
            { Label = "group"; Snippet = "group ${1:group name}\n\t"; Detail = Some "Group name"; Documentation = None }
            { Label = "if"; Snippet = "if"; Detail = Some "If"; Documentation = None }
            { Label = "if"; Snippet = "if ${1:condition}\n\t"; Detail = Some "If condition"; Documentation = None }
            { Label = "if-else"; Snippet = "if ${1:condition}\n\t${0}\nelse\n\t"; Detail = Some "If condition Else"; Documentation = None }
            { Label = "else"; Snippet = "else"; Detail = Some "Else"; Documentation = None }
            { Label = "loop"; Snippet = "loop"; Detail = Some "Loop"; Documentation = None }
            { Label = "loop"; Snippet = "loop ${1:condition}\n\t"; Detail = Some "Loop condition"; Documentation = None }
            { Label = "do"; Snippet = "do"; Detail = Some "Do action(s)"; Documentation = None }
            { Label = "do"; Snippet = "do ${1:action}"; Detail = Some "Do action"; Documentation = None }
            { Label = "left-note"; Snippet = "\"< ${1:note}\""; Detail = Some "Left Note"; Documentation = None }
            { Label = "note"; Snippet = "\"${1:note}\""; Detail = Some "Note"; Documentation = None }
            { Label = "right-note"; Snippet = "\"> ${1:note}\""; Detail = Some "Right Note"; Documentation = None }
            { Label = "left-note-multi"; Snippet = "\"<\"\n${1:note}\n\"<\""; Detail = Some "Multi-line Left Note"; Documentation = None }
            { Label = "note-multi"; Snippet = "\"\"\"\n${1:note}\n\"\"\""; Detail = Some "Multi-line Note"; Documentation = None }
            { Label = "right-note-multi"; Snippet = "\">\"\n${1:note}\n\">\""; Detail = Some "Multi-line Right Note"; Documentation = None }
        ]

    let private provideKeyWords () =
        let selector =
            createObj [
                "language" ==> Tuc.LanguageShortName
            ] |> unbox<DocumentSelector>

        let provider =
            { new CompletionItemProvider with
                member __.provideCompletionItems(document, position, token) =
                    keywords
                    |> List.mapi (fun index { Label = label; Snippet = snippet; Detail = detail; Documentation = documentation } ->
                        let item =
                            CompletionItem(
                                label = label,
                                kind = CompletionItemKind.Keyword,
                                insertText = !^SnippetString(snippet),
                                sortText = sprintf "1000000%d" index,
                                filterText = label
                            )

                        match detail with
                        | Some detail -> item.detail <- detail
                        | _ -> ()

                        match documentation with
                        | Some documentation -> item.documentation <- !^MarkdownString(documentation)
                        | _ -> ()

                        item
                    )
                    |> ResizeArray
                    |> (!^)

                member __.resolveCompletionItem(item, token) = !^item
            }

        vscode.languages.registerCompletionItemProvider(selector, provider)

    let activate (context: ExtensionContext) =
        context.subscriptions.Add(provideKeyWords())

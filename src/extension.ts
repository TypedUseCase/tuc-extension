/*---------------------------------------------------------
 * Copyright (C) Microsoft Corporation. All rights reserved.
 *--------------------------------------------------------*/

import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {

    const tucKeywordProvider = vscode.languages.registerCompletionItemProvider('tuc', {

        provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext) {

            const tucParts =
                [
                    // tuc name
                    { label: 'tuc', snippet: 'tuc', detail: 'Tuc name', documentation: 'Tuc name is a start of a tuc definition.' },
                    { label: 'tuc', snippet: 'tuc ${1:name}', detail: 'Tuc name', documentation: 'Tuc name is a start of a tuc definition.' },

                    // participants
                    { label: 'participants', snippet: 'participants', detail: 'Participants' },
                    { label: 'participants', snippet: 'participants\n\t${0}', detail: 'Participants' },
                    { label: 'participant-component', snippet: '${1:ComponentType} ${2:Domain}\n\t', detail: 'Component Participant' },
                    { label: 'participant-component-participant', snippet: '${1:ParticipantType}', detail: 'Participant of a Component' },
                    { label: 'participant-component-participant-alias', snippet: '${1:ParticipantType} as "${2:Alias}"', detail: 'Participant of a Component with Alias' },
                    { label: 'participant-active', snippet: '${1:ParticipantType} ${2:Domain}', detail: 'Active Participant' },
                    { label: 'participant-active-alias', snippet: '${1:ParticipantType} ${2:Domain} as "${3:Alias}"', detail: 'Active Participant with Alias' },

                    // parts
                    { label: 'section', snippet: 'section', detail: 'Section name' },
                    { label: 'section', snippet: 'section ${1:section name}', detail: 'Section name' },
                    { label: 'read-data', snippet: '[${1:dataObject}] -> ${2:data}', detail: 'Read data from data object' },
                    { label: 'post-data', snippet: '${1:data} -> [${2:dataObject}]', detail: 'Post data to data object' },
                    { label: 'post-event', snippet: '${1:event} -> [${2:streamName}Stream]', detail: 'Read event from stream' },
                    { label: 'read-event', snippet: '[${1:streamName}Stream] -> ${2:event}', detail: 'Post event to stream' },
                    { label: 'group', snippet: 'group', detail: 'Group name' },
                    { label: 'group', snippet: 'group ${1:group name}\n\t', detail: 'Group name' },
                    { label: 'if', snippet: 'if', detail: 'If' },
                    { label: 'if', snippet: 'if ${1:condition}\n\t', detail: 'If condition' },
                    { label: 'if-else', snippet: 'if ${1:condition}\n\t${0}\nelse\n\t', detail: 'If condition Else' },
                    { label: 'else', snippet: 'else', detail: 'Else' },
                    { label: 'loop', snippet: 'loop', detail: 'Loop' },
                    { label: 'loop', snippet: 'loop ${1:condition}\n\t', detail: 'Loop condition' },
                    { label: 'do', snippet: 'do', detail: 'Do action(s)' },
                    { label: 'do', snippet: 'do ${1:action}', detail: 'Do action' },
                    { label: 'left-note', snippet: '"< ${1:note}"', detail: 'Left Note' },
                    { label: 'note', snippet: '"${1:note}"', detail: 'Note' },
                    { label: 'right-note', snippet: '"> ${1:note}"', detail: 'Right Note' },
                    { label: 'left-note-multi', snippet: '"<"\n${1:note}\n"<"', detail: 'Multi-line Left Note' },
                    { label: 'note-multi', snippet: '"""\n${1:note}\n"""', detail: 'Multi-line Note' },
                    { label: 'right-note-multi', snippet: '">"\n${1:note}\n">"', detail: 'Multi-line Right Note' },
                ]
                    .map(({ label, snippet = null, detail = null, documentation = null }) => {
                        const item = new vscode.CompletionItem(label, vscode.CompletionItemKind.Keyword);

                        if (snippet !== null) {
                            item.insertText = new vscode.SnippetString(snippet);
                        }
                        if (detail !== null) {
                            item.detail = detail;
                        }
                        if (documentation !== null) {
                            item.documentation = new vscode.MarkdownString(documentation);
                        }

                        return item;
                    });

            // return all completion items as array
            return tucParts;
        }
    });

    context.subscriptions.push(tucKeywordProvider);
}

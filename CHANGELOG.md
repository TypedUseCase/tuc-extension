# Changelog

<!-- There is always Unreleased section on the top. Subsections (Add, Changed, Fix, Removed) should be Add as needed. -->
## Unreleased
- Show more info about language server and extension

## 2.2.0 - 2021-06-11
- Update dependencies
- Use net5.0 and F# 5

## 2.1.0 - 2020-11-16
- Handle `tuc` files only (*Domain `.fsx` files are handled by LS itself*)
- Handle `tuc/domainResolved` notification

## 2.0.0 - 2020-11-16
- Change `comment.line` to `comment.line.double-slash`
- Add recommended extensions
- Upgrade `Tuc` Grammar
    - Do not mark _empty_ keyword as invalid by a grammar.
    - Add Data read/write operator
- Show Tuc status item

## 1.2.0 - 2020-10-05
- Add code completion for tuc keywords

## 1.1.0 - 2020-10-01
- Mark escaped characters in string

## 1.0.0 - 2020-10-01
- Mark texts as strings
    - `section` name
    - `group` name
    - `if` condition value
    - `loop` condition value
- Mark `tuc name` as `key.word.other`
- Underline `tuc name`
- Fix `tuc name` with `tuc` word, which is wrongly marked as invalid
- Fix `link` syntax highlighting in strings

## 0.3.0 - 2020-09-10
- Add logo

## 0.2.0 - 2020-09-10
- Parse whole `participants` section
- Mark one-line notes with start/end and mark left/right sign
- Add more surrounding chars
- Format links and tooltips in notes
- Highlight method and handler calls
- Highlight read/write data
- Highlight caller in lifeline
- Highlight stream for handle

## 0.1.0 - 2020-09-07
- Initial release

TypedUseCase - .tuc
===================

![Checks](https://github.com/TypedUseCase/tuc-extension/workflows/Checks/badge.svg?branch=master)
[![Version](https://vsmarketplacebadge.apphb.com/version/TypedUseCase.tuc.svg)](https://marketplace.visualstudio.com/items?itemName=TypedUseCase.tuc)
[![tuc-docs](https://img.shields.io/badge/documentation-tuc-orange.svg)](https://typedusecase.github.io/)

> Extension for visual studio code, which add support for TUC language.

## TUC
> **T**yped **U**se-**C**ase

![tuc-logo](https://github.com/TypedUseCase/TypedUseCase.github.io/raw/master/assets/tuc-logo.png)

It is basically a use case definition, for which this console application can generate [PlantUML](https://plantuml.com/) diagram, where all services are domain specific type safe.

For more information, go check [tuc-console](https://github.com/TypedUseCase/tuc-console) or [documentation](https://typedusecase.github.io/).

## Tuc Extension features

### Syntax Highlighting
It highlights a syntax for keywords, participants and other parts of Tuc and even most common errors.

### Code Completion
It auto-complete a keywords, participants and other parts of Tuc.

---
## How to build and test a local version of Tuc.Extension

### Prerequisites

- [Visual Studio Code][vscode] 🙄
- [.NET Core 5.0][dotnet]
- [Node.js][nodejs]
- [Yarn][yarn]

### Building

Fork, from the github interface https://github.com/TypedUseCase/tuc-extension
 - if you don't use a certificate for committing to github:
```bash
git clone https://github.com/YOUR_GITHUB_USER/tuc-extension.git
```
 - if you use a certificate for github authentication:
```bash
git clone git@github.com:YOUR_GITHUB_USER/tuc-extension.git
```

#### First time build:
```bash
cd tuc-extension
./build.sh  # or build.cmd if your OS is Windows  (might need ./build Build here)
```

If `dotnet restore` gives the error ` The tools version "14.0" is unrecognized`, then you need to install [msbuildtools2015][msbuildtools2015]

If `dotnet restore` gives the error `error MSB4126: The specified solution configuration "Debug|x64" is invalid`, there's a good chance you have the `Platform` environment variable set to "x64".  Unset the variable and try the restore command again.

If `./build.sh` gives errors, you may need to run `./build.sh -t Build` one time.


Everything is done via `build.cmd` \ `build.sh` (_for later on, lets call it just `build`_).

- `build -t Build` does a full-build, including package installation and copying some necessary files.<br/>
  It should always be done at least once after any clone/pull.
- If a git dependency fails to build paket won't re-do it you can run their build scripts manually:
  - In `paket-files\github.com\fsharp\FsAutoComplete` run `build LocalRelease`

### Launching the extension

Once the initial build on the command line is completed, you should use vscode itself to build and launch the development extension.   To do this,

- open the project folder in vscode
- Use one of the following two configurations which will build the project and launch a new vscode instance running your vscode extension
- In VSCode two configurations are possible to run:
  - Use `Build and Launch Extension`
  - Start the `Watch` task and when a build is done start `Launch Only`

These two options can be reached in VsCode in the side bar (look for a Beetle symbol), or by typing `control-P Debug <space> ` and then selecting either `Build and Launch` or `Watch`

The new extension window will appear with window title `Extension development host`

### Working with FSAC

1. Open FSAC from a new instance of VSCode from the directory: `paket-files/github.com/fsharp/FsAutoComplete`
2. Build the FSAC solution and copy the dll output from the output log, it should be something like: `paket-files/github.com/fsharp/FsAutoComplete/src/FsAutoComplete/bin/Debug/netcoreapp2.1/fsautocomplete.dll`.  Note `netcoreapp2.1` may be a different version.
3. In the instance of VSCode that you have Tuc.Extension open, open settings (`CMD ,` or `Ctrl ,`), and find the section `FSharp > Fsac: Net Core Dll Path` and paste the output you copied from step 3.
4. Now find the section `FSharp > Fsac: Attach Debugger` and check the check box.
5. Close settings
6. Goto the debug section and hit `Build and Launch extension`, after a while another instance of VSCode will start, you can use this instance to test Tuc.Extension/FsAutoComplete.
7. To attach the debugger go back to the instance of VSCode where you open FSAC and goto the debug section, hit `.NET Core Attach` in the list shown you should see all the dotnet processes running, choose one that has `fsautocomplete.dll --mode lsp --attachdebugger` shown.
8. Now you will be able to use breakpoints in the FsAutocomplete solution to debug the instance from step 6.

There is a video [here](https://www.youtube.com/watch?v=w36_PvHNoPY) that goes through the steps and fixing a bug in a little more detail.

Remove the settings from steps 3 and 4 to go back to FSAC bundled in Tuc extension.

### Dependencies

[dotnet]: https://www.microsoft.com/net/download/core
[nodejs]: https://nodejs.org/en/download/
[yarn]: https://yarnpkg.com/en/docs/install
[vscode]: https://code.visualstudio.com/Download

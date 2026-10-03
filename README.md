# PhasmophobiaTools

A C# plugin project for experimenting with Phasmophobia through BepInEx 6 and Unity IL2CPP.

## Requirements

- Windows x64
- .NET 6 SDK
- Visual Studio 2022 or another compatible .NET IDE
- BepInEx 6 IL2CPP installed into the Phasmophobia game directory

## Build

Open `PhasmophobiaTools.sln` and build the `PhasmophobiaTools` project.

The compiled plugin is:

```text
src/PhasmophobiaTools/bin/<Configuration>/net6.0/PhasmophobiaTools.dll
```

Copy the DLL to:

```text
<Phasmophobia>/BepInEx/plugins/PhasmophobiaTools/PhasmophobiaTools.dll
```

Then start Phasmophobia. A successful load writes this message to the BepInEx log:

```text
Phasmophobia Tools v0.1.0 loaded.
```

## Project structure

```text
PhasmophobiaTools.sln
src/
  PhasmophobiaTools/
    PhasmophobiaTools.csproj
    Plugin.cs
```

## Notes

The project intentionally starts with only the plugin bootstrap. Game-specific hooks, Harmony patches, hotkeys, UI, and runtime inspection can be added incrementally.

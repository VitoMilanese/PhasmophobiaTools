# PhasmophobiaTools

A C# plugin project for experimenting with Phasmophobia through BepInEx 6 and Unity IL2CPP.

## Requirements

- Windows x64
- .NET 6 SDK
- Visual Studio 2022 or another compatible .NET IDE
- BepInEx 6 IL2CPP installed into the Phasmophobia game directory

Run Phasmophobia with BepInEx at least once before building this project. BepInEx generates the IL2CPP reference assemblies under:

```text
<Phasmophobia>/BepInEx/interop
```

Those generated assemblies are the correct compile-time references for Unity types in an IL2CPP game.

## Configure the game directory

If Phasmophobia is installed in the default Steam location, the project detects:

```text
C:\Program Files (x86)\Steam\steamapps\common\Phasmophobia
```

For another Steam library, set `PHASMOPHOBIA_GAME_DIR` before building:

```powershell
$env:PHASMOPHOBIA_GAME_DIR = "D:\SteamLibrary\steamapps\common\Phasmophobia"
dotnet build PhasmophobiaTools.sln
```

You can also pass the generated interop directory directly:

```powershell
dotnet build PhasmophobiaTools.sln -p:PhasmophobiaInteropDir="D:\SteamLibrary\steamapps\common\Phasmophobia\BepInEx\interop"
```

If you set `PHASMOPHOBIA_GAME_DIR` as a persistent Windows environment variable, restart Visual Studio so it can see the new value.

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
Press F8 to toggle the debug overlay.
```

## Debug overlay

Press `F8` to open or close the in-game debug overlay.

The overlay currently shows:

- the Unity version
- the active Unity scene
- a refreshable hierarchy of scene objects
- active/inactive state for each object
- a text filter for object names

The overlay temporarily unlocks and shows the mouse cursor while it is open, then restores the previous cursor state when it closes.

To avoid accidentally freezing the game UI on unusually large scenes, the scene inspector displays at most 5,000 objects per refresh.

## Project structure

```text
PhasmophobiaTools.sln
src/
  PhasmophobiaTools/
    DebugOverlayBehaviour.cs
    ImguiBridge.cs
    PhasmophobiaTools.csproj
    Plugin.cs
```

## Notes

The project intentionally does not reference Phasmophobia's generated `Assembly-CSharp.dll` yet. The current functionality only uses Unity APIs, so game-specific interop references can be added when the first Phasmophobia-specific hook is implemented.

Unity IMGUI is resolved from the game's generated IL2CPP interop assemblies at runtime. This avoids mixing BepInEx IL2CPP Unity types with regular managed Unity assemblies.

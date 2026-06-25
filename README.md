# RhinoToMinecraft

A Grasshopper plugin that voxelizes Rhino geometry, maps it to Minecraft blocks, and exports it for import into Minecraft.

## What it does

- Voxelizes a closed solid Brep into a voxel grid (Bounding Box + Point-in-Mesh containment test)
- Extracts a representative color per object/layer (flat display color, or material texture average)
- Maps colors to Minecraft Block IDs manually, or via a curated dropdown list
- Resolves overlapping voxels across multiple objects (later object in the input order wins)
- Exports the result as `/setblock` commands in a `.mcfunction` file, either to an arbitrary path or auto-deployed straight into a Minecraft world's datapack folder

## Requirements

- Rhino 8 + Grasshopper
- .NET 7 SDK (`net7.0-windows`)
- Visual Studio 2022, or the `dotnet` CLI

## Project structure

```
src/
  RhinoToMinecraft/          GHA plugin project
    Core/                    Pure C# geometry/data logic, decoupled from GH/Rhino UI where possible
    Components/              Grasshopper components (GH_Component / GH_ValueList)
  RhinoToMinecraft.Tests/     Standalone console test harness for the Core logic
```

## Build

```
dotnet build src/RhinoToMinecraft/RhinoToMinecraft.csproj
```

This produces a `.gha` and, via a post-build step, copies it into `%APPDATA%\Grasshopper\Libraries` so Grasshopper picks it up on next launch.

To debug interactively in Visual Studio, set the project's debug target to launch `Rhino.exe` directly (stored locally in `RhinoToMinecraft.csproj.user`, not committed).

## Components

- **Voxelize** — closed solid Brep to voxel centers; outputs `Voxels` (Minecraft block space, for the pipeline) and `ModelVoxels` (Rhino model space, for Custom Preview)
- **Extract Color** — representative color from a single referenced object's display color or material texture
- **Get Layer Objects** — geometry + representative color for an entire layer at once
- **Pick Block** — dropdown of curated, building-friendly Minecraft Block IDs
- **Resolve Overlaps** — deduplicates voxel positions claimed by multiple objects, later object wins
- **Export McFunction** — writes `/setblock` commands to a given file path
- **Export McFunction To World** — writes them directly into a world save's datapack folder, auto-creating `pack.mcmeta` and the `data/<namespace>/functions/` structure

## Testing

`src/RhinoToMinecraft.Tests` is a console harness exercising the `Core` classes (`VoxelizationEngine`, `VoxelOverlapResolver`, `McFunctionExporter`). It depends on RhinoCommon's native geometry kernel, which only initializes inside a running Rhino process, so it cannot run standalone outside one - `SelfTestComponent` in the GHA plugin exercises the same logic from inside Rhino for that reason.

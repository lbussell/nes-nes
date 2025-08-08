# nes-nes

This is a work-in-progress NES emulator written in .NET.

## Demo

https://github.com/user-attachments/assets/010d575f-9bbc-4de4-b5e7-5ea6ed7afef3

## Tech

- Built with .NET 10
- The emulator core has zero dependencies and is completely separate from the GUI
- Uses [Silk.NET](https://github.com/dotnet/Silk.NET) bindings for OpenGL, GLFW, and ImGui
- Fully [Native AOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/) compiled
  - You don't need the .NET Runtime installed to use the emulator
  - The final executable and all its dependencies are <5 MB (for win-x64)
 
## How to run it

### Prerequisites

- Install the .NET 10 SDK (https://dot.net)

### Build and run

To build and run the emulator:

```
pushd ./src/Gui/
dotnet run
```

### Publish (Native AOT)

Run this command to publish the project as a Native AOT app:

```
dotnet publish -c Release
```

The final executable will be located in the directory `src/Gui/bin/Release/net10.0/<your RID>/publish/`.

## What's missing

- [ ] Background scrolling
- [ ] Sprite 0 collision
- [ ] Cartridge mappers other than NROM
- [ ] 8x16 sprite mode support
- [ ] Audio
- [ ] Tons of other stuff! It's a work-in-progress.

## Attribution

- 6502 JSON Test Data is from https://github.com/SingleStepTests/65x02.
- nestest.nes by Kevin Horton

## Other stuff

### Useful resources

- **NES**
  - [Cycle reference chart](https://www.nesdev.org/wiki/Cycle_reference_chart)
- **CPU**
  - [Detailed 6502 instruction timing](https://www.nesdev.org/6502_cpu.txt)
- **PPU**
  - [Nametables](https://www.nesdev.org/wiki/PPU_nametables)
  - [Pattern tables](https://www.nesdev.org/wiki/PPU_pattern_tables)
  - [Attribute tables](https://www.nesdev.org/wiki/PPU_attribute_tables)
  - [Object attribute memory (OAM)](https://www.nesdev.org/wiki/PPU_OAM)
  - [Palettes](https://www.nesdev.org/wiki/PPU_palettes)
  - [Memory map](https://www.nesdev.org/wiki/PPU_memory_map)
  - [NTSC frame timing diagram](https://www.nesdev.org/wiki/File:Ppu.svg)

### What does the name mean?

It doesn't really mean anything. It kind of reminds me of the song "Ner Ner" by Guthrie Govan.

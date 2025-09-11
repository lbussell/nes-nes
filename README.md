# nes-nes

This is a work-in-progress NES emulator written in .NET.

<img width="1538" height="1079" alt="image" src="https://github.com/user-attachments/assets/4eb00a8c-06df-4e00-9d44-0b1664fcae41" />
    
> (above) _Full diagnostic mode_

## Screenshots

<img width="545" height="527" alt="image" src="https://github.com/user-attachments/assets/c32c684c-8ed1-4ae6-8db6-1579b93cccaf" />
<img width="533" height="523" alt="image" src="https://github.com/user-attachments/assets/017db7da-8408-473e-81ac-59fdfc73f217" />
<img width="533" height="523" alt="image" src="https://github.com/user-attachments/assets/d266c58a-de36-41bb-a331-b138bdc85a86" />


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
dotnet run -- --rom /path/to/your/rom.nes
```

### Publish (Native AOT)

Run this command to publish the project as a Native AOT app:

```
dotnet publish -c Release
```

The final executable will be located in the directory `src/Gui/bin/Release/net10.0/<your RID>/publish/`.

## What's missing

- [x] Input of any kind
- [x] Background scrolling
- [x] Sprite 0 collision
- [ ] Cartridge mappers other than NROM
- [ ] 8x16 sprite mode support
- [ ] Audio
- [ ] Tons of other stuff! It's a work-in-progress.

## Attribution

- 6502 JSON Test Data is from https://github.com/SingleStepTests/65x02.
- nestest.nes by Kevin Horton
- "Smooth (FBX)" color palette by firebrandx https://www.firebrandx.com/nespalette.html

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

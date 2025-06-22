// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

// Usage: dotnet run PaletteToCode.cs -- path/to/palette.pal

using System.Text;

var paletteData = File.ReadAllBytes(args[0]);
Console.WriteLine("Palette data length: " + paletteData.Length);

var code = new StringBuilder();

code.AppendLine("[");

for (int i = 0; i < paletteData.Length; i += 3)
{
    if (i + 2 >= paletteData.Length)
    {
        Console.WriteLine("Incomplete color data at index " + i);
        break;
    }

    byte r = paletteData[i];
    byte g = paletteData[i + 1];
    byte b = paletteData[i + 2];
    code.AppendLine($"    new Color(0x{r:X2}, 0x{g:X2}, 0x{b:X2}),");
}

code.AppendLine("]");

Console.WriteLine("Generated code:");
Console.WriteLine(code.ToString());

// SPDX-FileCopyrightText: Copyright (c) 2025 Logan Bussell
// SPDX-License-Identifier: MIT

// Usage: dotnet run CompareLogs.cs -- path/to/expected.txt path/to/actual.txt
// Returns 0 if the files match, 1 if they do not.
// Tries to have some useful logging output.

var expectedFile = args[0];
var actualFile = args[1];

var expectedLines = await File.ReadAllLinesAsync(expectedFile);
var actualLines = await File.ReadAllLinesAsync(actualFile);

if (expectedLines.Length != actualLines.Length)
{
    Console.WriteLine(
        $"Warning: {Path.GetFileName(expectedFile)} has {expectedLines.Length} lines, while"
        + $" {Path.GetFileName(actualFile)} has {actualLines.Length} lines."
    );
}

var comparisonLines = expectedLines
    .Zip(actualLines)
    .Select((pair, index) =>
        (index, expected: pair.First, actual: pair.Second)
    );

foreach (var (index, expected, actual) in comparisonLines)
{
    // We know that our log format is exactly 68 characters long. By cutting
    // off the expected log here, we can add extra info to the output that can
    // help us debug.
    var expectedComparison = expected[..68];
    if (expectedComparison.Trim() != actual.Trim())
    {
        Console.WriteLine(
            $"""
            Mismatch at line {index}:
            Expected: {expected}
            Actual:   {actual}
            """
        );
        return 1;
    }

    Console.WriteLine($"Line {index} OK: {expected}");
}

return 0;

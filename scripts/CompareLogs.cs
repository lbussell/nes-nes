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
        $"{Environment.NewLine}Warning: {Path.GetFileName(expectedFile)} has {expectedLines.Length} lines, while"
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
    var expectedComparison = expected[..69];
    if (Differs(expectedComparison.Trim(), actual.Trim(), out var diffIndex))
    {
        var diffString = new string(' ', diffIndex - 1);

        var previousLines = expectedLines[Math.Max(0, index - 5)..index];
        Console.WriteLine(
            $"""

            ...
            {string.Join(Environment.NewLine, previousLines)}

            """);

        Console.WriteLine(
            $"""
            Mismatch at line {index}, character {diffIndex + 1}:
                        {diffString}v
              Expected: {expected}
              Actual:   {actual}
                        {diffString}^
            """
        );

        return 1;
    }
}

return 0;


// Return the zero-based index of the first difference between two strings.
static bool Differs(string expected, string actual, out int index)
{
    var expectedSpan = expected.AsSpan();
    var actualSpan = actual.AsSpan();

    if (expectedSpan.Length != actualSpan.Length)
    {
        index = Math.Min(expectedSpan.Length, actualSpan.Length) + 1;
        return true;
    }

    for (int i = 0; i < Math.Min(expectedSpan.Length, actualSpan.Length); i++)
    {
        if (expectedSpan[i] != actualSpan[i])
        {
            index = i + 1;
            return true;
        }
    }

    // No differences found
    index = -1;
    return false;
}

using System.Diagnostics;
using System.Globalization;

string GetSizeValueAsString(string line, int index)
{
    var startIndex = -1;

    for (int i = 0; i < 5; i++)
    {
        var charIndex = index - i;
        if (charIndex < 0) break;
        if (line[charIndex] == ' ' || line[charIndex] == ':')
        {
            startIndex = charIndex;
            break;
        }
    }

    if (startIndex == -1)
        throw new InvalidOperationException($"Failed to extract value from\n{line}\nbefore position {index}");

    return line[(startIndex + 1)..(index + 2)];
}

string ReplaceWithEm(string line)
{
    var index = line.IndexOf("px", StringComparison.InvariantCulture);
    while (index > 0)
    {
        var isReplaceDone = false;

        try
        {
            // Will include "px" at the end
            var sizeValueAsString = GetSizeValueAsString(line, index);

            if (double.TryParse(sizeValueAsString[..^2], out var sizeValue) == false)
                throw new FormatException($"Failed to parse value from {sizeValueAsString}");

            var newValue = Math.Round(sizeValue / 14.0, 2, MidpointRounding.ToZero);

            var newValueAsString = $"{newValue:0.00}em";

            line = line.Replace(sizeValueAsString, newValueAsString);

            isReplaceDone = true;
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
        }

        index = isReplaceDone
            ? line.IndexOf("px", StringComparison.InvariantCulture)
            : line.IndexOf("px", index + 1, StringComparison.InvariantCulture);
    }

    return line;
}

string ReplaceWithRem(string line)
{
    var columnIndex = line.IndexOf(":", StringComparison.InvariantCulture);
    if (columnIndex == -1) return line;

    var uomIndex = line.IndexOf("px", StringComparison.InvariantCulture);
    if (uomIndex == -1) return line;

    var value = line[(columnIndex + 1)..uomIndex];

    if (double.TryParse(value, out var fontSize) == false) return line;

    fontSize = Math.Round(fontSize / 14.0, 2, MidpointRounding.ToZero);
    var newValue = $" {fontSize:0.00}rem;";

    var newLine = line.Replace($"{value}px;", newValue);

    return newLine;
}

string FixLine(string line)
{
    if (line.Trim().StartsWith("font-size"))
    {
        return ReplaceWithRem(line);
    }

    // use em

    line = ReplaceWithEm(line);

    return line;
}

if (args.Length < 1)
{
    Console.WriteLine("Please provide a *.css full filename as command-line parameter!");
    return;
}

var fullFilename = args[0];

if (File.Exists(fullFilename) == false)
{
    Console.WriteLine("*.css file NOT found!");
    return;
}

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var cssLines = File.ReadAllLines(fullFilename);

var result = cssLines.Select(FixLine).ToList();

var outputFilename = Path.ChangeExtension(fullFilename, string.Empty).TrimEnd('.');
outputFilename += "_fixed.css";

File.WriteAllLines(outputFilename, result);

Process.Start("notepad.exe", outputFilename);


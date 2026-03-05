using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TerritoryExpansionGame.Logic;

namespace TerritoryExpansionGame.Data;

public static class SaveFileService
{
    public static void Save(string path, GameState gameState)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Save path cannot be empty.", nameof(path));
        }

        ArgumentNullException.ThrowIfNull(gameState);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dimensionsLine = $"{gameState.Height} {gameState.Width}";
        var valuesLine = string.Join(' ', gameState.EnumerateFlattenedBoard());

        File.WriteAllLines(fullPath, new[] { dimensionsLine, valuesLine });
    }

    public static GameState Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Save path cannot be empty.", nameof(path));
        }

        var fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Save file was not found.", fullPath);
        }

        var lines = File.ReadAllLines(fullPath);

        if (lines.Length < 2)
        {
            throw new FormatException("Save file must contain at least two lines.");
        }

        var dimensions = ParseLineToIntegers(lines[0]);

        if (dimensions.Count != 2)
        {
            throw new FormatException("First line must contain exactly two integers: height and width.");
        }

        var height = dimensions[0];
        var width = dimensions[1];
        var flattenedValues = ParseLineToIntegers(string.Join(' ', lines.Skip(1)));

        return GameState.FromFlattenedBoard(height, width, flattenedValues);
    }

    private static List<int> ParseLineToIntegers(string input)
    {
        var tokens = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var values = new List<int>(tokens.Length);

        foreach (var token in tokens)
        {
            if (!int.TryParse(token, out var parsedValue))
            {
                throw new FormatException($"Invalid integer token '{token}'.");
            }

            values.Add(parsedValue);
        }

        return values;
    }
}

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Plugin.GenreRows;

/// <summary>
/// The kinds of rows the plugin makes, and the item types each one contains.
/// </summary>
internal static class RowKinds
{
    public static readonly BaseItemKind[] Movies = { BaseItemKind.Movie };
    public static readonly BaseItemKind[] Shows = { BaseItemKind.Series };
    public static readonly BaseItemKind[] All = { BaseItemKind.Movie, BaseItemKind.Series };

    /// <summary>
    /// Gets the rows to make for each genre in a given mode: (key, item types, title suffix).
    /// </summary>
    /// <param name="mode">The RowMode setting.</param>
    /// <returns>The row kinds.</returns>
    public static IReadOnlyList<(string Key, BaseItemKind[] Kinds, string Suffix)> ForMode(string? mode)
    {
        return mode switch
        {
            "Combined" => new[] { ("All", All, string.Empty) },
            "MoviesOnly" => new[] { ("Movie", Movies, " Movies") },
            "ShowsOnly" => new[] { ("Series", Shows, " Shows") },
            _ => new[] { ("Movie", Movies, " Movies"), ("Series", Shows, " Shows") },
        };
    }

    public static bool TryParse(string key, out BaseItemKind[] kinds)
    {
        kinds = key switch
        {
            "Movie" => Movies,
            "Series" => Shows,
            "All" => All,
            _ => Array.Empty<BaseItemKind>(),
        };
        return kinds.Length > 0;
    }
}

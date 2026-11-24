using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.GenreRows.Configuration;

/// <summary>
/// Settings for the Genre Rows plugin. Edited from Dashboard → Plugins → Genre Rows.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets how rows are built.
    /// "Separate" = one row per genre per type ("Comedy Movies", "Comedy Shows").
    /// "Combined" = one row per genre mixing movies and shows.
    /// "MoviesOnly" / "ShowsOnly" = only that type.
    /// </summary>
    public string RowMode { get; set; } = "Separate";

    /// <summary>
    /// Gets or sets a comma-separated list of genres to create rows for.
    /// Leave empty to use the biggest genres automatically.
    /// </summary>
    public string IncludedGenres { get; set; } = string.Empty;

    /// <summary>Gets or sets a comma-separated list of genres that never get a row.</summary>
    public string ExcludedGenres { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of genres used when picking automatically.</summary>
    public int MaxGenres { get; set; } = 10;

    /// <summary>Gets or sets the minimum number of items a genre needs (per type) to get a row.</summary>
    public int MinItemsPerRow { get; set; } = 5;

    /// <summary>Gets or sets the number of items shown in each row.</summary>
    public int ItemsPerRow { get; set; } = 20;

    /// <summary>Gets or sets the item order inside a row: Random, DateCreated, CommunityRating, PremiereDate or SortName.</summary>
    public string SortBy { get; set; } = "Random";

    /// <summary>Gets or sets a value indicating whether watched items are hidden.</summary>
    public bool HideWatched { get; set; }
}

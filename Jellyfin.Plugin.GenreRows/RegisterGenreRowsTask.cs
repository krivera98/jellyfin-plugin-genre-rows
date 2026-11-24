using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.GenreRows.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.GenreRows;

/// <summary>
/// Works out which genre rows to offer and registers them with Home Screen Sections.
/// Runs at startup, every day, and whenever the plugin settings are saved.
/// Shown in Dashboard → Scheduled Tasks, where it can also be run by hand after adding media.
/// </summary>
public class RegisterGenreRowsTask : IScheduledTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<RegisterGenreRowsTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterGenreRowsTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="logger">Logger.</param>
    public RegisterGenreRowsTask(ILibraryManager libraryManager, ILogger<RegisterGenreRowsTask> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Register genre rows";

    /// <inheritdoc />
    public string Key => "GenreRowsRegister";

    /// <inheritdoc />
    public string Description => "Adds a Home Screen Sections row for each genre chosen in the Genre Rows settings.";

    /// <inheritdoc />
    public string Category => "Genre Rows";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.StartupTrigger };
        yield return new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromHours(24).Ticks };
    }

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var register = HomeScreenSectionsBridge.FindRegisterMethod();
        if (register is null)
        {
            _logger.LogWarning("Genre Rows: the Home Screen Sections plugin is not installed or not loaded, so no rows were added.");
            return Task.CompletedTask;
        }

        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var rows = PlanRows(config);

        for (var i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (id, title, data) = rows[i];
            HomeScreenSectionsBridge.Register(register, id, title, data);
            progress.Report(100.0 * (i + 1) / rows.Count);
        }

        _logger.LogInformation(
            "Genre Rows: registered {Count} rows with Home Screen Sections: {Rows}",
            rows.Count,
            string.Join(", ", rows.Select(r => r.Title)));
        return Task.CompletedTask;
    }

    private static List<string> ParseList(string? text)
    {
        return (text ?? string.Empty)
            .Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    /// <summary>
    /// Returns the rows to register: (section id, title, data for <see cref="GenreRowResults"/>).
    /// </summary>
    private List<(string Id, string Title, string Data)> PlanRows(PluginConfiguration config)
    {
        var rowKinds = RowKinds.ForMode(config.RowMode);
        var minItems = Math.Max(1, config.MinItemsPerRow);
        var excluded = new HashSet<string>(ParseList(config.ExcludedGenres), StringComparer.OrdinalIgnoreCase);
        var included = ParseList(config.IncludedGenres);

        // How many items of each row kind every genre has, across all libraries.
        var counts = new Dictionary<string, (Guid Id, string Name, Dictionary<string, int> PerKind)>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, kinds, _) in rowKinds)
        {
            var genres = _libraryManager.GetGenres(new InternalItemsQuery
            {
                IncludeItemTypes = kinds,
                Recursive = true,
            });

            foreach (var (item, itemCounts) in genres.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    continue;
                }

                if (!counts.TryGetValue(item.Name, out var entry))
                {
                    entry = (item.Id, item.Name, new Dictionary<string, int>());
                    counts[item.Name] = entry;
                }

                entry.PerKind[key] = key switch
                {
                    "Movie" => itemCounts.MovieCount,
                    "Series" => itemCounts.SeriesCount,
                    _ => itemCounts.MovieCount + itemCounts.SeriesCount,
                };
            }
        }

        IEnumerable<(Guid Id, string Name, Dictionary<string, int> PerKind)> ordered;
        if (included.Count > 0)
        {
            ordered = included
                .Where(name => counts.ContainsKey(name))
                .Select(name => counts[name]);
        }
        else
        {
            ordered = counts.Values
                .OrderByDescending(g => g.PerKind.Values.Sum())
                .ThenBy(g => g.Name, StringComparer.OrdinalIgnoreCase);
        }

        var maxGenres = included.Count > 0 ? int.MaxValue : Math.Max(1, config.MaxGenres);
        var rows = new List<(string, string, string)>();
        var genresUsed = 0;

        foreach (var genre in ordered)
        {
            if (genresUsed >= maxGenres)
            {
                break;
            }

            if (excluded.Contains(genre.Name))
            {
                continue;
            }

            var added = false;
            foreach (var (key, _, suffix) in rowKinds)
            {
                if (genre.PerKind.GetValueOrDefault(key) < minItems)
                {
                    continue;
                }

                // The id must be stable (Home Screen Sections saves per-row settings under it) and URL safe.
                rows.Add(($"GenreRows-{key}-{genre.Id:N}", genre.Name + suffix, key + "|" + genre.Name));
                added = true;
            }

            if (added)
            {
                genresUsed++;
            }
        }

        return rows;
    }
}

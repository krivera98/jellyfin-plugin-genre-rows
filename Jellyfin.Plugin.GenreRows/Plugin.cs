using System;
using System.Collections.Generic;
using Jellyfin.Plugin.GenreRows.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.GenreRows;

/// <summary>
/// Adds one Home Screen Sections section per genre ("Comedy Movies", "Drama Shows", ...).
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Server paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    /// <param name="taskManager">Scheduled task manager.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ITaskManager taskManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _taskManager = taskManager;
    }

    /// <summary>Gets the running plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Genre Rows";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("620eedb4-ad26-47d7-a878-f42a41da0db2");

    /// <inheritdoc />
    public override string Description => "Adds a Home Screen Sections row for each genre of movies and shows.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html",
            },
        };
    }

    /// <inheritdoc />
    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        base.UpdateConfiguration(configuration);

        // Register rows for newly chosen genres straight away.
        _taskManager.QueueScheduledTask<RegisterGenreRowsTask>();
    }
}

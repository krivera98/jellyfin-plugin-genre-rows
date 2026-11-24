using System;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.GenreRows.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace Jellyfin.Plugin.GenreRows;

/// <summary>
/// What Home Screen Sections sends when it asks for a row's items.
/// </summary>
public class GenreRowPayload
{
    /// <summary>Gets or sets the user the home screen is for.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the data given at registration: "Movie|Comedy", "Series|Comedy" or "All|Comedy".</summary>
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Returns the items for one genre row. Created by Home Screen Sections each time a row loads.
/// </summary>
public class GenreRowResults
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IDtoService _dtoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenreRowResults"/> class.
    /// </summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="dtoService">DTO service.</param>
    public GenreRowResults(ILibraryManager libraryManager, IUserManager userManager, IDtoService dtoService)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
        _dtoService = dtoService;
    }

    /// <summary>
    /// Gets the items for a row, using the user's own library access and parental controls.
    /// </summary>
    /// <param name="payload">Which user and which genre row.</param>
    /// <returns>The items.</returns>
    public QueryResult<BaseItemDto> GetResults(GenreRowPayload payload)
    {
        var config = Plugin.Instance?.Configuration ?? new PluginConfiguration();
        var user = _userManager.GetUserById(payload.UserId);
        var parts = (payload.AdditionalData ?? string.Empty).Split('|', 2);
        if (user is null || parts.Length != 2 || !RowKinds.TryParse(parts[0], out var kinds))
        {
            return new QueryResult<BaseItemDto>();
        }

        var dtoOptions = new DtoOptions
        {
            Fields = new[] { ItemFields.PrimaryImageAspectRatio, ItemFields.MediaSourceCount },
            ImageTypeLimit = 1,
        };

        // Same path the normal /Items API uses: start from the user's root folder
        // so only libraries this user can see are searched.
        var result = _libraryManager.GetUserRootFolder().GetItems(new InternalItemsQuery(user)
        {
            IncludeItemTypes = kinds,
            Genres = new[] { parts[1] },
            Recursive = true,
            Limit = Math.Clamp(config.ItemsPerRow, 1, 100),
            OrderBy = GetOrder(config.SortBy),
            IsPlayed = config.HideWatched ? false : null,
            DtoOptions = dtoOptions,
        });

        var dtos = _dtoService.GetBaseItemDtos(result.Items.ToList(), dtoOptions, user);
        return new QueryResult<BaseItemDto>(dtos);
    }

    private static (ItemSortBy OrderBy, SortOrder SortOrder)[] GetOrder(string? sortBy)
    {
        return sortBy switch
        {
            "DateCreated" => new[] { (ItemSortBy.DateCreated, SortOrder.Descending) },
            "CommunityRating" => new[] { (ItemSortBy.CommunityRating, SortOrder.Descending), (ItemSortBy.SortName, SortOrder.Ascending) },
            "PremiereDate" => new[] { (ItemSortBy.PremiereDate, SortOrder.Descending) },
            "SortName" => new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
            _ => new[] { (ItemSortBy.Random, SortOrder.Ascending) },
        };
    }
}

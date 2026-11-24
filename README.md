# Genre Rows: a Jellyfin plugin

Adds rows of **movies and shows grouped by genre** to the Jellyfin home screen:
"Action Movies", "Comedy Movies", "Comedy Shows", "Crime Shows", and so on.

It works through the **[Home Screen Sections](https://github.com/IAmParadox27/jellyfin-plugin-home-sections)** plugin.
Every genre row becomes a normal Home Screen Sections section, so you turn rows on or off and order them
alongside *Continue Watching*, *Next Up*, *Because You Watched* and the rest.

Built for **Jellyfin 12.4**.

> Home Screen Sections has its own "Genre" section, but it shows **movies only** and picks
> **random genres** based on what each user watches. Genre Rows gives you a fixed row for each genre
> you choose, for movies, shows, or both.

## Requirements

- Jellyfin 12.4
- Home Screen Sections, plus the plugins it needs: File Transformation and Plugin Pages.
  These come from `https://www.iamparadox.dev/jellyfin/plugins/manifest.json`.

## Install from a plugin repository

1. In Jellyfin, go to **Dashboard → Plugins → Repositories** (in the *Catalog* area) and click **+**.
   - Name: `Genre Rows`
   - URL: `https://raw.githubusercontent.com/krivera98/jellyfin-plugin-genre-rows/main/manifest.json`
2. Go to **Catalog**, find **Genre Rows** under *General*, and click **Install**.
3. **Restart Jellyfin.**
4. Open **Dashboard → Plugins → Genre Rows**, choose your genres, and click **Save**.
5. Open the **Home Screen Sections** settings. The new rows appear in the section list, each named after its genre,
   such as "Comedy Movies". Turn on the ones you want, set their order, and save.
   For tall movie posters, set each row's view mode to *Portrait*.
6. Reload the web page with Ctrl+F5.

Jellyfin checks the repository for new versions. They appear under *Updates*, and Jellyfin installs them
by itself if automatic updates are on.

## If the rows don't appear on the home screen

- **User Override is ticked.** In the Home Screen Sections settings, a row with *User Override* ticked
  only appears for users who turned it on in their own **Modular Home** settings (in the user menu).
  Users who saved their own list before the genre rows existed won't get the new rows.
  Untick *User Override* to show the rows to everyone, or let each user turn them on themselves.
- **The home screen is cached.** Home Screen Sections keeps each user's layout in server memory for up to an hour,
  so changes may not show straight away. Restart Jellyfin, then reload the page with Ctrl+F5.
- **Rows with the same Order number** are grouped and shuffled on every visit.
  Give each row its own number for a fixed order.

## Settings

| Setting | What it does |
|---|---|
| Rows | Separate movie and show rows, one mixed row per genre, movies only, or shows only. |
| Genres to make rows for | A list such as `Action, Comedy, Animation`. Leave empty to use your biggest genres automatically. |
| Genres to skip | Genres that never get a row. |
| Maximum number of genres | The limit for automatic mode. |
| Skip a genre if it has fewer than… | Leaves out tiny genres. This is checked separately for movies and shows. |
| Items per row / Order / Hide watched | How each row is filled. Random order changes on every visit. |

Each user only sees the movies and shows their library access and parental controls allow.

The rows are rebuilt at startup, once a day, and every time you save the settings.
After adding new media, you can also run **Dashboard → Scheduled Tasks → Genre Rows → Register genre rows**.
Rows for genres you remove stay in the Home Screen Sections list until Jellyfin restarts.

## Publishing a new version

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), `zip` and `python3`.

```
./build-release.sh 1.0.1.0 "What changed"
```

This builds the plugin and writes `releases/genre-rows_1.0.1.0.zip`. It then adds that version to
`manifest.json`, with the MD5 checksum Jellyfin checks before installing. Commit and push both files to `main`.
Jellyfin downloads them without logging in, so this repository must stay public.

## How it works

| File | Purpose |
|---|---|
| `RegisterGenreRowsTask.cs` | A scheduled task. It counts the movies and shows in each genre, picks the genres from your settings, and registers one section per row with Home Screen Sections. |
| `HomeScreenSectionsBridge.cs` | Calls Home Screen Sections' public `PluginInterface.RegisterSection` method, as its README describes for third-party plugins. |
| `GenreRowResults.cs` | Called by Home Screen Sections when a row loads. It returns that user's movies and shows for the genre. |
| `Configuration/` | The settings and the settings page. |

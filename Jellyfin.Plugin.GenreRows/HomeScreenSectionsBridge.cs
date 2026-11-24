using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

namespace Jellyfin.Plugin.GenreRows;

/// <summary>
/// Talks to the Home Screen Sections plugin.
/// Jellyfin loads each plugin in its own load context, so it can't be referenced directly.
/// Instead we call its public <c>PluginInterface.RegisterSection</c> method through reflection,
/// as described in the Home Screen Sections README.
/// </summary>
internal static class HomeScreenSectionsBridge
{
    private const string PluginInterfaceType = "Jellyfin.Plugin.HomeScreenSections.PluginInterface";

    /// <summary>
    /// Finds Home Screen Sections' <c>RegisterSection</c> method, or null if that plugin isn't loaded.
    /// </summary>
    /// <returns>The method, or null.</returns>
    public static MethodInfo? FindRegisterMethod()
    {
        var assembly = AssemblyLoadContext.All
            .SelectMany(context => context.Assemblies)
            .FirstOrDefault(a => a.FullName?.Contains(".HomeScreenSections", StringComparison.Ordinal) ?? false);

        return assembly?.GetType(PluginInterfaceType)?.GetMethod("RegisterSection");
    }

    /// <summary>
    /// Registers one section. Registering the same id again replaces it.
    /// </summary>
    /// <param name="registerMethod">The method from <see cref="FindRegisterMethod"/>.</param>
    /// <param name="id">Unique section id.</param>
    /// <param name="displayText">Row title.</param>
    /// <param name="additionalData">Data passed back to <see cref="GenreRowResults.GetResults"/>.</param>
    public static void Register(MethodInfo registerMethod, string id, string displayText, string additionalData)
    {
        var payload = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["displayText"] = displayText,
            ["limit"] = 1,
            ["additionalData"] = additionalData,
            ["resultsAssembly"] = typeof(GenreRowResults).Assembly.FullName,
            ["resultsClass"] = typeof(GenreRowResults).FullName,
            ["resultsMethod"] = nameof(GenreRowResults.GetResults),
        };

        // RegisterSection takes a Newtonsoft JObject from Home Screen Sections' own copy of Newtonsoft.Json.
        // Build it with that exact type (JObject.Parse) so the types match.
        var jObjectType = registerMethod.GetParameters()[0].ParameterType;
        var parse = jObjectType.GetMethod("Parse", new[] { typeof(string) })
            ?? throw new InvalidOperationException("Home Screen Sections: JObject.Parse not found.");
        var jObject = parse.Invoke(null, new object[] { JsonSerializer.Serialize(payload) });

        registerMethod.Invoke(null, new[] { jObject });
    }
}

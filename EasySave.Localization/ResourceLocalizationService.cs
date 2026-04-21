using System.Globalization;
using System.Resources;

namespace EasySave.Localization;

public class ResourceLocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly CultureInfo _culture;

    public ResourceLocalizationService(string culture)
    {
        _resourceManager = new ResourceManager(
            "EasySave.Localization.Resources.Strings",
            typeof(ResourceLocalizationService).Assembly);

        _culture = new CultureInfo(culture);
    }

    public string Get(string key)
    {
        var value = _resourceManager.GetString(key, _culture);
        return value ?? key;
    }
}
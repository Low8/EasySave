using System.Globalization;
using System.Resources;

namespace EasySave.Localization;

public class LocalizationService
{
    private readonly ResourceManager _resources = new("EasySave.Localization.Resources.Messages", typeof(LocalizationService).Assembly);

    public string Get(string key) =>
        _resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    public string Get(string key, params object[] args) =>
        string.Format(Get(key), args);
}

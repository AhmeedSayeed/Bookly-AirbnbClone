using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace PL.Extensions;

public static class EnumLocalizationExtensions
{
    public static List<SelectListItem> GetLocalizedEnumSelectList<TEnum>(
        this IHtmlHelper html,
        IStringLocalizer localizer)
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(value => new SelectListItem
            {
                Value = value.ToString(),
                Text = localizer[value.ToString()]
            })
            .ToList();
    }
}
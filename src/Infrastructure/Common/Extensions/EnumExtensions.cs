using Retailer.Application.Common.Models;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace Retailer.Infrastructure.Common.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum enumValue)
    {
        object[] attr = enumValue.GetType().GetField(enumValue.ToString())!
            .GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attr.Length > 0)
            return ((DescriptionAttribute)attr[0]).Description;
        string result = enumValue.ToString();
        result = Regex.Replace(result, "([a-z])([A-Z])", "$1 $2");
        result = Regex.Replace(result, "([A-Za-z])([0-9])", "$1 $2");
        result = Regex.Replace(result, "([0-9])([A-Za-z])", "$1 $2");
        result = Regex.Replace(result, "(?<!^)(?<! )([A-Z][a-z])", " $1");
        return result;
    }

    public static List<string> GetDescriptionList(this Enum enumValue)
    {
        string result = enumValue.GetDescription();
        return result.Split(',').ToList();
    }

    public static LookupResponse? ToLookupResponse<TEnum>(this TEnum value)
       where TEnum : Enum?
    {
        if (value == null) return null;

        return new LookupResponse
        {
            Id = Convert.ToInt32(value),
            Name = value.ToString()
        };
    }

    public static string? GetObject<TEnum>(this TEnum value)
       where TEnum : Enum?
    {
        if (value == null) return null;
        return JsonConvert.SerializeObject(
           new LookupResponse
           {
               Id = Convert.ToInt32(value),
               Name = value.ToString()
           },
           new JsonSerializerSettings
           {
               ContractResolver = new CamelCasePropertyNamesContractResolver()
           });
    }

    public static TEnum GetEnumValue<TEnum>(this int value)
       where TEnum : Enum
       => (TEnum)Enum.Parse(typeof(TEnum), value.ToString(), true);
}
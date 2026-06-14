using Retailer.Infrastructure.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Retailer.Infrastructure.Common.Convertor;
public class StringEnumConverterExtension : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum; // Add anything additional here such as typeToConvert.IsEnumWithDescription() to check for description attributes.

    public override System.Text.Json.Serialization.JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (System.Text.Json.Serialization.JsonConverter)Activator.CreateInstance(typeof(CustomStringEnumConverter<>).MakeGenericType(typeToConvert))!;
}

public class CustomStringEnumConverter<T> : System.Text.Json.Serialization.JsonConverter<T>
        where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetInt32()!.GetEnumValue<T>()!;

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteRawValue(value.GetObject());
    }
}


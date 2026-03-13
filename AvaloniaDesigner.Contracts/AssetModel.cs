using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AvaloniaDesigner.Contracts;

public enum AssetType
{
    UserControl,
    Window,
    Styles,
    ResourceDictionary,
    CustomControl,
    Unknown
}

public sealed class RelativeSourceModel
{
    public string Mode { get; set; } = "FindAncestor";
    public string? AncestorType { get; set; }
    public int? AncestorLevel { get; set; }
    public string? Tree { get; set; }
}

[JsonConverter(typeof(PropertyModelConverter))]
public sealed class PropertyModel : IEquatable<PropertyModel>
{
    public string Type { get; set; } = string.Empty;
    public object? Value { get; set; }

    public string? BindingPath { get; set; }
    public string? BindingMode { get; set; }
    public string? BindingConverter { get; set; }
    public string? BindingStringFormat { get; set; }
    public string? BindingElementName { get; set; }
    public object? BindingFallbackValue { get; set; }
    public object? BindingTargetNullValue { get; set; }
    public object? BindingConverterParameter { get; set; }
    public RelativeSourceModel? BindingRelativeSource { get; set; }

    public string? ResourceKey { get; set; }
    public string? AssetPath { get; set; }
    public string? AssetAssembly { get; set; }

    public Dictionary<string, PropertyModel> Properties { get; set; } = new();
    public List<PropertyModel>? Items { get; set; }

    public bool Equals(PropertyModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return Type == other.Type &&
               BindingPath == other.BindingPath &&
               BindingMode == other.BindingMode &&
               ResourceKey == other.ResourceKey &&
               AssetPath == other.AssetPath &&
               AssetAssembly == other.AssetAssembly &&
               Equals(Value, other.Value);
    }

    public override bool Equals(object? obj) => Equals(obj as PropertyModel);

    public override int GetHashCode() => (Type, Value, BindingPath, AssetPath).GetHashCode();
}

public sealed class AssetModel : IEquatable<AssetModel>
{
    public AssetType AssetType { get; set; } = AssetType.UserControl;
    public Dictionary<string, PropertyModel> Properties { get; set; } = new();

    public bool Equals(AssetModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return AssetType == other.AssetType;
    }
}

public sealed class PropertyModelConverter : JsonConverter<PropertyModel>
{
    public override PropertyModel? ReadJson(
        JsonReader reader,
        Type objectType,
        PropertyModel? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonToken.StartArray)
        {
            var array = JArray.Load(reader);
            var result = new PropertyModel { Items = new List<PropertyModel>() };

            foreach (var item in array)
            {
                var child = item.ToObject<PropertyModel>(serializer);
                if (child != null)
                {
                    result.Items.Add(child);
                }
            }

            return result;
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            var result = new PropertyModel();

            if (obj.TryGetValue("$binding", out var bindingToken))
            {
                if (bindingToken.Type == JTokenType.String)
                {
                    result.BindingPath = bindingToken.ToString();
                }
                else if (bindingToken.Type == JTokenType.Object)
                {
                    result.BindingPath = bindingToken["Path"]?.ToString();
                }

                if (obj.TryGetValue("Mode", out var mode))
                {
                    result.BindingMode = mode.ToString();
                }

                if (obj.TryGetValue("Converter", out var converter))
                {
                    result.BindingConverter = converter.ToString();
                }

                if (obj.TryGetValue("StringFormat", out var stringFormat))
                {
                    result.BindingStringFormat = stringFormat.ToString();
                }

                if (obj.TryGetValue("ElementName", out var elementName))
                {
                    result.BindingElementName = elementName.ToString();
                }

                if (obj.TryGetValue("FallbackValue", out var fallbackValue))
                {
                    result.BindingFallbackValue = fallbackValue.ToObject<object>();
                }

                if (obj.TryGetValue("TargetNullValue", out var targetNullValue))
                {
                    result.BindingTargetNullValue = targetNullValue.ToObject<object>();
                }

                if (obj.TryGetValue("ConverterParameter", out var converterParameter))
                {
                    result.BindingConverterParameter = converterParameter.ToObject<object>();
                }

                if (obj.TryGetValue("RelativeSource", out var relativeSourceToken) &&
                    relativeSourceToken.Type == JTokenType.Object)
                {
                    result.BindingRelativeSource = relativeSourceToken.ToObject<RelativeSourceModel>(serializer);
                }

                return result;
            }

            if (obj.TryGetValue("$resource", out var resourceToken))
            {
                result.ResourceKey = resourceToken.ToString();
                return result;
            }

            if (obj.TryGetValue("$asset", out var assetToken))
            {
                if (assetToken.Type == JTokenType.String)
                {
                    result.AssetPath = assetToken.ToString();
                }
                else if (assetToken.Type == JTokenType.Object)
                {
                    result.AssetPath = assetToken["Path"]?.ToString();
                    result.AssetAssembly = assetToken["Assembly"]?.ToString();
                }

                return result;
            }

            if (obj.TryGetValue("Type", out var typeToken))
            {
                result.Type = typeToken.ToString();
                if (obj.TryGetValue("Properties", out var propertiesToken) && propertiesToken is JObject propertiesObject)
                {
                    foreach (var property in propertiesObject.Properties())
                    {
                        var child = property.Value.ToObject<PropertyModel>(serializer);
                        if (child != null)
                        {
                            result.Properties[property.Name] = child;
                        }
                    }
                }

                return result;
            }

            foreach (var property in obj.Properties())
            {
                var child = property.Value.ToObject<PropertyModel>(serializer);
                if (child != null)
                {
                    result.Properties[property.Name] = child;
                }
            }

            return result;
        }

        var token = JToken.Load(reader);
        return new PropertyModel { Value = token.ToObject<object?>(serializer) };
    }

    public override void WriteJson(JsonWriter writer, PropertyModel? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value.Items is { Count: > 0 })
        {
            writer.WriteStartArray();
            foreach (var item in value.Items)
            {
                serializer.Serialize(writer, item);
            }

            writer.WriteEndArray();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.BindingPath))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$binding");
            writer.WriteValue(value.BindingPath);

            WriteOptionalProperty(writer, serializer, "Mode", value.BindingMode);
            WriteOptionalProperty(writer, serializer, "Converter", value.BindingConverter);
            WriteOptionalProperty(writer, serializer, "StringFormat", value.BindingStringFormat);
            WriteOptionalProperty(writer, serializer, "ElementName", value.BindingElementName);
            WriteOptionalProperty(writer, serializer, "FallbackValue", value.BindingFallbackValue);
            WriteOptionalProperty(writer, serializer, "TargetNullValue", value.BindingTargetNullValue);
            WriteOptionalProperty(writer, serializer, "ConverterParameter", value.BindingConverterParameter);
            WriteOptionalProperty(writer, serializer, "RelativeSource", value.BindingRelativeSource);
            writer.WriteEndObject();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.ResourceKey))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$resource");
            writer.WriteValue(value.ResourceKey);
            writer.WriteEndObject();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.AssetPath))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("$asset");
            if (string.IsNullOrWhiteSpace(value.AssetAssembly))
            {
                writer.WriteValue(value.AssetPath);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Path");
                writer.WriteValue(value.AssetPath);
                writer.WritePropertyName("Assembly");
                writer.WriteValue(value.AssetAssembly);
                writer.WriteEndObject();
            }

            writer.WriteEndObject();
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.Type))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Type");
            writer.WriteValue(value.Type);
            writer.WritePropertyName("Properties");
            writer.WriteStartObject();
            foreach (var property in value.Properties)
            {
                writer.WritePropertyName(property.Key);
                serializer.Serialize(writer, property.Value);
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
            return;
        }

        if (value.Properties.Count > 0)
        {
            writer.WriteStartObject();
            foreach (var property in value.Properties)
            {
                writer.WritePropertyName(property.Key);
                serializer.Serialize(writer, property.Value);
            }

            writer.WriteEndObject();
            return;
        }

        serializer.Serialize(writer, value.Value);
    }

    private static void WriteOptionalProperty(JsonWriter writer, JsonSerializer serializer, string name, object? value)
    {
        if (value == null)
        {
            return;
        }

        writer.WritePropertyName(name);
        serializer.Serialize(writer, value);
    }
}

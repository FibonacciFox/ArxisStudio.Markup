using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AvaloniaDesigner.Generator.Models
{
    /// <summary>
    /// Вспомогательный класс для сбора информации о полях, 
    /// которые будут объявлены в генерируемом классе.
    /// </summary>
    public class ControlInfo
    {
        public string Type { get; set; } = "";
        public string? Name { get; set; } = "";
    }

    /// <summary>
    /// Класс для представления свойства (примитив, вложенный объект или коллекция).
    /// </summary>
    [JsonConverter(typeof(PropertyModelConverter))]
    public class PropertyModel
    {
        /// <summary>
        /// Полное имя типа, если это вложенный контрол (например, "Avalonia.Controls.Button").
        /// Для обычных примитивных свойств (Width, Text и т.п.) остаётся пустым.
        /// </summary>
        public string Type { get; set; } = ""; 
        
        /// <summary>
        /// Значение свойства (примитив, enum, строка).
        /// Например: 800, "Hello", true, 0.5.
        /// </summary>
        public object? Value { get; set; }
        
        /// <summary>
        /// Вложенные свойства объекта/контрола.
        /// Пример: Content.Properties["Text"], Properties["Background"], Properties["Children"] и т.д.
        /// </summary>
        public Dictionary<string, PropertyModel> Properties { get; set; } = new();

        /// <summary>
        /// Элементы коллекции (Children, Items и т.п.).
        /// Пример: Children: [ {Type: Button}, {Type: Button} ].
        /// </summary>
        public List<PropertyModel>? Items { get; set; }
    }
    
    /// <summary>
    /// Основная модель, представляющая UserControl/Window.
    /// </summary>
    public class AvaloniaModel
    {
        public string FormName { get; set; } = "GeneratedView";
        public string NamespaceSuffix { get; set; } = "Views";
        public string ParentClassType { get; set; } = ""; 
        
        /// <summary>
        /// Содержит свойства корневого элемента (Width, Height, Content и т.п.).
        /// Значения могут быть как примитивами (800, "Hello"), так и объектами с Type.
        /// </summary>
        public Dictionary<string, PropertyModel> Properties { get; set; } = new(); 
    }

    /// <summary>
    /// Конвертер для поддержки короткого JSON-формата:
    ///
    /// 1) Примитив:
    ///    "Width": 800
    ///    → PropertyModel { Value = 800 }
    ///
    /// 2) Коллекция:
    ///    "Children": [ {...}, {...} ]
    ///    → PropertyModel { Items = List<PropertyModel> }
    ///
    /// 3) Контрол:
    ///    "Content": {
    ///        "Type": "Avalonia.Controls.TextBlock",
    ///        "Properties": {
    ///            "Text": "Hello"
    ///        }
    ///    }
    ///    → PropertyModel { Type = "...", Properties = { "Text" => ... } }
    ///
    /// Свойства контролов ОБЯЗАТЕЛЬНО находятся в "Properties".
    /// Вариант с "Type" и свойствами рядом ("Text": "Hello") — не поддерживается.
    /// </summary>
    public class PropertyModelConverter : JsonConverter<PropertyModel>
    {
        public override PropertyModel? ReadJson(
            JsonReader reader,
            Type objectType,
            PropertyModel? existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            // 1. МАССИВ → КОЛЛЕКЦИЯ (Children: [ {...}, {...} ])
            if (reader.TokenType == JsonToken.StartArray)
            {
                var array = JArray.Load(reader);
                var result = new PropertyModel
                {
                    Items = new List<PropertyModel>()
                };

                foreach (var item in array)
                {
                    var child = item.ToObject<PropertyModel>(serializer);
                    if (child != null)
                        result.Items.Add(child);
                }

                return result;
            }

            // 2. ОБЪЕКТ
            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = JObject.Load(reader);
                var result = new PropertyModel();

                // 2.1. Контрол / сложный тип: ОБЯЗАТЕЛЬНО есть Type
                if (obj.TryGetValue("Type", out var typeToken))
                {
                    result.Type = typeToken.ToString();

                    // Свойства КОНТРОЛА берём ТОЛЬКО из блока Properties
                    if (obj.TryGetValue("Properties", out var propsToken) &&
                        propsToken.Type == JTokenType.Object)
                    {
                        var propsObj = (JObject)propsToken;
                        foreach (var p in propsObj.Properties())
                        {
                            var child = p.Value.ToObject<PropertyModel>(serializer);
                            if (child != null)
                                result.Properties[p.Name] = child;
                        }
                    }

                    // Если Properties нет — контрол без свойств
                    return result;
                }

                // 2.2. Обычный объект без Type
                foreach (var p in obj.Properties())
                {
                    var child = p.Value.ToObject<PropertyModel>(serializer);
                    if (child != null)
                        result.Properties[p.Name] = child;
                }

                return result;
            }

            // 3. ПРИМИТИВ (число, строка, bool и т.п.) → Value
            var token = JToken.Load(reader);
            return new PropertyModel
            {
                // конвертим сразу в object?, без JValue
                Value = token.ToObject<object?>(serializer)
            };
        }

        public override void WriteJson(JsonWriter writer, PropertyModel? value, JsonSerializer serializer)
        {
            throw new NotImplementedException("Обратная сериализация для генератора не требуется.");
        }
    }
}

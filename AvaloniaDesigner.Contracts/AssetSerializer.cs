using Newtonsoft.Json;
namespace AvaloniaDesigner.Contracts;

public static class AssetSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Converters = { new PropertyModelConverter() }
    };

    public static AssetModel? Deserialize(string json) =>
        JsonConvert.DeserializeObject<AssetModel>(json, Settings);

    public static string Serialize(AssetModel model) =>
        JsonConvert.SerializeObject(model, Formatting.Indented, Settings);
}

using Newtonsoft.Json;

namespace HuaJiBot.NET.Adapter.OneBot.Message.Entity;

internal class ImageMessageEntity(string url) : MessageEntity
{
    public ImageMessageEntity()
        : this("") { }

    [JsonProperty("file")]
    public string File { get; set; } = url;

    [JsonProperty("url")]
    public string Url { get; set; } = url;
}

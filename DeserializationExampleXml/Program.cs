using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.Serialization;

using Newtonsoft.Json;

using RestEase;

var cid = "H:1D670A1783307C2/D:WORK/D:1D71734AE4F4847/C:1D8EE93F67A4846";
var ssid = "69F7F786AC0926890B69F5369F65163F";
IOdaApi api = new RestClient("http://127.0.0.1:8080", async (request, cancellationToken) =>
{
    if (request?.Content is not null)
    {
        var content = (await request.Content.ReadAsStringAsync()) ?? string.Empty;
        await Console.Out.WriteLineAsync(content);
    }
})
{
    ResponseDeserializer = new XmlResponseDeserializer(),
    RequestBodySerializer = new XmlRequestBodySerializer()
}.For<IOdaApi>();
api.ApiToken = ssid;

IceCream IceCream = new() { Name = "MilkyWay", Price = 40 };

var saveResult = await api.Save(cid, IceCream);
var readAfterSave = await api.Read<IceCream>($"{cid}/O:{IceCream.Id}");
Console.WriteLine(readAfterSave);
IceCream.Price = 30;
saveResult = await api.Save(cid, IceCream);
readAfterSave = await api.Read<IceCream>($"{cid}/O:{IceCream.Id}");
Console.WriteLine(readAfterSave);

[BasePath("api")]
public interface IOdaApi
{
    [Path("ssid")]
    string ApiToken { get; set; }

    [Obsolete("Only for internal usage")]
    [Get("{path}?method=get_object&ssid={ssid}&format=xml")]
    internal Task<T> _Read<T>([Path] string path, string xq = "");

    [Obsolete("Only for internal usage")]
    [Post("{path}?method=save_object&ssid={ssid}&format=xml")]
    [AllowAnyStatusCode]
    internal Task<ApiResult> _Save<T>([Path] string path, [Body] T payload);
}
public static class IOdaApiExtensions
{
    public static async Task<ApiResult> Save<T>(this IOdaApi api, string path, T payload) where T : BaseDBEntity
    {
        var result = await api._Save(path, payload);
        payload.Id = result.Result;
        return result;
    }

    public static async Task<T?> Read<T>(this IOdaApi api, string path) where T : BaseDBEntity
    {
        var result = await api._Read<T>(path);
        return result;
    }
}

[XmlRoot("PACK")]
public record PackWrapper<T>
{
    [XmlElement("OBJECT")]
    public List<T> OBJECT;
}
[XmlRoot("noxml")]
public record ApiResult
{
    [XmlText]
    public string Result { get; set; }
}

[XmlRoot("OBJECT")]
public record IceCream : BaseDBEntity
{
    public decimal Price { get; set; }
    public string Name { get; set; }
}
[XmlRoot("OBJECT")]
public record BaseDBEntity
{
    [XmlAttribute("oid")]
    [JsonProperty("oid", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }
}

public static class XmlSerializerCache
{
    private static ConcurrentDictionary<Type, XmlSerializer> _xmlSerializers = new();

    public static XmlSerializer GetOrAdd<T>()
    {
        return _xmlSerializers.GetOrAdd(typeof(T), (type) => new XmlSerializer(typeof(T)));
    }
}

public class XmlResponseDeserializer : ResponseDeserializer
{
    private ConcurrentDictionary<Type, XmlSerializer> xmlSerializers = new();
    public override T Deserialize<T>(string content, HttpResponseMessage response, ResponseDeserializerInfo info)
    {
        var serializer = XmlSerializerCache.GetOrAdd<T>();
        using var stringReader = new StringReader(content);
        return (T)serializer.Deserialize(stringReader);
    }
}

public class XmlRequestBodySerializer : RequestBodySerializer
{
    private ConcurrentDictionary<Type, XmlSerializer> xmlSerializers = new();
    public override HttpContent SerializeBody<T>(T body, RequestBodySerializerInfo info)
    {
        if (body == null)
            return null;

        var serializer = XmlSerializerCache.GetOrAdd<T>();

        using var stringWriter = new StringWriter();

        serializer.Serialize(stringWriter, body);
        var content = new StringContent(stringWriter.ToString());
        // Set the default Content-Type header to application/xml
        content.Headers.ContentType.MediaType = "application/xml";
        return content;

    }
}
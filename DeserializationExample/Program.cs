using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

using Newtonsoft.Json;

using RestEase;

var cid = "H:1D670A1783307C2/D:WORK/D:1D71734AE4F4847/C:1D8EE93F67A4846";
var ssid = "3A1C27479F78CCD10F1C0EBA69E6B8D2";
IOdaApi api = RestClient.For<IOdaApi>("http://127.0.0.1:8080", async (request, cancellationToken) =>
{
    if (request?.Content is not null)
    {
        var content = (await request.Content.ReadAsStringAsync()) ?? string.Empty;
        await Console.Out.WriteLineAsync(content);
    }
});
api.ApiToken = ssid;

IceCream IceCream = new(Name: "MilkyWay", Price: 40);

var saveResult = await api.Save(cid, IceCream);
var readAfterSave = await api.Read<IceCream>($"{cid}/O:{IceCream.Id}");
Console.WriteLine(readAfterSave);

[BasePath("api")]
public interface IOdaApi
{
    [Path("ssid")]
    string ApiToken { get; set; }

    [Obsolete("Only for internal usage")]
    [Get("{path}?method=get_object&ssid={ssid}")]
    internal Task<T> _Read<T>([Path] string path, string xq = "");

    [Obsolete("Only for internal usage")]
    [Post("{path}?method=save_object&ssid={ssid}")]
    internal Task<ApiResult> _Save<T>([Path] string path, [Body] T payload);
}
public static class IOdaApiExtensions
{
    public static async Task<ApiResult> Save<T>(this IOdaApi api, string path, T payload) where T : BaseDBEntity
    {
        var result = await api._Save(path, new PackWrapper<T>(new() { payload }));
        payload.Id = result.Result;
        return result;
    }

    public static async Task<T?> Read<T>(this IOdaApi api, string path) where T : BaseDBEntity
    {
        var result = await api._Read<PackWrapper<T>>(path);
        return result.OBJECT.FirstOrDefault();
    }
}
public record PackWrapper<T>([JsonProperty("$OBJECT")] List<T> OBJECT);
public record ApiResult([property: JsonProperty("result")] string Result);
public record IceCream(string Name, decimal Price) : BaseDBEntity;
public record BaseDBEntity
{
    [JsonProperty("oid", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }
}
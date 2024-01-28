using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Content.Server._White.PandaSocket.Interfaces;

public interface IPandaStatusHandlerContext
{
    HttpMethod RequestMethod { get; }
    IPEndPoint RemoteEndPoint { get; }

    /// <summary>
    /// Stream that reads the request body data,
    /// </summary>
    Stream RequestBody { get; }
    Uri Url { get; }
    bool IsGetLike { get; }
    bool IsPostLike { get; }
    IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

    IDictionary<string, string> ResponseHeaders { get; }
    bool KeepAlive { get; set; }

    Task RespondNoContentAsync();

    Task RespondAsync(
        string text,
        HttpStatusCode code = HttpStatusCode.OK,
        string contentType = "text/plain; charset=utf-8");

    Task RespondAsync(
        string text,
        int code = 200,
        string contentType = "text/plain; charset=utf-8");

    Task RespondAsync(
        byte[] data,
        HttpStatusCode code = HttpStatusCode.OK,
        string contentType = "text/plain; charset=utf-8");

    Task RespondAsync(
        byte[] data,
        int code = 200,
        string contentType = "text/plain; charset=utf-8");

    Task RespondErrorAsync(HttpStatusCode code);

    Task RespondJsonAsync(object jsonData, HttpStatusCode code = HttpStatusCode.OK);

    Task<Stream> RespondStreamAsync(HttpStatusCode code = HttpStatusCode.OK);
}

public delegate Task<bool> PandaStatusHostHandlerAsync(
    IPandaStatusHandlerContext context);

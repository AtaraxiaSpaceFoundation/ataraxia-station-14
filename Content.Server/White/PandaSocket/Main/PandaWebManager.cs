using System.Linq;
using System.Net.Http;
using System.Web;
using Content.Shared.White;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robust.Shared.Configuration;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Content.Server.White.PandaSocket.Main;

public sealed class PandaWebManager
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();
    private string? _token;
    private string? _utkaUri;

    public void Initialize()
    {
        IoCManager.InjectDependencies(this);

        _token = _cfg.GetCVar(WhiteCVars.PandaToken);
        _cfg.OnValueChanged(WhiteCVars.PandaToken, token => _token = token, true);

        _utkaUri = _cfg.GetCVar(WhiteCVars.UtkaClientBind);
        _cfg.OnValueChanged(WhiteCVars.UtkaClientBind, uri => _utkaUri = uri, true);
    }

    public async void SendBotMessage(PandaBaseMessage message)
    {
        if (_utkaUri is null || _token is null)
            return;

        var json = JsonSerializer.Serialize(message, message.GetType());
        var jObj = (JObject) JsonConvert.DeserializeObject(json)!;
        var query = String.Join("&",
            jObj.Children().Cast<JProperty>()
                .Select(jp=>jp.Name + "=" + HttpUtility.UrlEncode(jp.Value.ToString())));

        var request = $"http://{_utkaUri}?token={_token}&{query}";

        try
        {
            await _httpClient.GetAsync(request);
        }
        catch (Exception e)
        {
            return;
        }
    }
}

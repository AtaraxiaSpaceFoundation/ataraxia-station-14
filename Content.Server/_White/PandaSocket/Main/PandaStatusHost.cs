using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Server._White.PandaSocket.Interfaces;
using Content.Shared._White;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Robust.Server.Player;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Content.Server._White.PandaSocket.Main;

public sealed partial class PandaStatusHost : IDisposable
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IDependencyCollection _deps = default!;
    [Dependency] private readonly ILogManager _logMan = default!;
    [Dependency] private readonly ITaskManager _taskManager = default!;

    private const string Sawmill = "panda.socket";

    private static readonly ConcurrentDictionary<string, IPandaCommand> Commands = new();
    private readonly List<PandaStatusHostHandlerAsync> _handlers = new();
    private HttpListener? _listener;
    private TaskCompletionSource? _stopSource;
    private ISawmill _httpSawmill = default!;
    private string? _token;

    public void Start()
    {
        var statusBind = _cfg.GetCVar(WhiteCVars.PandaStatusBind);
        if (statusBind == "")
            return;

        _httpSawmill = _logMan.GetSawmill($"{Sawmill}.http");

        RegisterHandlers();
        RegisterCommands();

        _token = _cfg.GetCVar(WhiteCVars.PandaToken);
        _cfg.OnValueChanged(WhiteCVars.PandaToken, token => _token = token, true);

        _stopSource = new TaskCompletionSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{statusBind}/");
        _listener.Start();

        Task.Run(ListenerThread);
    }

    private void RegisterCommands()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();

        var commands = types.Where(type =>
                typeof(IPandaCommand).IsAssignableFrom(type) && type.GetInterfaces().Contains(typeof(IPandaCommand))).ToList();

        foreach (var command in commands)
        {
            if (Activator.CreateInstance(command) is IPandaCommand pandaCommand)
            {
                Commands[pandaCommand.Name] = pandaCommand;
            }
        }
    }

    private void AddHandler(PandaStatusHostHandlerAsync handler)
    {
        _handlers.Add(handler);
    }

    private void ExecuteCommand(IPandaStatusHandlerContext context, PandaBaseMessage baseMessage)
    {
        var command = baseMessage.Command!;

        if (!Commands.ContainsKey(command))
        {
            return;
        }

        _taskManager.RunOnMainThread(() => Commands[command].Execute(context, baseMessage));
    }

    private async Task ListenerThread()
    {
        while (true)
        {
            var getContextTask = _listener!.GetContextAsync();
            var task = await Task.WhenAny(getContextTask, _stopSource!.Task);

            if (task == _stopSource.Task)
            {
                return;
            }

            // Task.Run this so it gets run on another thread pool thread.
#pragma warning disable CS4014
            Task.Run(async () =>
#pragma warning restore CS4014
            {
                try
                {
                    var ctx = await getContextTask;
                    await ProcessRequestAsync(ctx);
                }
                catch (Exception e)
                {
                    _httpSawmill.Error($"Error inside ProcessRequestAsync:\n{e}");
                }
            });
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var apiContext = (IPandaStatusHandlerContext) new ContextImpl(context);

        _httpSawmill.Info(
            $"{apiContext.RequestMethod} {apiContext.Url.PathAndQuery} from {apiContext.RemoteEndPoint}");

        try
        {
            foreach (var handler in _handlers)
            {
                if (await handler(apiContext))
                {
                    return;
                }
            }

            // No handler returned true, assume no handlers care about this.
            // 404.
            await apiContext.RespondAsync("Not Found", HttpStatusCode.NotFound);
        }
        catch (Exception e)
        {
            _httpSawmill.Error($"Exception in StatusHost: {e}");
            await apiContext.RespondErrorAsync(HttpStatusCode.InternalServerError);
        }
    }

    private bool ValidateMessage(string message, out PandaBaseMessage? baseMessage)
    {
        baseMessage = null;

        if (string.IsNullOrEmpty(message))
            return false;

        var collection = HttpUtility.ParseQueryString(message);
        var json = JsonSerializer.Serialize(collection.AllKeys.ToDictionary(y => y!, y => collection[y]));
        var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("token", out var token))
            return false;

        if (token.GetString() != _token)
            return false;

        if (!root.TryGetProperty("command", out var commandNameElement))
            return false;

        var commandName = commandNameElement.GetString();
        if (commandName == null)
            return false;

        var pandaCommand = Commands.Values.FirstOrDefault(x => x.Name == commandName);
        if (pandaCommand == null)
            return false;

        var messageType = pandaCommand.RequestMessageType;

        try
        {
            baseMessage = JsonConvert.DeserializeObject(json, messageType) as PandaBaseMessage;
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    private bool ValidatePostMessage(Stream message, out PandaBaseMessage? baseMessage)
    {
        baseMessage = null;

        var reader = new StreamReader(message, Encoding.UTF8);

        var task = Task.Run(async () => await reader.ReadToEndAsync());
        _taskManager.BlockWaitOnTask(task);
        var json = task.GetAwaiter().GetResult();

        var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        if (!root.TryGetProperty("token", out var token))
            return false;

        if (token.GetString() != _token)
            return false;

        if (!root.TryGetProperty("command", out var commandNameElement))
            return false;

        var commandName = commandNameElement.GetString();
        if (commandName == null)
            return false;

        var pandaCommand = Commands.Values.FirstOrDefault(x => x.Name == commandName);
        if (pandaCommand == null)
            return false;

        var messageType = pandaCommand.RequestMessageType;

        try
        {
            baseMessage = JsonConvert.DeserializeObject(json, messageType) as PandaBaseMessage;
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        if (_stopSource == null)
        {
            return;
        }

        _stopSource.SetResult();
        _listener!.Stop();
    }

    private sealed class ContextImpl : IPandaStatusHandlerContext
    {
        private readonly HttpListenerContext _context;
        private readonly Dictionary<string, string> _responseHeaders;
        public HttpMethod RequestMethod { get; }
        public IPEndPoint RemoteEndPoint => _context.Request.RemoteEndPoint!;
        public Stream RequestBody => _context.Request.InputStream;
        public Uri Url => _context.Request.Url!;
        public bool IsGetLike => RequestMethod == HttpMethod.Head || RequestMethod == HttpMethod.Get;
        public bool IsPostLike => RequestMethod == HttpMethod.Post;
        public IReadOnlyDictionary<string, StringValues> RequestHeaders { get; }

        public bool KeepAlive
        {
            get => _context.Response.KeepAlive;
            set => _context.Response.KeepAlive = value;
        }

        public IDictionary<string, string> ResponseHeaders => _responseHeaders;

        public ContextImpl(HttpListenerContext context)
        {
            _context = context;
            RequestMethod = new HttpMethod(context.Request.HttpMethod!);

            var headers = new Dictionary<string, StringValues>();
            foreach (string? key in context.Request.Headers.Keys)
            {
                if (key == null)
                    continue;

                headers.Add(key, context.Request.Headers.GetValues(key));
            }

            RequestHeaders = headers;
            _responseHeaders = new Dictionary<string, string>();
        }

        public T? RequestBodyJson<T>()
        {
            return JsonSerializer.Deserialize<T>(RequestBody);
        }

        public async Task<T?> RequestBodyJsonAsync<T>()
        {
            return await JsonSerializer.DeserializeAsync<T>(RequestBody);
        }

        public void Respond(string text, HttpStatusCode code = HttpStatusCode.OK, string contentType = MediaTypeNames.Text.Plain)
        {
            Respond(text, (int) code, contentType);
        }

        public void Respond(string text, int code = 200, string contentType = MediaTypeNames.Text.Plain)
        {
            _context.Response.StatusCode = code;
            _context.Response.ContentType = contentType;

            if (RequestMethod == HttpMethod.Head)
            {
                return;
            }

            using var writer = new StreamWriter(_context.Response.OutputStream, new UTF8Encoding());

            writer.Write(text);
        }

        public void Respond(byte[] data, HttpStatusCode code = HttpStatusCode.OK, string contentType = MediaTypeNames.Text.Plain)
        {
            Respond(data, (int) code, contentType);
        }

        public void Respond(byte[] data, int code = 200, string contentType = MediaTypeNames.Text.Plain)
        {
            _context.Response.StatusCode = code;
            _context.Response.ContentType = contentType;
            _context.Response.ContentLength64 = data.Length;

            if (RequestMethod == HttpMethod.Head)
            {
                _context.Response.Close();
                return;
            }

            _context.Response.OutputStream.Write(data);
            _context.Response.Close();
        }

        public Task RespondNoContentAsync()
        {
            RespondShared();

            _context.Response.StatusCode = (int) HttpStatusCode.NoContent;
            _context.Response.Close();

            return Task.CompletedTask;
        }

        public Task RespondAsync(string text, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/plain")
        {
            return RespondAsync(text, (int) code, contentType);
        }

        public async Task RespondAsync(string text, int code = 200, string contentType = "text/plain")
        {
            RespondShared();

            _context.Response.StatusCode = code;
            _context.Response.ContentType = contentType;

            if (RequestMethod == HttpMethod.Head)
                return;

            using var writer = new StreamWriter(_context.Response.OutputStream, new UTF8Encoding());

            await writer.WriteAsync(text);
        }

        public Task RespondAsync(byte[] data, HttpStatusCode code = HttpStatusCode.OK, string contentType = "text/plain")
        {
            return RespondAsync(data, (int) code, contentType);
        }

        public async Task RespondAsync(byte[] data, int code = 200, string contentType = "text/plain")
        {
            RespondShared();

            _context.Response.StatusCode = code;
            _context.Response.ContentType = contentType;
            _context.Response.ContentLength64 = data.Length;

            if (RequestMethod == HttpMethod.Head)
            {
                _context.Response.Close();
                return;
            }

            await _context.Response.OutputStream.WriteAsync(data);
            _context.Response.Close();
        }

        public void RespondError(HttpStatusCode code)
        {
            Respond(code.ToString(), code);
        }

        public Task RespondErrorAsync(HttpStatusCode code)
        {
            return RespondAsync(code.ToString(), code);
        }

        public void RespondJson(object jsonData, HttpStatusCode code = HttpStatusCode.OK)
        {
            RespondShared();

            _context.Response.ContentType = "application/json";

            JsonSerializer.Serialize(_context.Response.OutputStream, jsonData);

            _context.Response.Close();
        }

        public async Task RespondJsonAsync(object jsonData, HttpStatusCode code = HttpStatusCode.OK)
        {
            RespondShared();

            _context.Response.ContentType = "application/json";

            await JsonSerializer.SerializeAsync(_context.Response.OutputStream, jsonData);

            _context.Response.Close();
        }

        public Task<Stream> RespondStreamAsync(HttpStatusCode code = HttpStatusCode.OK)
        {
            RespondShared();

            _context.Response.StatusCode = (int) code;

            return Task.FromResult(_context.Response.OutputStream);
        }

        private void RespondShared()
        {
            foreach (var (header, value) in _responseHeaders)
            {
                _context.Response.AddHeader(header, value);
            }
        }
    }
}

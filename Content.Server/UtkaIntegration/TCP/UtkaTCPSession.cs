﻿using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using NetCoreServer;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.UtkaIntegration.TCP;

public sealed class UtkaTCPSession : TcpSession
{
    public event EventHandler<UtkaBaseMessage>? OnMessageReceived;
    private string BufferCahce = string.Empty;

    public bool Authenticated { get; set; }

    public UtkaTCPSession(TcpServer server) : base(server)
    {
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        BufferCahce += Encoding.UTF8.GetString(buffer, (int) offset, (int) size);

        HandleCache();
    }

    protected override void OnError(SocketError error)
    {
        SendAsync($"{error.ToString()}&%^sep^%&");
        base.OnError(error);
    }

    protected override void OnConnected()
    {
        SendAsync("Utka sosal handshake&%^sep^%&");
        base.OnConnected();
    }

    private bool ValidateMessage(string message, out UtkaBaseMessage? fromDiscordMessage)
    {
        fromDiscordMessage = null;

        if (string.IsNullOrEmpty(message))
        {
            return false;
        }

        var commandName = JObject.Parse(message)["command"];
        if (commandName == null)
            return false;

        var utkaCommand = UtkaTCPServer.Commands.Values.FirstOrDefault(x => x.Name == commandName.ToString());

        if (utkaCommand == null)
            return false;

        var messageType = utkaCommand.RequestMessageType;

        try
        {
            fromDiscordMessage = JsonSerializer.Deserialize(message, messageType) as UtkaBaseMessage;
        }
        catch (Exception e)
        {
            return false;
        }

        return true;
    }

    protected override void OnDisconnected()
    {
        OnDisconnecting();
        Dispose();
        BufferCahce = string.Empty;
    }

    private void HandleCache()
    {
        var handles = BufferCahce.Split("&%^sep^%&");

        for (var i = 0; i < handles.Length; i++)
        {
            var handle = handles[i];

            if (i + 1 == handles.Length && !BufferCahce.EndsWith("&%^sep^%&"))
                continue;

            if (handle.Length == 0 || !handle.StartsWith("{") || !handle.EndsWith("}"))
                continue;

            var pos = BufferCahce.IndexOf(handle);

            BufferCahce = BufferCahce.Substring(0, pos) + BufferCahce.Substring(pos + handle.Length + "&%^sep^%&".Length);

            if (!ValidateMessage(handle, out var message))
            {
                SendAsync("Validation fail&%^sep^%&");
                return;
            }

            OnMessageReceived?.Invoke(this, message!);
        }
    }
}

﻿using System.Net.WebSockets;
using System.Text;
using System.Xml.Linq;
using HuaJiBot.NET.Bot;
using HuaJiBot.NET.Events;
using HuaJiBot.NET.Plugin.MessageBridge.Types;
using HuaJiBot.NET.Plugin.MessageBridge.Types.Packet;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace HuaJiBot.NET.Plugin.MessageBridge;

public class PluginConfig : ConfigBase
{
    public ClientInfo[] Clients { get; set; } = [];

    public class ClientInfo
    {
        public ClientType Type { get; set; } = ClientType.Minecraft;
        public string Address { get; set; } = "ws://localhost:8080";
        public string Token { get; set; } = "token";
        public GroupConfig[] Groups { get; set; } = [];
    }

    public class GroupConfig
    {
        public string GroupId { get; set; } = "123456";
        public bool Enabled { get; set; } = true;

        /**
         *  是否将来自群的消息转发给客户端
         */
        public bool ForwardToClient { get; set; } = true;

        /**
         *  是否将来自客户端的消息转发给群
         */
        public bool ForwardFromClient { get; set; } = true;
    }

    public enum ClientType
    {
        Minecraft
    }
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    //配置
    public PluginConfig Config { get; } = new();

    private readonly Dictionary<PluginConfig.ClientInfo, WebsocketClient> _clients = new();

    //初始化
    protected override async Task InitializeAsync()
    {
        foreach (var clientInfo in Config.Clients)
        {
            WebsocketClient client =
                new(
                    new Uri(clientInfo.Address),
                    () =>
                    {
                        var cfg = new ClientWebSocket
                        {
                            Options =
                            {
                                KeepAliveInterval = TimeSpan.FromSeconds(5),
                                CollectHttpResponseDetails = true
                            }
                        };
                        if (!string.IsNullOrEmpty(clientInfo.Token))
                            cfg.Options.SetRequestHeader(
                                "Authorization",
                                "Bearer " + clientInfo.Token
                            );
                        return cfg;
                    }
                )
                {
                    IsReconnectionEnabled = true,
                    ReconnectTimeout = null,
                    MessageEncoding = Encoding.UTF8,
                    IsTextMessageConversionEnabled = true
                };
            client.MessageReceived.Subscribe(msg =>
            {
                if (msg.MessageType == WebSocketMessageType.Text)
                {
                    try
                    {
                        ProcessMessageFromClient(
                            msg.Text ?? throw new NullReferenceException("msg.Text"),
                            clientInfo
                        );
                    }
                    catch (Exception e)
                    {
                        Error("处理消息时出现异常：", e);
                    }
                }
                else
                {
                    Info("收到非文本消息！");
                }
            });
            client.DisconnectionHappened.Subscribe(info =>
                Info("Disconnection Happened " + info.Type)
            );
            client.ReconnectionHappened.Subscribe(info =>
                Info("Reconnection Happened " + info.Type)
            );
            await client.Start();
            _clients.Add(clientInfo, client);
        }

        Service.Events.OnGroupMessageReceived += (s, e) => _ = ProcessMessageFromGroupAsync(e);
        Service.Events.OnBotLogin += (s, e) =>
        {
            _defaultInformation = new SenderInformation(
                $"HuaJiBot.NET.Plugin.MessageBridge({e.ClientName})",
                e.ClientVersion ?? "?"
            );
        };

        Info("启动成功！");
    }

    private SenderInformation? _defaultInformation = new SenderInformation(
        "HuaJiBot.NET.Plugin.MessageBridge",
        "?"
    );

    private async Task ProcessMessageFromGroupAsync(GroupMessageEventArgs e)
    {
        var groupName = await e.GetGroupNameAsync();
        List<Action<string>> sendActions = new();
        foreach (var clientInfo in Config.Clients)
        {
            if (
                clientInfo
                    .Groups.Where(x => x is { Enabled: true, ForwardToClient: true })
                    .Any(x => x.GroupId == e.GroupId)
            )
            {
                if (_clients.TryGetValue(clientInfo, out var client))
                    sendActions.Add(msg => client.Send(msg));
            }
        }

        if (sendActions.Any())
        {
            var pkt = new GroupMessagePacket
            {
                Data = new GroupMessagePacketData
                {
                    SenderName = e.SenderMemberCard,
                    GroupName = groupName,
                    SenderId = e.SenderId,
                    GroupId = e.GroupId,
                    Message = e.TextMessage //todo structure message
                },
                Source = _defaultInformation
            };
            var str = pkt.ToJson();
            foreach (var action in sendActions)
                action(str);
        }
    }

    private void ProcessMessageFromClient(string messageRaw, PluginConfig.ClientInfo clientInfo)
    {
        var message = BasePacket.FromJson(messageRaw);
        var senderName = message is { Source.DisplayName: var sn } ? sn : "Unknown";
        switch (clientInfo.Type)
        {
            case PluginConfig.ClientType.Minecraft:
                switch (message)
                {
                    case PlayerChatPacket { Data: { Message: var msg, PlayerName: var name } }:
                        SendGroupMessage(clientInfo, $"[{senderName}] <{name}> {msg}");
                        break;
                    case PlayerJoinPacket { Data.PlayerName: var name }:
                        SendGroupMessage(clientInfo, $"[{senderName}] {name} 加入了服务器");
                        break;
                    case PlayerQuitPacket { Data.PlayerName: var name }:
                        SendGroupMessage(clientInfo, $"[{senderName}] {name} 离开了服务器");
                        break;
                    case PlayerDeathPacket { Data.DeathMessage: var msg }:
                        SendGroupMessage(clientInfo, $"[{senderName}] {msg}");
                        break;
                }
                break;
        }
    }

    private void SendGroupMessage(PluginConfig.ClientInfo clientInfo, string message)
    {
        foreach (
            var config in from config in clientInfo.Groups
            where config is { Enabled: true, ForwardFromClient: true }
            select config
        )
        {
            Service.SendGroupMessage(null, config.GroupId, new TextMessage(message));
        }
    }

    protected override void Unload() { }
}

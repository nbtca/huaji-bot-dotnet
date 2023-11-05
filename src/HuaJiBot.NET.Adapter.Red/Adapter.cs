﻿using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Adapter.Red;

public class RedProtocolAdapter(string url, string token) : BotServiceBase
{
    readonly Connector _connector = new(url);

    public override async Task SetupService()
    {
        await _connector.Connect(token);
    }

    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(string robotId, string targetGroup, string message)
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string robotId, string targetGroup, string userId, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string userId)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string userId)
    {
        throw new NotImplementedException();
    }

    public override void Log(object message)
    {
        throw new NotImplementedException();
    }

    public override void LogDebug(object message)
    {
        throw new NotImplementedException();
    }

    public override void LogError(object message, object detail)
    {
        throw new NotImplementedException();
    }

    public override string GetPluginDataPath()
    {
        throw new NotImplementedException();
    }
}

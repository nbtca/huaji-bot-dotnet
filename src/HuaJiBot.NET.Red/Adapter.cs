﻿namespace HuaJiBot.NET.Red;

public class RedProtocolAdapter : BotServiceBase
{
    public override string[] GetAllRobots()
    {
        throw new NotImplementedException();
    }

    public override void SendGroupMessage(string robotId, string targetGroup, string message)
    {
        throw new NotImplementedException();
    }

    public override string GetGroupName(string robotId, string targetGroup)
    {
        throw new NotImplementedException();
    }

    public override string GetMemberCard(string robotId, string targetGroup, string id)
    {
        throw new NotImplementedException();
    }

    public override void FeedbackAt(string robotId, string targetGroup, string id, string text)
    {
        throw new NotImplementedException();
    }

    public override MemberType GetMemberType(string robotId, string targetGroup, string id)
    {
        throw new NotImplementedException();
    }

    public override string GetNick(string robotId, string id)
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

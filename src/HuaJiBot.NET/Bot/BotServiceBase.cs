﻿using System.Text;
using System.Text.RegularExpressions;
using HuaJiBot.NET.Commands;
using HuaJiBot.NET.Events;

namespace HuaJiBot.NET.Bot;

public enum MemberType
{
    Unknown = 0,
    Member = 1,
    Admin = 2,
    Owner = 3
}



public abstract record SendingMessageBase
{
    public static implicit operator SendingMessageBase(string text) => new TextMessage(text);
};

public sealed record TextMessage(string Text) : SendingMessageBase;
public sealed record ImageMessage(string ImagePath) : SendingMessageBase;
public sealed record AtMessage(string Target) : SendingMessageBase;
public sealed record ReplyMessage(string ReplayMsgSeq, string ReplyMsgId, string Target) : SendingMessageBase;

public abstract class BotServiceBase
{
    public abstract Task SetupService();
    public Config.ConfigWrapper Config { get; internal set; } = null!;
    public Events.Events Events { get; } = new();
    public abstract string[] GetAllRobots();
    public abstract void SendGroupMessage(string? robotId, string targetGroup, params SendingMessageBase[] messages);
    public abstract void FeedbackAt(
        string? robotId,
        string targetGroup,
        string userId,
        string text
    );
    public abstract MemberType GetMemberType(string robotId, string targetGroup, string userId);
    public abstract string GetNick(string robotId, string userId);
    public abstract void Log(object message);
    public abstract void Warn(object message);
    public abstract void LogDebug(object message);
    public abstract void LogError(object message, object detail);
    public abstract string GetPluginDataPath();
    private bool ProcessHelp(GroupMessageEventArgs e)
    {
        var reader = e.CommandReader;
        if (reader.Match([
                "help",
            "帮助"
            ], x => x, out _))
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用命令：");
            foreach (var (name, (description, _, info)) in _commands)
            {
                sb.Append(name);
                if (info.Any())
                {
                    sb.Append(" ");
                    foreach (var arg in info)
                    {
                        sb.Append(arg.Attribute.ArgumentType switch
                        {
                            CommandArgumentType.String => "<string>",
                            CommandArgumentType.RegexString => "<regex>",
                            CommandArgumentType.Enum => "<enum>",
                            _ => throw new ArgumentOutOfRangeException()
                        });
                        if (!arg.IsOptional)
                        {
                            sb.Append('*');
                        }
                        sb.Append(' ');
                    }
                }
                sb.AppendLine();
                sb.AppendLine($"    {description}");
            }
            e.Feedback(sb.ToString());
            return true;
        }
        return false;
    }
    private void ProcessCommand(GroupMessageEventArgs e)
    {
        if (ProcessHelp(e))
        {
            return;
        }
        var reader = e.CommandReader;
        if (reader.Match(_commands.Keys, out var matched))
        {
            var (description, method, info) = _commands[matched];
            object?[]? args = null;
            if (info.Any())
            {
                args = new object?[info.Length];
                for (int i = 0; i < info.Length; i++)
                {
                    var arg = info[i];
                    object? value = null;
                    switch (arg.Attribute.ArgumentType)
                    {
                        case CommandArgumentType.String:

                            {
                                if (!reader.Input(out var str))
                                    continue;
                                value = str;
                            }
                            break;
                        case CommandArgumentType.RegexString:

                            {
                                var regexAttr = (CommandArgumentStringMatchAttribute)arg.Attribute;
                                if (!reader.Input(out var str))
                                    continue;
                                if (!Regex.IsMatch(str, regexAttr.Pattern, regexAttr.Options))
                                    continue;
                                value = str;
                            }
                            break;
                        case CommandArgumentType.Enum:

                            {
                                var enumAttr = (CommandArgumentEnumAttributeBase)arg.Attribute;
                                if (!reader.Match(
                                        enumAttr.EnumItems,
                                        x => [
                                            x.Key,
                                            x.Alias
                                        ],
                                        out var item
                                    ))
                                {
                                    continue;
                                }
                                value = item.Value;
                            }
                            break;
                    }
                    args[i] = value;
                }
            }
            //匹配参数
            method(args);
        }
    }

    Dictionary<
        string,
        (string description, Action<object?[]?> method, PluginBase.CommandArgumentInfo[] info)
    > _commands = new();

    public void LoadAddCommand(PluginBase plugin)
    {
        foreach (var (name, description, method, info) in plugin.GetAllCommands())
        {
            _commands.Add(name, (description, method, info));
            Log($"读取命令 {name} ，描述：{description}");
        }
        if (_commands.Any())
        {
            //监听群消息事件，匹配命令
            Events.OnGroupMessageReceived += (sender, e) => ProcessCommand(e);
            Log($"从插件 {plugin.Name} 加载了 {_commands.Count} 条命令");
        }
    }
}

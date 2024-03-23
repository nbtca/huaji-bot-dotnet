﻿using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Config;

public partial class Config
{
    public class OneBotConnectionInfo
    {
        public string Url = "";
        public string? Token = "";
    }

    public OneBotConnectionInfo OneBot = new();

    public class SatoriConnectionInfo
    {
        public string Url = "";
        public string Token = "";
    }

    public SatoriConnectionInfo Satori = new();
    public string[] ExtraPlugins { get; set; } = [];

    public Dictionary<string, JObject> Plugins = new();
}

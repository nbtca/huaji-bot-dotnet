﻿using System.Text;
using HuaJiBot.NET.Plugin.Calendar;

namespace HuaJiBot.NET.Plugin.RepairTeam;

public class PluginConfig : ConfigBase
{
    public int MinRange = -128;
    public int MaxRange = 48;
}

public class PluginMain : PluginBase, IPluginWithConfig<PluginConfig>
{
    public PluginConfig Config { get; } = new();

    protected override Task Initialize()
    {
        //订阅群消息事件
        Service.Events.OnGroupMessageReceived += Events_OnGroupMessageReceived;
        Service.Log("[日程] 启动成功！");
        LoadCalendar();
        return Task.CompletedTask;
    }

    private const string icalUrl = "https://i.nbtca.space/panel/ical";

    private Ical.Net.Calendar ical;
    private DateTime lastLoadTime = DateTime.MinValue;

    private Task LoadCalendar()
    {
        lock (this)
        {
            if (DateTime.Now - lastLoadTime < TimeSpan.FromMinutes(15)) //如果距离上次加载小于15分钟
            {
                return Task.CompletedTask; //直接返回
            }
            //否则重新加载
            lastLoadTime = DateTime.Now;
        }
        return Task.Run(async () =>
        {
            try
            {
                HttpClient client = new();
                var resp = await client.GetAsync(icalUrl); //从Url获取
                resp.EnsureSuccessStatusCode();
                ical = Ical.Net.Calendar.Load(await resp.Content.ReadAsStringAsync());
                var now = DateTime.Now;
                var end = now.AddDays(7);
                foreach (var (period, e) in ical.GetEvents(now, end))
                {
                    Service.Log(period.StartTime);
                    Service.Log(e.Summary);
                }
            }
            catch (Exception ex)
            {
                Service.LogError(nameof(LoadCalendar), ex);
            }
        });
    }

    private readonly Dictionary<string, DateTime> _cache = new();

    private void Events_OnGroupMessageReceived(object? sender, Events.GroupMessageEventArgs e)
    {
        Task.Run(async () =>
        {
            if (e.TextMessage.StartsWith("日程"))
            {
                await LoadCalendar();
                const int coldDown = 10_000; //冷却时间
                var now = DateTime.Now; //当前时间
                if (_cache.TryGetValue(e.SenderId, out var lastTime)) //如果缓存中有上次发送的时间
                {
                    var diff = (now - lastTime).TotalMilliseconds; //计算时间差
                    if (diff < coldDown) //如果小于冷却时间
                    {
                        e.Feedback($"我知道你很急，但是你先别急，{(coldDown - diff) / 1000:F0}秒后再逝");
                        return;
                    }
                }
                _cache[e.SenderId] = now; //更新缓存
                _ = Task.Delay(coldDown).ContinueWith(_ => _cache.Remove(e.SenderId)); //冷却时间后移除缓存
                var week = 1;
                var content = e.TextMessage[2..].Trim(); //去除前两个字符后的文本内容
                if (!string.IsNullOrWhiteSpace(content)) //参数不为空
                {
                    if (!int.TryParse(content, out week)) //尝试转换为数字表示周数
                    {
                        e.Feedback("参数错误");
                        return;
                    }
                }

                if (week < Config.MinRange || week > Config.MaxRange) //进行一个输入范围合法性检查
                {
                    e.Feedback($"超出范围 [{Config.MinRange},{Config.MaxRange}] ");
                    return;
                }
                DateTime start,
                    end;
                if (week > 0) //正数表示未来
                {
                    start = now; //开始于当前时间
                    end = start.AddDays(7 * week); //当前时间加上周数
                }
                else
                {
                    end = now; //结束时间
                    start = end.AddDays(7 * week); //week是负的，所以开始时间等于现在减去..
                }
                StringBuilder sb = new();
                foreach (var (period, ev) in ical.GetEvents(start, end)) //遍历每一个事件
                {
                    sb.AppendLine($"{period.StartTime:yyyy-MM-dd HH:mm} {ev.Summary}"); //输出
                }
                e.Feedback($"近{week}周的日程：\n{sb}");
                //Service.LogDebug(JsonConvert.SerializeObject(e));
            }
        });
    }

    protected override void Unload() { }
}

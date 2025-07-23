using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starinfall
{
    public class MainConfig
    {
        [Description("入服提醒")]
        public string JoinMessage { get; set; } = "这是一个实例提醒";
        [Description("入服提醒时长(s)（设为0以禁用）")]
        public int JoinMessageTime { get; set; } = 8;
        [Description("定时提醒")]
        public string ScheduledMessage { get; set; } = "这是一个示例定时信息";
        [Description("定时提醒时间(s)（设为0以禁用）")]
        public int ScheduledTime { get; set; } = 300;
        [Description("MySQL连接字符串")]
        public string ConnectionString { get; set; } = "server=127.0.0.1;database=scp;user=root;password=123456;charset=utf-8";
    }
    public class PluginMain : Plugin<MainConfig>
    {
        public static PluginMain Instance { get; private set; }
        public override string Author => "崩坏4:银河漫游团队";
        public override string Name => "Starinfall";
        public override Version Version => new Version(0,1,0,0);
        public override string Description => "服务器核心插件";
        public override Version RequiredApiVersion => new Version("0.0.0.0");
        public string Codename => "StarRail";
        public override void Enable()
        {
            Instance = this;
            BroadcastMain.OnEnabled();
            Logger.Info("Plugin Loaded");
        }
        public override void Disable()
        {
            Logger.Info("Plugin Disabled");
        }
    }
}

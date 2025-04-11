using CommandSystem;
using GameCore;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using PlayerRoles;
using Scp914;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starinfall
{
    public enum BroadcastPriority : byte { Lowest = 1, Lower = 50, Low = 75, Normal = 100, High = 125, Higher = 150, Highest = 200, eme = 255 }
    public class BroadcastItem : IComparable<BroadcastItem>
    {
        public bool Noprefix = false;
        public readonly DateTime CreatedTime = DateTime.Now;
        public int time;
        public string prefix, text;
        public Func<Player, bool> Check;
        public byte priority;
        public List<string> targets = new List<string>();
        public static bool operator <(BroadcastItem lhs, BroadcastItem rhs)
        {
            if (lhs.priority == rhs.priority) return lhs.CreatedTime < rhs.CreatedTime;
            else return lhs.priority < rhs.priority;
        }
        public static bool operator >(BroadcastItem lhs, BroadcastItem rhs)
        {
            if (lhs.priority == rhs.priority) return lhs.CreatedTime > rhs.CreatedTime;
            else return lhs.priority > rhs.priority;
        }
        public override string ToString()
        {
            string result = "";
            if (!Noprefix) result += $"「{prefix}」";
            result += text;
            //result += $"";
            return result;
        }
        public int CompareTo(BroadcastItem other)
        {
            if (this < other) return 1;
            if (this > other) return -1;
            return 0;
        }
    }
    public class MiscBroadcast
    {
        public static CoroutineHandle ScheduledMessage;
        public static IEnumerator<float> ScheduleMessager(int time)
        {
            yield return Timing.WaitForSeconds(time);
            BroadcastMain.SendGlobalcast(new BroadcastItem
            {
                prefix = "定时公告",
                priority = (byte)BroadcastPriority.Low,
                text = PluginMain.Instance.Config.ScheduledMessage,
                time = 5
            });
        }
        
        public static void On914Activated(Scp914ActivatedEventArgs ev)
        {
            BroadcastMain.SendNormalCast(new BroadcastItem
            {
                prefix = "<color=yellow>SCP914</color>",
                Check = p => p.Room.Name == RoomName.Lcz914,
                priority = (byte)BroadcastPriority.High,
                text = $"{ev.Player.Nickname}以{Scp914Mode(ev.KnobSetting)}模式启动了914",
                time = 3
            });
        }
        public static void On914KnobChanged(Scp914KnobChangedEventArgs ev)
        {
            BroadcastMain.SendNormalCast(new BroadcastItem
            {
                prefix = "<color=yellow>SCP914</color>",
                Check = p => p.Room.Name == RoomName.Lcz914,
                priority = (byte)BroadcastPriority.High,
                text = $"{ev.Player.Nickname}将加工模式调整为{Scp914Mode(ev.KnobSetting)}",
                time = 3
            });
        }
        public static string Scp914Mode(Scp914KnobSetting setting)
        {
            switch (setting)
            {
                case Scp914KnobSetting.Rough: return "粗加工";
                case Scp914KnobSetting.Coarse: return "半粗加工";
                case Scp914KnobSetting.OneToOne: return "1:1";
                case Scp914KnobSetting.Fine: return "精加工";
                case Scp914KnobSetting.VeryFine: return "超精加工";
            }
            return "Unknown";
        }

        public static void OnRoundStarted()
        {
            if (PluginMain.Instance.Config.ScheduledTime != 0) ScheduledMessage = Timing.RunCoroutine(ScheduleMessager(PluginMain.Instance.Config.ScheduledTime));
        }

        public static void OnPlayerJoined(PlayerJoinedEventArgs ev)
        {
            if (PluginMain.Instance.Config.JoinMessage == "null") return;
            BroadcastMain.SendNormalCast(new BroadcastItem
            {
                prefix = "<color=red>入服提醒</color>",
                targets = new List<string> { ev.Player.UserId },
                priority = (byte)BroadcastPriority.Low,
                text = PluginMain.Instance.Config.JoinMessage,
                time = 3
            });
        }

        public static void OnRoundRestart()
        {
            Timing.KillCoroutines(ScheduledMessage);
        }
    }
    public class BroadcastMain
    {
        public static CoroutineHandle ExpiringCoroutine, MessageCoroutine;
        public static List<BroadcastItem> globals = new List<BroadcastItem>();
        public static List<BroadcastItem> normals = new List<BroadcastItem>();
        public static void OnRoundRestart()
        {
            globals.Clear();
            normals.Clear();
            Timing.KillCoroutines(ExpiringCoroutine, MessageCoroutine);
        }
        public static void OnEnabled()
        {
            ServerEvents.RoundRestarted += OnRoundRestart;
            ServerEvents.WaitingForPlayers += OnWaitingForPlayersEvent;
            Scp914Events.KnobChanged += MiscBroadcast.On914KnobChanged;
            Scp914Events.Activated += MiscBroadcast.On914Activated;
            ServerEvents.RoundStarted += MiscBroadcast.OnRoundStarted;
            PlayerEvents.Joined += MiscBroadcast.OnPlayerJoined;
            ServerEvents.RoundRestarted += MiscBroadcast.OnRoundRestart;
        }
        public static void OnWaitingForPlayersEvent()
        {
            ExpiringCoroutine = Timing.RunCoroutine(ExpiringMain());
            MessageCoroutine = Timing.RunCoroutine(MessageMain());
        }
        public static void SendGlobalcast(BroadcastItem item)
        {
            globals.Add(item);
        }
        public static void SendNormalCast(BroadcastItem item)
        {
            normals.Add(item);
        }
        public static IEnumerator<float> ExpiringMain()
        {
            while (true)
            {
                //消息过期处理
                foreach (var item in globals)
                {
                    item.time--;
                }
                foreach (var item in normals)
                {
                    item.time--;
                }
                for (int i = globals.Count - 1; i >= 0; --i)
                {
                    if (globals[i].time < 0) globals.RemoveAt(i);
                }
                for (int i = normals.Count - 1; i >= 0; --i)
                {
                    if (normals[i].time < 0) normals.RemoveAt(i);
                }
                yield return Timing.WaitForSeconds(1f);
            }
        }
        public static IEnumerator<float> MessageMain()
        {
            while (true)
            {
                //消息主循环
                foreach (Player player in Player.List)
                {
                    player.SendBroadcast(GetOutput(player), 5, shouldClearPrevious: true);
                }
                yield return Timing.WaitForSeconds(0.5f);
            }
        }
        public static string GetOutput(Player player)
        {
            try
            {
                string response = "<line-height=65%><size=24>";
                List<BroadcastItem> items = new List<BroadcastItem>();
                items.AddRange(globals.Where(b => b.time > 0));
                items.AddRange(from item in normals
                               where (item.time > 0) && player != Player.Host && (item.targets.Contains(player.UserId) || (item.Check != null && item.Check(player)))
                               select item);
                items.Sort();
                int remain = 5;
                foreach (var item in items)
                {
                    if (remain > 0) response += $"{item}\n";
                    remain--;
                }
                if (remain < 0) response += $"<alpha=#AA>还有{-remain}条信息未显示...";
                response += "</size></line-height>";
                return response;
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.GetType()}:{ex.Message}");
                return "";
            }
        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    public class TeamChat : ICommand
    {
        public string Command => "c";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "阵营聊天";

        public static bool Spectators(Player player) => player.Role == RoleTypeId.Spectator;

        public static bool MTF(Player player) => player.Role == RoleTypeId.NtfSpecialist || player.Role == RoleTypeId.NtfPrivate || player.Role == RoleTypeId.NtfCaptain || player.Role == RoleTypeId.NtfSergeant || player.Role == RoleTypeId.FacilityGuard;

        public static bool Chaos(Player player) => player.Role == RoleTypeId.ChaosConscript || player.Role == RoleTypeId.ChaosRifleman || player.Role == RoleTypeId.ChaosMarauder || player.Role == RoleTypeId.ChaosRepressor;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            BroadcastItem item = new BroadcastItem { prefix = "<color=yellow>阵营聊天</color>", priority = (byte)BroadcastPriority.Normal, text = $"{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}", time = 5 };
            switch (player.Team)
            {
                case Team.FoundationForces:
                    item.Check += MTF;
                    item.text = $"<color=cyan>{item.text}</color>";
                    break;
                case Team.ChaosInsurgency:
                    item.Check += Chaos;
                    item.text = $"<color=green>{item.text}</color>";
                    break;
                case Team.Scientists:
                case Team.ClassD:
                    response = "博士和DD无法使用阵营聊天";
                    return false;
                case Team.Dead:
                    item.Check += Spectators;
                    break;
            }
            BroadcastMain.SendNormalCast(item);
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    public class Bchat : ICommand
    {
        public string Command => "globalchat";

        public string[] Aliases => new string[] { "bc" };

        public string Description => "全部聊天";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            BroadcastItem item = new BroadcastItem { prefix = "<color=yellow>全局聊天</color>", priority = (byte)BroadcastPriority.Normal, text = $"{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}", time = 5 };
            BroadcastMain.SendGlobalcast(item);
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class AdminBroadCast : ICommand
    {
        public string Command => "abc";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "管理员公告";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            BroadcastItem broadcastItem = new BroadcastItem { prefix = "<color=cyan>管理员公告</color>", priority = (byte)BroadcastPriority.High, text = $"{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}", time = arguments.Count > 1 ? int.Parse(arguments.At(1)) : 15 };
            BroadcastMain.SendGlobalcast(broadcastItem);
            response = "Done!";
            return true;
        }
    }
    [CommandHandler(typeof(ClientCommandHandler))]
    public class AdminHelp : ICommand
    {
        public string Command => "ac";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "向在线管理发送消息";

        public bool CheckPermission(Player player)
        {
            return (player.UserGroup?.Permissions & (ulong)PlayerPermissions.AdminChat) != 0;
        }

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var adminList = from p in Player.List
                            where CheckPermission(p)
                            select p;
            if (adminList.Count() == 0)
            {
                response = "当前服务器中没有管理员！";
                return false;
            }
            else
            {
                Player player = Player.Get(sender);
                BroadcastItem item = new BroadcastItem
                {
                    prefix = "<color=red>玩家求助</color>",
                    priority = (byte)BroadcastPriority.High,
                    text = $"{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}",
                    time = 8,
                    Check = CheckPermission
                };
                BroadcastMain.SendNormalCast(item);
                response = "Done!";
                return true;
            }
        }
    }
}

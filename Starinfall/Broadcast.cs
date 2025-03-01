using CommandSystem;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
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
            string result = "「";
            if (!Noprefix) result += $"{prefix}|";
            result += text;
            result += $"」";
            return result;
        }
        public int CompareTo(BroadcastItem other)
        {
            if (this < other) return 1;
            if (this > other) return -1;
            return 0;
        }
    }
    public class BroadcastMain
    {
        public static CoroutineHandle coroutine;
        public static List<BroadcastItem> globals = new List<BroadcastItem>();
        public static List<BroadcastItem> normals = new List<BroadcastItem>();
        public static void OnRoundRestart()
        {
            globals.Clear();
            normals.Clear();
            Timing.KillCoroutines(coroutine);
        }
        public static void OnEnabled()
        {
            ServerEvents.RoundRestarted += OnRoundRestart;
            ServerEvents.WaitingForPlayers += OnWaitingForPlayersEvent;
        }
        public static void OnWaitingForPlayersEvent()
        {
            coroutine = Timing.RunCoroutine(Main());
        }
        public static void SendGlobalcast(BroadcastItem item)
        {
            globals.Add(item);
        }
        public static void SendNormalCast(BroadcastItem item)
        {
            normals.Add(item);
        }
        public static IEnumerator<float> Main()
        {
            while (true)
            {
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
            BroadcastItem item = new BroadcastItem { prefix = "阵营聊天", priority = (byte)BroadcastPriority.Normal, text = $"[{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}]", time = 5 };
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
            BroadcastItem item = new BroadcastItem { prefix = "全局聊天", priority = (byte)BroadcastPriority.Normal, text = $"[{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}]", time = 5 };
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
            BroadcastItem broadcastItem = new BroadcastItem { prefix = "管理员公告", priority = (byte)BroadcastPriority.High, text = $"[{player.DisplayName}:{arguments.At(0).Replace('|', ' ')}]", time = arguments.Count > 1 ? int.Parse(arguments.At(1)) : 15 };
            BroadcastMain.SendGlobalcast(broadcastItem);
            response = "Done!";
            return true;
        }
    }
}

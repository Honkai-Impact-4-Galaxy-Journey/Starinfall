using GameCore;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Subroutines;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starinfall
{

    public class UI
    {
        public static CoroutineHandle maincoroutine;
        public static string TopMessage = "";
        public static string GetTeamStatus(Player player)
        {
            if (!Round.IsRoundStarted || player.Role == RoleTypeId.Tutorial) return "";
            Team team = player.Team;
            string result = "<color=orange>团队</color>|";
            if (Player.ReadyList.Count(p => p.Team == team) == 1 && player.IsAlive)
            {
                result += $"<color=yellow>你是阵营中最后一人，祝好运</color>";
                return result;
            }
            switch (team)
            {
                case Team.FoundationForces:
                    result += $"<color=blue>九尾狐</color>：剩余{Player.ReadyList.Count(p => p.Team == team)}人";
                    return result;
                case Team.ChaosInsurgency:
                    result += $"<color=green>混沌</color>：剩余{Player.ReadyList.Count(p => p.Team == team)}人";
                    return result;
                case Team.Scientists:
                    result += $"<color=yellow>科学家</color>：剩余{Player.ReadyList.Count(p => p.Team == team)}人";
                    return result;
                case Team.ClassD:
                    result += $"<color=green>DD</color>：剩余{Player.ReadyList.Count(p => p.Team == team)}人";
                    return result;
                case Team.SCPs:
                    var scps = from p in Player.ReadyList
                               where p.Team == team
                               select p;
                    int js = scps.Count(p => p.Role == RoleTypeId.Scp0492);
                    foreach (Player p in scps)
                    {
                        if (p.Role == RoleTypeId.Scp0492) continue;
                        result += $"<color=red>{p.Role}</color>: ";
                        if (player.Role != RoleTypeId.Scp079)
                        {
                            if (p.ReferenceHub.playerStats.GetModule<HumeShieldStat>().CurValue != 0) result += $"<color=purple>{(int)p.ReferenceHub.playerStats.GetModule<HumeShieldStat>().CurValue}</color> ";
                            else
                            {
                                if (p.Health / p.MaxHealth > 0.8) result += $"<color=green>{(int)p.Health}</color> ";
                                else if (p.Health / p.MaxHealth > 0.4) result += $"<color=yellow>{(int)p.Health}</color> ";
                                else result += $"<color=red>{(int)p.Health}</color> ";
                            }
                        }
                        else
                        {
                            SubroutineManagerModule module = ((Scp079Role)player.RoleBase).SubroutineModule;
                            module.TryGetSubroutine(out Scp079AuxManager auxManager);
                            module.TryGetSubroutine(out Scp079TierManager tierManager);
                            result += $"<color=#FF96DE>[{tierManager.AccessTierLevel}]</color> <color=#66CCFF>{(int)auxManager.CurrentAux}</color>";
                        }
                        result += '|';
                    }
                    if (js > 0) result += $"小僵尸：剩余{js}个";
                    return result;
                default:
                    return "\n";
            }
        }
        public static Stopwatch stopwatch;
        public static void OnWaitingForPlayers()
        {
            maincoroutine = Timing.RunCoroutine(Main());
        }
        public static void OnRoundRestart()
        {
            Timing.KillCoroutines(maincoroutine);
        }
        public static IEnumerator<float> Main()
        {
            while (true)
            {
                foreach (Player player in Player.ReadyList)
                {
                    string topMessage = GetTopMessage(player);
                    string middleMessage = GetMiddleMessage(player);
                    string bottomMessage = GetBottomMessage(player);
                    player.SendHint($"<line-height=65%><size=24>{topMessage}\n\n{middleMessage}\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n{bottomMessage}\n\n\n\n\n\n\n\n\n\n\n\n</size>");
                }
                yield return Timing.WaitForSeconds(0.5f);
            }
        }
        public static string GetBottomMessage(Player player) //第三部分：底部区域
        {
            string result = $"<size=20><color=green>{player.Nickname}</color> <color=#66CCFF>[LV0-档案禁用]</color>|经验：0 / 0|TPS:{Server.Tps}</size>";
            return result;
        }
        public static string GetMiddleMessage(Player player) //第二部分：中上区域
        {
            try
            {
                List<BroadcastItem> items = new List<BroadcastItem>();
                items.AddRange(BroadcastMain.globals.Where(b => b.time > 0));
                items.AddRange(from item in BroadcastMain.normals
                               where (item.time > 0) && (item.targets.Contains(player.UserId) || (item.Check != null && item.Check(player)))
                               select item);
                items.Sort();
                int remain = 8;
                string result = "", text = "";
                foreach (BroadcastItem item in items)
                {
                    if (remain > 0)
                    {
                        text += $"{item}\n";
                        remain--;
                    }
                }
                for (int i = 0; i < remain / 2; i++)
                {
                    result += '\n';
                }
                result += text;
                if (remain % 2 == 1) result += "\n";
                for (int i = 0; i < remain / 2; i++)
                {
                    result += '\n';
                }
                return result;
            }
            catch (Exception e)
            {
                Logger.Error($"{e.Message}{e.GetType()}");
            }
            return "";
        }
        public static string GetTopMessage(Player player) //顶部信息区
        {
            string result = "";
            string Teams = GetTeamStatus(player); // 第一部分：阵营信息与刷新时间显示
            string Indicators = "|";
            int lines = 4;
            if (Server.FriendlyFire == true) Indicators += "友伤已开启|";
            if (Round.IsLocked) Indicators += "回合已锁定|";
            if (player.IsGodModeEnabled) Indicators += "无敌已开启|";
            if (player.IsBypassEnabled) Indicators += "万能卡已启用|";
            if (!string.IsNullOrEmpty(Teams))
            {
                result += $"<align=center>{Teams}\n</align>";
                lines--;
            }
            if (Indicators != "|")
            {
                result += $"<align=center>{Indicators}\n</align>";
                lines--;
            }
            for (; lines > 0; lines--)
            {
                result += "\n";
            }
            return result;
        }
    }
}

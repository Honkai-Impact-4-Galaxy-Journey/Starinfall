using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
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
}

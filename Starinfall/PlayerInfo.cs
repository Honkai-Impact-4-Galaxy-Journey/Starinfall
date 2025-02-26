using LabApi.Events.Arguments.ServerEvents;
using LabApi.Features.Wrappers;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starinfall
{
    public class PlayerInfo
    {
        public string Steamid { get; set; } // 不会变动，需要初始化
        public string LastNickName { get; set; }
        public string BadgeText { get; set; } // 不会变动
        public string BadgeColor { get; set; } // 不会变动
        public DateTime LastJoinTime { get; set; }
        public bool ReserveSlotEnabled { get; set; } // 不会变动
        public string LastIP { get; set; }
        public int Id { get; set; } // 不会变动
        public string JoinMessage { get; set; } // 不会变动
        public int Exp { get; set; }
        public int Level { get; set; }
        public string AdminRank { get; set; } // 不会变动
        public bool Cover { get; set; } // 不会变动
        public bool Modified = false;
    }
    public class Database
    {
        public static MySqlConnection connection;
        public static void OpenConnection(string connectionString)
        {
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }
        public static void CloseConnection()
        {
            connection.Close();
        }
        public static PlayerInfo GetPlayerInfo(string steamid)
        {
            using (MySqlCommand command = new MySqlCommand($"select * from playerinfo where SteamID='{steamid}'", connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    PlayerInfo info = new PlayerInfo();
                    if (!reader.HasRows)
                    {
                        info = CreateNewPlayer(Player.Get(steamid));
                    }
                    else info = Transfer(reader);
                    return info;
                }
            }
        }

        private static PlayerInfo CreateNewPlayer(Player player)
        {
            PlayerInfo info = new PlayerInfo() { Steamid = player.UserId, LastJoinTime = DateTime.Now, LastIP = player.IpAddress, LastNickName = player.Nickname };
            using (MySqlCommand command = new MySqlCommand($"INSERT INTO playerinfo (SteamID, LastJoinTime, LastIP, LastNickName) VALUES ('{info.Steamid}', NOW(), '{info.LastIP}', '{info.LastNickName}')", connection))
            {
                command.ExecuteNonQuery();
            }
            using (MySqlCommand command = new MySqlCommand($"select * from playerinfo where SteamID='{player.UserId}'", connection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    info = Transfer(reader);
                }
            }
            return info;
        }

        private static PlayerInfo Transfer(MySqlDataReader reader)
        {
            return new PlayerInfo()
            {
                Steamid = reader.GetString("SteamID"),
                AdminRank = reader.GetString("AdminRank"),
                BadgeColor = reader.GetString("BadgeColor"),
                BadgeText = reader.GetString("BadgeText"),
                Cover = reader.GetBoolean("Cover"),
                Exp = reader.GetInt32("Exp"),
                Id = reader.GetInt32("id"),
                JoinMessage = reader.GetString("JoinMessage"),
                LastIP = reader.GetString("LastIP"),
                LastJoinTime = reader.GetDateTime("LastJoinTime"),
                LastNickName = reader.GetString("LastNickName"),
                Level = reader.GetInt32("Level"),
                ReserveSlotEnabled = reader.GetBoolean("ReserveSlotEnabled")
            };
        }
        public static bool IsUseful => connection.State == System.Data.ConnectionState.Open;
    }
}

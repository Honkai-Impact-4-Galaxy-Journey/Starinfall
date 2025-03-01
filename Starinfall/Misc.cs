using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starinfall
{
    [CommandHandler(typeof(ClientCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class Starinfo : ICommand
    {
        public string Command => "Starinfo";

        public string[] Aliases => Array.Empty<string>();

        public string Description => "Starinfall相关信息";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Version version = PluginMain.Instance.Version;
            response = $"Starinfall Version {version.Major}.{version.Minor}{(version.Revision == 0 ? "" : $"Patch{version.Revision}")}: {PluginMain.Instance.Codename}\n";
            response += $"Copyright (C) {PluginMain.Instance.Author}, All Rights Reserved.\n";
            response += "仓库地址: https://github.com/Honkai-Impact-4-Galaxy-Journey/Starinfall";
            return true;
        }
    }
}

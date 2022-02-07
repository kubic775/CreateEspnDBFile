using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace CreateEspnDBFile
{
    public static class PlayerNameConverter
    {
        private static readonly Dictionary<string, string> PlayersNames = new Dictionary<string, string>();

        static PlayerNameConverter()
        {
            foreach (var player in JArray.Parse(File.ReadAllText("PlayerNamesConverter.json")))
            {
                PlayersNames.Add(player["YahooName"].Value<string>(), player["EspnName"].Value<string>());
            }
        }

        public static string GetEspnPlayerName(string yahooPlayerName)
        {
            return PlayersNames.ContainsKey(yahooPlayerName) ? PlayersNames[yahooPlayerName] : yahooPlayerName;
        }
    }
}

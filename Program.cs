using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace CreateEspnDBFile
{
    class Program
    {
        static void Main(string[] args)
        {
            int[] playerIds;
            Console.WriteLine($"DB Path = {DBMethods.GetDataBaseConnectionString()}");
            if (ConfigurationManager.AppSettings["updateSpecificPlayers"].ToBool())
            {
                playerIds = ConfigurationManager.AppSettings["specificPlayersIds"].
                    Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.ToInt()).ToArray();
            }
            else
            {
                playerIds = File.ReadAllLines("Teams.txt").AsParallel().WithDegreeOfParallelism(32)
                    .SelectMany(GetActivePlayersIds).OrderBy(i => i).ToArray();
            }
            Console.WriteLine($"Found {playerIds.Length} PlayerIds");

            if (ConfigurationManager.AppSettings["runInParallel"].ToBool())
                UpdatePlayersInParallel(playerIds);
            else
                UpdatePlayers(playerIds);

            if (ConfigurationManager.AppSettings["updateRostersPlayers"].ToBool())
            {
                Stopwatch sw = Stopwatch.StartNew();
                var yahooTeams = YahooLeague.GetYahooTeams();
                DBMethods.UpdateYahooTeams(yahooTeams);
                Console.WriteLine($"Total Runtime: {sw.Elapsed}");
            }
        }

        static List<int> GetActivePlayersIds(string teamUrl)
        {
            Console.WriteLine("Extract PlayerIds From " + teamUrl);
            HtmlDocument doc = new HtmlWeb().Load(teamUrl);
            var linkedPages = doc.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !String.IsNullOrEmpty(u) && u.Contains(@"https://www.espn.com/nba/player/_/id/"))
                .ToList();
            return linkedPages.Select(s => Regex.Match(s, @"\d+").Value.ToInt()).Distinct().ToList();
        }

        private static void UpdatePlayersInParallel(int[] playerIds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int counter = 1;
            var players = playerIds.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(64).Select(id =>
            {
                Console.Title = $"{counter++}/{playerIds.Length}";
                return new PlayerInfo(id);
            }).ToArray();
            DBMethods.UpdatePlayersGames(players);
            Console.WriteLine($"Total Runtime: {sw.Elapsed}");
        }

        private static void UpdatePlayers(int[] playerIds)
        {
            int counter = 1;
            foreach (int playerId in playerIds)
            {
                try
                {
                    Console.Title = $"{counter}/{playerIds.Length}";
                    Console.WriteLine($"Start Create Player Id {playerId} ({counter++}/{playerIds.Length})");
                    if (!ConfigurationManager.AppSettings["updateExistPlayer"].ToBool() && DBMethods.IsPlayerExist(playerId))
                    {
                        Console.WriteLine("Already Exist In DB");
                        continue;
                    }

                    var player = new PlayerInfo(playerId);
                    if (player.Valid)
                        DBMethods.AddNewPlayer(player);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    File.AppendAllLines("Errors.txt", new[] { $"{playerId}" });
                }
            }
        }

        private static string[] GetTeamPlayersFromYahoo(int teamNumber)
        {
            HashSet<string> players = new HashSet<string>();
            using var client = new HttpClient();
            var teamUrl = ConfigurationManager.AppSettings["yahooTeamsUrl"].Replace("{teamId}", teamNumber.ToString());
            var teamStr = client.GetStringAsync(teamUrl).Result;

            int i1 = teamStr.IndexOf(@"Nowrap name F-link");
            while (i1 != -1)
            {
                int i2 = teamStr.IndexOf(@"</a>", i1);
                string playerName = teamStr.Substring(i1 + 85, i2 - i1 - 85);
                players.Add(playerName);
                i1 = teamStr.IndexOf(@"Nowrap name F-link", i2);
            }
            return players.ToArray();
        }
    }

}

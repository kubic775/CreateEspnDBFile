using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CreateEspnDBFile
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"DB Path = {DBMethods.GetDataBaseConnectionString()}");
            //var playerIds = File.ReadAllLines("Teams.txt").SelectMany(GetActivePlayersIds).OrderBy(i => i).ToArray();
            var playerIds = File.ReadAllText("playerIds.csv").Split(',').Select(s => s.ToInt()).ToArray();
            File.WriteAllText("playerIds.csv", string.Join(',', playerIds));
            Console.WriteLine($"Found {playerIds.Length} PlayerIds");

            int counter = 1;
            foreach (int playerId in playerIds)
            {
                try
                {
                    Console.Title = $"{counter}/{playerIds.Length}";
                    Console.WriteLine($"Start Create Player Id {playerId} ({counter++}/{playerIds.Length})");
                    if (!ConfigurationManager.AppSettings["UpdateOnlyLastYearGames"].ToBool() && DBMethods.IsPlayerExist(playerId))
                    {
                        Console.WriteLine("Already Exist In DB");
                        continue;
                    }

                    var player = new PlayerInfo(playerId);
                    DBMethods.AddNewPlayer(player);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    File.AppendAllLines("Errors.txt", new[] { $"{playerId}" });
                }
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
    }

}

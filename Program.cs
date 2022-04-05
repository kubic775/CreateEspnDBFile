using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using CreateEspnDBFile.Models;
using HtmlAgilityPack;

namespace CreateEspnDBFile
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args != null && args.Length > 0)
                    RunUpdate(args[0]);
                else
                    RunUpdate();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.WriteLine(string.Join("", Enumerable.Repeat('-', 100)));
            Console.ReadLine();
        }

        private static void RunUpdate(string playerIdsStr = null)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} Start Update DB File");
                Stopwatch sw = Stopwatch.StartNew();
                if (playerIdsStr != null)
                {
                    if (!ValidatePlayersIds(playerIdsStr))
                    {
                        Console.WriteLine("Error - Players Ids argument not valid - only numbers and ',' are allowed");
                        return;
                    }
                    var playerIds = playerIdsStr.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(s => s.ToInt()).ToArray();
                    UpdateDBFile(playerIds);
                }
                else
                {
                    UpdateDBFile();
                }

                Console.WriteLine($"Total Runtime: {sw.Elapsed}");
            }
            catch (Exception ex)
            {
                var msg = $"Error While Run Update Timer - {ex.Message}{Environment.NewLine}{ex.InnerException?.Message}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(msg);
                File.AppendAllLines("Errors.txt", new[] { msg });
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private static bool ValidatePlayersIds(string playerIds)
        {
            return playerIds.All(c => char.IsDigit(c) || c == ',');
        }

        private static void UpdateDBFile(int[] playerIds = null)
        {
            Console.WriteLine($"DB Path = {DBMethods.GetDataBaseConnectionString()}");
            playerIds ??= File.ReadAllLines("Teams.txt").AsParallel().WithDegreeOfParallelism(32)
                .SelectMany(GetActivePlayersIds).OrderBy(i => i).ToArray();
            Console.WriteLine($"Found {playerIds.Length} PlayerIds");

            if (ConfigurationManager.AppSettings["runInParallel"].ToBool())
                UpdatePlayersInParallel(playerIds);
            else
                UpdatePlayers(playerIds);

            if (ConfigurationManager.AppSettings["updateRostersPlayers"].ToBool())
            {
                List<YahooLeagueTeam> yahooTeams = YahooLeague.GetYahooTeams();
                DBMethods.UpdateYahooTeams(yahooTeams);
                YahooLeague.UpdateYahooTeamsStats(yahooTeams.Count,
                    DateTime.ParseExact(ConfigurationManager.AppSettings["seasonStartDate"], "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }

            Console.WriteLine($"Set LastUpdateTime - {DateTime.Now}");
            DBMethods.UpdateLastUpdateTime();
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
            //Stopwatch sw = Stopwatch.StartNew();
            int counter = 1;
            var players = playerIds.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(Environment.ProcessorCount).Select(id =>
              {
                  Console.Title = $"{counter++}/{playerIds.Length}";
                  return new PlayerInfo(id);
              }).ToArray();
            Console.Title = "Update DB...";
            DBMethods.UpdatePlayersGames(players);
            Console.Title = "CreateEspnDBFile";
            //Console.WriteLine($"Total Runtime: {sw.Elapsed}");
        }

        private static void UpdatePlayers(int[] playerIds)
        {
            int counter = 1;
            var startTime = DateTime.Now.AddMinutes(-ConfigurationManager.AppSettings["updatePlayerMaxDelay"].ToInt());
            var notValidPlayers = new List<(long, string)>();
            foreach (int playerId in playerIds)
            {
                try
                {
                    Console.Title = $"{counter}/{playerIds.Length}";
                    Console.WriteLine($"Start Create Player Id {playerId} ({counter++}/{playerIds.Length})");
                    Player dbPlayer = DBMethods.IsPlayerExist(playerId);
                    if (dbPlayer == null)
                    {
                        var player = new PlayerInfo(playerId);
                        if (player.Valid)
                            DBMethods.AddNewPlayer(player);
                        else
                            notValidPlayers.Add((player.Player.Id, player.Player.Name));
                    }
                    else if (dbPlayer.LastUpdateTime < startTime)
                    {
                        Console.WriteLine($"Start Update Player {playerId} In DB ({dbPlayer.Name})");
                        var player = new PlayerInfo(playerId, true);
                        if (player.Valid)
                            DBMethods.UpdateExistingPlayer(player);
                        else
                            notValidPlayers.Add((player.Player.Id, player.Player.Name));
                    }
                    else
                    {
                        Console.WriteLine($"Player {playerId} Already Updated In DB ({dbPlayer.Name})");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    File.AppendAllLines("Errors.txt", new[] { $"{playerId}" });
                }
            }

            Console.WriteLine($"Found {notValidPlayers.Count } Not Valid Players:");
            foreach ((long, string) notValidPlayer in notValidPlayers)
            {
                Console.WriteLine($"{notValidPlayer.Item1},{notValidPlayer.Item2}");
            }
        }

    }

}

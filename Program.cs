using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using HtmlAgilityPack;
using Timer = System.Timers.Timer;

namespace CreateEspnDBFile
{
    class Program
    {
        static Timer _updateTimer;
        private static readonly AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

        static void Main(string[] args)
        {
            try
            {
                InitTimer();
                RunUpdateTimer(null, null);
                AutoResetEvent.WaitOne();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _updateTimer?.Dispose();
            }
        }

        private static void InitTimer()
        {
            _updateTimer = new Timer(TimeSpan
                .FromMinutes(ConfigurationManager.AppSettings["updateTimerInterval"].ToInt()).TotalMilliseconds);
            _updateTimer.Elapsed += RunUpdateTimer;
            _updateTimer.Start();
        }

        private static void RunUpdateTimer(object sender, ElapsedEventArgs e)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now} Start Update DB File");
                Stopwatch sw = Stopwatch.StartNew();
                UpdateDBFile();
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

        private static void UpdateDBFile()
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
                //Stopwatch sw = Stopwatch.StartNew();
                var yahooTeams = YahooLeague.GetYahooTeams();
                DBMethods.UpdateYahooTeams(yahooTeams);
                //Console.WriteLine($"Total Runtime: {sw.Elapsed}");
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
            var players = playerIds.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism).WithDegreeOfParallelism(64).Select(id =>
            {
                Console.Title = $"{counter++}/{playerIds.Length}";
                return new PlayerInfo(id);
            }).ToArray();
            DBMethods.UpdatePlayersGames(players);
            //Console.WriteLine($"Total Runtime: {sw.Elapsed}");
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

    }

}

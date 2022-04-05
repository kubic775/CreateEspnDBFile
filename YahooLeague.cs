using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CreateEspnDBFile.Models;
using HtmlAgilityPack;

namespace CreateEspnDBFile
{
    public static class YahooLeague
    {
        public static List<YahooLeagueTeam> GetYahooTeams()
        {
            Console.WriteLine("\nStart Download Teams From Yahoo League");

            Dictionary<int, string> yahooTeamsDic = new Dictionary<int, string>();
            List<YahooLeagueTeam> yahooTeams = new List<YahooLeagueTeam>();

            var yahooLeagueUrl = ConfigurationManager.AppSettings["YahooLeague"];
            using var client = new HttpClient();
            var leagueStr = client.GetStringAsync(yahooLeagueUrl).Result;

            var pattern = @"https://basketball.fantasysports.yahoo.com/nba/5867/";
            var pattern2 = @"</a>";
            int i1 = leagueStr.IndexOf(pattern);
            while (i1 != -1)
            {
                if (char.IsDigit(leagueStr[i1 + pattern.Length]))
                {
                    var i2 = leagueStr.IndexOf(pattern2, i1);
                    var subString = leagueStr.Substring(i1 + pattern.Length, i2 - i1 - pattern.Length);
                    var teamId = Regex.Match(subString, @"\d+").Value.ToInt();
                    var i3 = subString.IndexOf(@">");
                    var teamName = subString.Substring(i3 + 1);
                    if (!yahooTeamsDic.ContainsKey(teamId))
                        yahooTeamsDic.Add(teamId, teamName);
                }
                i1 = leagueStr.IndexOf(pattern, i1 + 1);
            }

            Console.WriteLine("Start Download Players From Yahoo League");
            yahooTeams = yahooTeamsDic.AsParallel().WithDegreeOfParallelism(yahooTeamsDic.Count).Select(t => new YahooLeagueTeam
            { Id = t.Key, Name = t.Value, PlayersNames = GetTeamPlayersFromYahoo(t.Key) }).ToList();

            return yahooTeams;
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
            return players.Select(PlayerNameConverter.GetEspnPlayerName).ToArray();
        }

        public static void UpdateYahooTeamsStats(int numOfTeams, DateTime seasonStartDate)
        {
            Console.WriteLine($"Start Update Yahoo Teams Stats");
            var totalDays = (DateTime.Now.Date - seasonStartDate.Date).TotalDays;
            List<DateTime> allDates = Enumerable.Range(0, (int)totalDays).Select(d => seasonStartDate.AddDays(d))
                .OrderByDescending(d => d).ToList();

            foreach (var currentDate in allDates)
            {
                var relevantTeams = DBMethods.GetMissingTeamStatsIds(numOfTeams, currentDate);
                if(!relevantTeams.Any()) continue;
                var teamStats = relevantTeams.Select(teamNum => GetYahooTeamStats(teamNum, currentDate)).ToList();
                DBMethods.UploadTeamStats(teamStats);
            }
        }

        private static YahooTeamStat GetYahooTeamStats(long teamNumber, DateTime date)
        {
            Task.Delay(1_000).Wait();//do not remove this line as it necessary to not being ban from Yahoo
            Console.WriteLine($"Yahoo Team Number - {teamNumber}, {date:yyyy-MM-dd}");
            var teamUrl = ConfigurationManager.AppSettings["yahooTeamsUrl"].Replace("{teamId}", teamNumber.ToString()) +
                          $"/team?&date={date:yyyy-MM-dd}";
            using var client = new HttpClient();
            var teamStatsStr = client.GetStringAsync(teamUrl).Result;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(teamStatsStr);
            List<List<string>> teamStats = doc.DocumentNode.SelectSingleNode("//table")
                .Descendants("tr")
                .Skip(1)
                .Where(tr => tr.Elements("td").Count() > 1)
                .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                .ToList();

            int numOfGames = 0;
            var relevantPos = new List<string> { "PG", "SG", "G", "SF", "PF", "F", "C", "Util" };
            foreach (List<string> playerStats in teamStats)
            {
                var pos = playerStats.First();
                if (!relevantPos.Contains(pos)) continue;

                var numOfEmpty = playerStats.Count(s => s.Equals("-"));
                var numOfZeroes = playerStats.Count(s => s.Equals("0"));
                var numOfZeroes2 = playerStats.Count(s => s.Equals("0/0"));
                if (numOfEmpty > 7 || (numOfZeroes >= 6 && numOfZeroes2 == 2))
                    continue;

                numOfGames++;
            }

            var teamStatsTotals = teamStats.Last().Skip(2).ToList();
            teamStatsTotals = teamStatsTotals.Skip(teamStatsTotals.FindIndex(s => !string.IsNullOrEmpty(s))).ToList();
            var yahooTeamStats = new YahooTeamStat
            {
                GameDate = date,
                YahooTeamId = teamNumber,
                Fgm = teamStatsTotals[0].Split("/", StringSplitOptions.RemoveEmptyEntries).First().ToInt(),
                Fga = teamStatsTotals[0].Split("/", StringSplitOptions.RemoveEmptyEntries).Last().ToInt(),
                FgPer = teamStatsTotals[1].ToDouble() ?? 0,
                Ftm = teamStatsTotals[2].Split("/", StringSplitOptions.RemoveEmptyEntries).First().ToInt(),
                Fta = teamStatsTotals[2].Split("/", StringSplitOptions.RemoveEmptyEntries).Last().ToInt(),
                FtPer = teamStatsTotals[3].ToDouble() ?? 0,
                Tpm = teamStatsTotals[4].ToInt(),
                Pts = teamStatsTotals[5].ToInt(),
                Reb = teamStatsTotals[6].ToInt(),
                Ast = teamStatsTotals[7].ToInt(),
                Stl = teamStatsTotals[8].ToInt(),
                Blk = teamStatsTotals[9].ToInt(),
                To = teamStatsTotals[10].ToInt(),
                Gp = numOfGames
            };

            Console.WriteLine(Utils.GetObjectString(yahooTeamStats));
            return yahooTeamStats;
        }
    }



    public class YahooLeagueTeam
    {
        public int Id;
        public string Name;
        public string[] PlayersNames;
    }

}

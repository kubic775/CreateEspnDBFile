using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace CreateEspnDBFile
{
    public static class YahooLeague
    {
        public static List<YahooTeam> GetYahooTeams()
        {
            Console.WriteLine("\nStart Download Teams From Yahoo League");

            Dictionary<int, string> yahooTeamsDic = new Dictionary<int, string>();
            List<YahooTeam> yahooTeams = new List<YahooTeam>();

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
                    if(!yahooTeamsDic.ContainsKey(teamId))
                        yahooTeamsDic.Add(teamId,teamName);
                }
                i1 = leagueStr.IndexOf(pattern, i1 + 1);
            }

            Console.WriteLine("Start Download Players From Yahoo League");
            yahooTeams = yahooTeamsDic.AsParallel().WithDegreeOfParallelism(yahooTeamsDic.Count).Select(t => new YahooTeam()
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
            return players.ToArray();
        }
    }

    public class YahooTeam
    {
        public int Id;
        public string Name;
        public string[] PlayersNames;
    }
}

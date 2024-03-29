﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreateEspnDBFile.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CreateEspnDBFile
{
    public class PlayerInfo
    {
        public readonly Player Player;
        public readonly List<Game> Games;
        public readonly bool Valid;

        public PlayerInfo(int id, bool updateMode = false)
        {
            try
            {
                string playerUrl = ConfigurationManager.AppSettings["playerUrl"].Replace("{id}", id.ToString());
                string playerStr = Utils.GetSourceFromURL(playerUrl);
                Player = UpdatePlayerInfo(playerStr, id);
                Console.WriteLine($"Current Player - {Player.Name}");
                Games = new List<Game>();
                CreatePlayerGames(playerUrl, ConfigurationManager.AppSettings["updateHistoryGames"].ToBool());
                Valid = true;
            }
            catch (Exception e)
            {
                Valid = false;
                Console.WriteLine(e);
                Task.Run(() => Utils.WriteTextAsync("Errors.txt", $"{id}{Environment.NewLine}"));
                //File.AppendAllLines("Errors.txt", new[] { $"{id}" });
                Player = new Player { Id = id };
            }
        }

        private Player UpdatePlayerInfo(string playerStr, int id)
        {
            string pattern = "{\"app\"";
            string pattern2 = ";</script>";
            var i1 = playerStr.IndexOf(pattern);
            var i2 = playerStr.IndexOf(pattern2, i1);
            if (i1 == -1) return null;
            string jsonStr = playerStr.Substring(i1, i2 - i1);
            JObject json = JObject.Parse(jsonStr);
            JToken playerInfo = json["page"]["content"]["player"]["plyrHdr"]["ath"];


            int.TryParse(
                playerInfo["dob"].ToString().Split("()".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last(),
                out int age);
            var player = new Player
            {
                Id = id,
                Name = playerInfo["dspNm"].ToString(),
                Team = (playerInfo["tm"] ?? "").ToString(),
                Age = age,
                Misc = $"{playerInfo["pos"]} | {playerInfo["sts"]}"
            };

            return player;
        }

        private void CreatePlayerGames(string playerUrl, bool updateHistoryGames = false)
        {
            int gamesHistoryLength = updateHistoryGames ? ConfigurationManager.AppSettings["gamesHistoryLength"].ToInt() : 0;
            var years = Enumerable.Range(Utils.GetCurrentYear() - gamesHistoryLength + 1, gamesHistoryLength + 1).Reverse();

            foreach (int year in years)
            {
                var gamesUrl = playerUrl + $"/type/nba/year/{year}";
                var gamesData = Utils.GetSourceFromURL(gamesUrl);
                int start = gamesData.IndexOf("Regular Season");
                if (start == -1) continue;
                int end = gamesData.IndexOf("Spring Regular Season");
                if (end == -1)
                    end = gamesData.IndexOf("Preseason");
                if (end == -1)
                    end = gamesData.IndexOf("Data provided by Elias Sports Bureau");
                if (end == -1)
                    end = gamesData.IndexOf("glossary");

                if (start == -1 || end == -1) continue;
                var gamesStr = gamesData.Substring(start, end - start);
                var games = CreatePlayerGames(gamesStr, year);
                Games.AddRange(games);
                Console.WriteLine($"{year} - {games.Count()} Games");
            }
        }

        private IEnumerable<Game> CreatePlayerGames(string gamesXml, int year)
        {
            var games = new List<Game>();
            int index1 = gamesXml.IndexOf("<tr");
            if (index1 == -1) return games;
            int index2 = gamesXml.IndexOf("</tr>", index1) + "</tr>".Length;
            while (index1 != -1 && index2 != -1)
            {
                var gameStr = gamesXml.Substring(index1, index2 - index1);
                gamesXml = gamesXml.Remove(0, index2 - index1);
                index1 = gamesXml.IndexOf("<tr");
                if (index1 == -1) break;
                index2 = gamesXml.IndexOf("</tr>", index1) + "</tr>".Length;
                //GetValuesFromTR(gameStr);
                var gameStats = new GameStats(gameStr, year);
                if (gameStats.GameDate != default(DateTime))
                {
                    //Console.WriteLine(game.ToString());
                    var game = JsonConvert.DeserializeObject<Game>(JsonConvert.SerializeObject(gameStats));
                    game.PlayerId = Player.Id;
                    games.Add(game);
                }
            }
            return games;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CreateEspnDBFile.Models;
using Microsoft.EntityFrameworkCore;

namespace CreateEspnDBFile
{
    //Scaffold-Dbcontext "DataSource=Z:\Dropbox\NBA fantasy\CreateEspnDBFile\espn.sqlite" Microsoft.EntityFrameworkCore.Sqlite -Context EspnDB -OutputDir Models -f
    public class DBMethods
    {
        private static long _nextGamePk = -1;
        private static long _nextTeamStatsPk = -1;
        private static Dictionary<long, string> _statusPlayers;
        private static readonly Mutex Mutex = new Mutex();

        public static string GetDataBaseConnectionString()
        {
            using var db = new EspnDB();
            return db.Database.GetDbConnection().ConnectionString;
        }

        private static long GetNextGamePk()
        {
            long pk;
            Mutex.WaitOne();
            try
            {
                if (_nextGamePk == -1)
                {
                    using var db = new EspnDB();
                    _nextGamePk = db.Games.Max(g => g.Pk) + 1;
                    pk = _nextGamePk;
                }
                else
                {
                    pk = ++_nextGamePk;
                }
            }
            catch (Exception)
            {
                _nextGamePk = 1;
                pk = 1;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
            return pk;
        }

        private static long GetNextYahooTeamPk()
        {
            using var db = new EspnDB();
            if (!db.YahooTeams.Any())
                return 1;
            else
                return db.YahooTeams.Max(t => t.Pk) + 1;
        }

        public static Player IsPlayerExist(long playerId)
        {
            using var db = new EspnDB();

            var players = db.Players.ToList();
            return players.FirstOrDefault(p => p.Id == playerId);
        }

        public static bool IsPlayerExist(Player player, Player[] dbPlayers)
        {
            return dbPlayers.Any(p => p.Id == player.Id);
        }

        public static bool IsGameExist(long playerId, DateTime gameDate)
        {
            using var db = new EspnDB();
            return db.Games.Any(g => g.PlayerId == playerId && g.GameDate.Date.Equals(gameDate.Date));
        }

        public static bool IsGameExist(Game newGame, Game[] allGames)
        {
            return allGames.Where(g => g.PlayerId == newGame.PlayerId).Any(g => g.GameDate.Date.Equals(newGame.GameDate.Date));
        }

        public static void AddNewPlayer(PlayerInfo player)
        {
            using var db = new EspnDB();
            player.Player.LastUpdateTime = DateTime.Now;
            db.Players.Add(player.Player);
            db.SaveChanges();

            foreach (Game game in player.Games)
            {
                if (IsGameExist(player.Player.Id, game.GameDate))
                    continue;
                game.Pk = GetNextGamePk();
                db.Games.Add(game);
            }
            db.SaveChanges();
            Console.WriteLine($"Player {player.Player.Name} Uploaded To DB");
        }

        public static void UpdateExistingPlayer(PlayerInfo player)
        {
            using var db = new EspnDB();
            //var dbPlayer = db.Players.First(p => p.Id == player.Player.Id);
            //dbPlayer.Age = player.Player.Age;
            //dbPlayer.Misc = player.Player.Misc;
            //dbPlayer.Team = player.Player.Team;
            //dbPlayer.LastUpdateTime = DateTime.Now;

            var lastPlayerGame = player.Games.FirstOrDefault();
            if (lastPlayerGame != null)
            {
                var lastDbGame = db.Games.FirstOrDefault(g => g.PlayerId == player.Player.Id && g.GameDate.Date.Equals(lastPlayerGame.GameDate.Date));
                if (lastDbGame != null)
                    db.Games.Remove(lastDbGame);

                db.SaveChanges();
            }

            var dbGamesDate = db.Games.Where(g => g.PlayerId == player.Player.Id).Select(g => g.GameDate.Date).ToList();
            int updatedGames = 0;
            foreach (Game game in player.Games)
            {
                if (dbGamesDate.Contains(game.GameDate.Date))
                    continue;
                game.Pk = GetNextGamePk();
                db.Games.Add(game);
                updatedGames++;
            }
            db.SaveChanges();
            Console.WriteLine($"Player {player.Player.Name} Updated In DB ({updatedGames} Games)");
        }

        public static void UpdatePlayersGames(PlayerInfo[] players)
        {
            GetStatusPlayers();
            TruncateTables();

            players = FilterPlayersNotValid(players);
            using var db = new EspnDB();

            var dbPlayers = db.Players.ToArray();
            var newPlayers = players.Where(p => !IsPlayerExist(p.Player, dbPlayers) && p.Valid).Select(p => p.Player).ToArray();
            if (newPlayers.Any())
            {
                db.Players.AddRange(newPlayers);
                db.SaveChanges();
                Console.WriteLine($"{newPlayers.Length} New Players Uploaded To DB");
            }

            if (_statusPlayers != null && _statusPlayers.Count > 0)
            {
                Console.WriteLine("Start Update Status Players In DB");
                foreach (KeyValuePair<long, string> player in _statusPlayers)
                {
                    var dbPlayer = db.Players.FirstOrDefault(p => p.Id == player.Key);
                    if (dbPlayer == null) continue;
                    dbPlayer.Status = player.Value;
                    Console.WriteLine($"Set {dbPlayer.Name} To {dbPlayer.Status}");
                }
                db.SaveChanges();
            }

            var dbGames = db.Games.ToArray();
            Console.WriteLine($"Found {dbGames.Length} Games in DB, Search For New Games");
            var playerGames = players.SelectMany(p => p.Games).ToArray();
            var newGames = playerGames.Where(g => !IsGameExist(g, dbGames)).ToArray();
            Console.WriteLine($"Found {newGames.Length} New Games, Start Upload To DB");
            foreach (Game newGame in newGames)
            {
                newGame.Pk = GetNextGamePk();
                db.Games.Add(newGame);
            }

            db.SaveChanges();
            Console.WriteLine($"{newGames.Length} New Games Uploaded To DB");
        }

        private static PlayerInfo[] FilterPlayersNotValid(PlayerInfo[] players)
        {
            var badPlayers = players.Where(p => !p.Valid).ToArray();
            Console.WriteLine($"Found {badPlayers.Length} Not Valid Players:");
            foreach (PlayerInfo playerInfo in badPlayers)
            {
                Console.WriteLine(playerInfo.Player.Name);
                File.AppendAllText("badPlayers.txt", playerInfo.Player.Name + Environment.NewLine);
            }

            return players.Where(p => p.Valid).ToArray();
        }

        public static void UpdateYahooTeams(List<YahooLeagueTeam> teams)
        {
            Console.WriteLine("Start Update Yahoo Teams And Players In DB");
            using var db = new EspnDB();
            int rows = db.Database.ExecuteSqlRaw("UPDATE Players SET TeamNumber=null");
            db.SaveChanges();
            foreach (var team in teams)
            {
                var dbTeam = db.YahooTeams.FirstOrDefault(t => t.TeamId == team.Id);
                if (dbTeam == null) //add new team
                    db.YahooTeams.Add(new YahooTeam { Pk = GetNextYahooTeamPk(), TeamId = team.Id, TeamName = team.Name });
                else //update team
                    dbTeam.TeamName = team.Name;

                var currentTeamPlayers = db.Players.Where(p => team.PlayersNames.Contains(p.Name)).ToList();
                foreach (Player player in currentTeamPlayers)
                {
                    player.TeamNumber = team.Id;
                }
                db.SaveChanges();
            }
            Console.WriteLine("Done");
        }

        public static void UpdateLastUpdateTime()
        {
            using var db = new EspnDB();
            if (!db.GlobalParams.Any())
                db.GlobalParams.Add(new GlobalParam { Pk = 1, LastUpdateTime = DateTime.Now });
            else
                db.GlobalParams.First().LastUpdateTime = DateTime.Now;

            db.SaveChanges();
        }

        private static void GetStatusPlayers()
        {
            using var db = new EspnDB();
            _statusPlayers = db.Players.Where(p => !string.IsNullOrEmpty(p.Status))
                .ToDictionary(key => key.Id, val => val.Status);
        }

        private static void TruncateTables()
        {
            Console.WriteLine("Start Truncate Tables");
            string[] tableNames = { "Games", "YahooTeams", "Players" };
            using var db = new EspnDB();
            foreach (string tableName in tableNames)
            {
                int rows = db.Database.ExecuteSqlRaw($"DELETE FROM {tableName}");//syntax for SQLite 
                db.SaveChanges();
            }
            db.Database.ExecuteSqlRaw("VACUUM");//syntax for SQLite 
            db.SaveChanges();
        }

        #region YahooTeamStats
        private static long GetNextTeamStatsPk()
        {
            long pk;
            Mutex.WaitOne();
            try
            {
                if (_nextTeamStatsPk == -1)
                {
                    using var db = new EspnDB();
                    _nextTeamStatsPk = !db.YahooTeamStats.Any() ? 1 : db.YahooTeamStats.Max(g => g.Pk) + 1;
                    pk = _nextTeamStatsPk;
                }
                else
                {
                    pk = ++_nextTeamStatsPk;
                }
            }
            catch (Exception)
            {
                pk = _nextTeamStatsPk = 1;
            }
            finally
            {
                Mutex.ReleaseMutex();
            }
            return pk;
        }

        public static long[] GetMissingTeamStatsIds(int numOfTeams, DateTime date)
        {
            using var db = new EspnDB();
            long[] existTeamsIds = db.YahooTeamStats.Where(t => t.GameDate == date).Select(t => t.YahooTeamId.Value).ToArray();
            return Enumerable.Range(1, numOfTeams).Select(Convert.ToInt64).Where(i => !existTeamsIds.Contains(i)).ToArray();
        }

        public static Dictionary<long, DateTime> GetLastTeamStatDate()
        {
            using var db = new EspnDB();
            Dictionary<long, DateTime> teamStats = db.YahooTeamStats.ToList().GroupBy(team => team.YahooTeamId.Value)
                .ToDictionary(key => key.Key, val => val.Select(g => g.GameDate).OrderByDescending(d => d).First());
            return teamStats;
        }

        public static void UploadTeamStats(IEnumerable<YahooTeamStat> teamStats)
        {
            using var db = new EspnDB();
            foreach (var teamStat in teamStats)
            {
                var dbTeamStats = db.YahooTeamStats.FirstOrDefault(t =>
                    t.YahooTeamId == teamStat.YahooTeamId && t.GameDate == teamStat.GameDate);
                if (dbTeamStats == null)
                {
                    teamStat.Pk = GetNextTeamStatsPk();
                    db.YahooTeamStats.Add(teamStat);
                }
                else
                {
                    dbTeamStats.Ast = teamStat.Ast;
                    dbTeamStats.Reb = teamStat.Reb;
                    dbTeamStats.Blk = teamStat.Blk;
                    dbTeamStats.Pts = teamStat.Pts;
                    dbTeamStats.Tpm = teamStat.Tpm;
                    dbTeamStats.Stl = teamStat.Stl;
                    dbTeamStats.To = teamStat.To;
                    dbTeamStats.Fgm = teamStat.Fgm;
                    dbTeamStats.Fga = teamStat.Fga;
                    dbTeamStats.FgPer = teamStat.FgPer;
                    dbTeamStats.Ftm = teamStat.Ftm;
                    dbTeamStats.Fta = teamStat.Fta;
                    dbTeamStats.FtPer = teamStat.FtPer;
                }
            }

            db.SaveChanges();
        }

        #endregion
    }
}

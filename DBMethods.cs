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
    public class DBMethods
    {
        private static long _nextGamePk = -1;
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

        public static bool IsPlayerExist(long playerId)
        {
            using var db = new EspnDB();

            var players = db.Players.ToList();
            return players.Any(p => p.Id == playerId);
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
            if (!IsPlayerExist(player.Player.Id))
            {
                db.Players.Add(player.Player);
                db.SaveChanges();
            }
            else
            {
                if (!ConfigurationManager.AppSettings["updateExistPlayer"].ToBool())
                    return;
            }

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

        public static void UpdatePlayersGames(PlayerInfo[] players)
        {
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
            foreach (var player in badPlayers)
            {
                Console.WriteLine(player.ToString());
                File.AppendAllText("badPlayers.txt", player + Environment.NewLine);
            }

            return players.Where(p => p.Valid).ToArray();
        }
    }
}

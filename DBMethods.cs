﻿using System;
using System.Collections.Generic;
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

        public static void AddNewPlayer(PlayerInfo player)
        {
            //if (!db.Players.Any(p => p.Id == player.Player.Id))
            if (IsPlayerExist(player.Player.Id))
                return;

            using var db = new EspnDB();
            db.Players.Add(player.Player);
            db.SaveChanges();

            foreach (Game game in player.Games)
            {
                game.Pk = GetNextGamePk();
                db.Games.Add(game);
            }
            db.SaveChanges();
            Console.WriteLine($"Player {player.Player.Name} Uploaded To DB");
        }
    }
}
﻿using System;
using System.Collections.Generic;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class Game
    {
        public long Pk { get; set; }
        public long? PlayerId { get; set; }
        public DateTime GameDate { get; set; }
        public long? Gp { get; set; }
        public double? Pts { get; set; }
        public double? Reb { get; set; }
        public double? Ast { get; set; }
        public double? Tpm { get; set; }
        public double? Tpa { get; set; }
        public double? Fga { get; set; }
        public double? Fgm { get; set; }
        public double? Fta { get; set; }
        public double? Ftm { get; set; }
        public double? Stl { get; set; }
        public double? Blk { get; set; }
        public double? To { get; set; }
        public double? Min { get; set; }
        public double? Pf { get; set; }
        public double? FtPer { get; set; }
        public double? FgPer { get; set; }
        public double? TpPer { get; set; }
        public double? Score { get; set; }
        public string Opp { get; set; }
    }
}

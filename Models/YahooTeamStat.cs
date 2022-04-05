using System;
using System.Collections.Generic;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class YahooTeamStat
    {
        public long Pk { get; set; }
        public DateTime GameDate { get; set; }
        public long? YahooTeamId { get; set; }
        public long? Pts { get; set; }
        public long? Reb { get; set; }
        public long? Ast { get; set; }
        public long? Tpm { get; set; }
        public long? Fga { get; set; }
        public long? Fgm { get; set; }
        public double? FgPer { get; set; }
        public long? Fta { get; set; }
        public long? Ftm { get; set; }
        public double? FtPer { get; set; }
        public long? Stl { get; set; }
        public long? Blk { get; set; }
        public long? To { get; set; }
        public long? Gp { get; set; }
    }
}

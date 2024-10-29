using System;
using System.Collections.Generic;

namespace CreateEspnDBFile.Models;

public partial class YahooTeamStat
{
    public int Pk { get; set; }

    public DateTime GameDate { get; set; }

    public int? YahooTeamId { get; set; }

    public int? Pts { get; set; }

    public int? Reb { get; set; }

    public int? Ast { get; set; }

    public int? Tpm { get; set; }

    public int? Fga { get; set; }

    public int? Fgm { get; set; }

    public double? FgPer { get; set; }

    public int? Fta { get; set; }

    public int? Ftm { get; set; }

    public double? FtPer { get; set; }

    public int? Stl { get; set; }

    public int? Blk { get; set; }

    public int? To { get; set; }

    public int? Gp { get; set; }
}

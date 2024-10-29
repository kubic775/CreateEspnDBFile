using System;
using System.Collections.Generic;

namespace CreateEspnDBFile.Models;

public partial class LeagueTeam
{
    public int Pk { get; set; }

    public string Name { get; set; }

    public string Abbreviation { get; set; }

    public int? TeamId { get; set; }
}

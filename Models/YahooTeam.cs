using System;
using System.Collections.Generic;

namespace CreateEspnDBFile.Models;

public partial class YahooTeam
{
    public int Pk { get; set; }

    public int TeamId { get; set; }

    public string TeamName { get; set; }
}

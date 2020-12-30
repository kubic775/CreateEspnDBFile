using System;
using System.Collections.Generic;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class LeagueTeam
    {
        public long Pk { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
        public long? TeamId { get; set; }
    }
}

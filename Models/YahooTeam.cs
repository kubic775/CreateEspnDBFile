using System;
using System.Collections.Generic;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class YahooTeam
    {
        public long Pk { get; set; }
        public long TeamId { get; set; }
        public string TeamName { get; set; }
    }
}

using System;
using System.Collections.Generic;

#nullable disable

namespace CreateEspnDBFile.Models
{
    public partial class Player
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public long? Age { get; set; }
        public string Misc { get; set; }
        public long? TeamNumber { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdateTime { get; set; }
    }
}

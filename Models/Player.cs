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
        public long? Type { get; set; }

        public override string ToString()
        {
            return $"{Name}, {Team}, {Id}";
        }
    }
}

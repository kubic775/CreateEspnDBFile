using System;
using System.Collections.Generic;

namespace CreateEspnDBFile.Models;

public partial class Player
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Team { get; set; }

    public int? Age { get; set; }

    public string Misc { get; set; }

    public int? TeamNumber { get; set; }

    public string Status { get; set; }

    public DateTime? LastUpdateTime { get; set; }
}

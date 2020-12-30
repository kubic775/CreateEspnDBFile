using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace CreateEspnDBFile
{
    public class GameStats
    {
        public DateTime GameDate;
        public double Pts, Reb, Ast, Tpm, Tpa, Fga, Fgm, Ftm, Fta, Stl, Blk, To, Min, Pf;
        public double FtPer, FgPer, TpPer;
        public double Score;
        public string Opp, Result;
        public int Gp;

        public GameStats(string gameXml, int year)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(gameXml);
            var val = xmlDoc.FirstChild.ChildNodes.OfType<XmlNode>().Select(node => node.InnerText).ToList();
            //Console.WriteLine(string.Join(",", val));
            var dateInfo = val[0].Remove(0, 4).Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (dateInfo.Length != 2) return;
            var month = dateInfo[0].ToInt();
            var day = dateInfo[1].ToInt();
            if (month == 0 || day == 0) return;
            GameDate = new DateTime(month < 10 ? year : year - 1, month, day);
            Opp = new string(val[1].Where(c => Char.IsLetter(c) && Char.IsUpper(c)).ToArray());
            Result = val[2];
            Min = val[3].ToInt();
            Fga = val[4].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last().ToInt();
            Fgm = val[4].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).First().ToInt();
            FgPer = Fgm / Fga;
            FgPer = double.IsNaN(FgPer) ? 0 : Math.Round(FgPer, 3);
            Tpa = val[6].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last().ToInt();
            Tpm = val[6].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).First().ToInt();
            TpPer = Tpm / Tpa;
            TpPer = double.IsNaN(TpPer) ? 0 : Math.Round(TpPer, 3);
            Fta = val[8].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Last().ToInt();
            Ftm = val[8].Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).First().ToInt();
            FtPer = Ftm / Fta;
            FtPer = double.IsNaN(FtPer) ? 0 : Math.Round(FtPer, 3);
            Reb = val[10].ToInt();
            Ast = val[11].ToInt();
            Blk = val[12].ToInt();
            Stl = val[13].ToInt();
            Pf = val[14].ToInt();
            To = val[15].ToInt();
            Pts = val[16].ToInt();
        }

        public string ToShortString()
        {
            IEnumerable<FieldInfo> fieldNames = typeof(GameStats).GetFields().Where(f => f.FieldType == typeof(double));
            return string.Join(",", fieldNames.Select(f => (double)f.GetValue(this)).ToArray());
        }

        public override string ToString()
        {
            return $"Pts:{Pts:0.0}, Reb:{Reb:0.0}, Ast:{Ast:0.0}, Tpm:{Tpm:0.0}, Stl:{Stl:0.0}, Blk:{Blk:0.0}, To:{To:0.0}, " +
                   $"FgPer:{Fgm:0.0}/{Fga:0.0}({FgPer:0.0}%), FtPer:{Ftm:0.0}/{Fta:0.0}({FtPer:0.0}%), Min:{Min:0.0}, Gp:{Gp}";
        }

        //Date
        // OPP
        // Result
        // MIN
        // FG
        // FG%
        // 3PT
        // 3P%
        // FT
        // FT%
        // REB
        // AST
        // BLK
        // STL
        // PF
        // TO
        // PTS
        //Wed 10/23,@DAL,L108-100,32,7-25,28.0,1-11,9.1,4-6,66.7,6,9,0,3,4,2,19
    }

}

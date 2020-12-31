using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace CreateEspnDBFile
{
    public static class Utils
    {
        public static int GetCurrentYear()
        {
            return DateTime.Now.Month >= 10 ? DateTime.Now.Year : DateTime.Now.Year - 1;
        }

        public static string GetSourceFromURL(string url)
        {
            using (var client = new HttpClient())
            {
                using (HttpResponseMessage response = client.GetAsync(url).Result)
                {
                    using (HttpContent content = response.Content)
                    {
                        return content.ReadAsStringAsync().Result;
                    }
                }
            }
        }
    }

    public static class StringExtensions
    {
        public static int ToInt(this string str)
        {
            int.TryParse(str, out int num);
            return num;
        }

        public static bool ToBool(this string str)
        {
            return str.ToLower().Equals("true") || str.Equals("1");
        }
    }


}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
            using var client = new HttpClient();
            return client.GetStringAsync(url).Result;
            //using var wc = new WebClient();
            //return wc.DownloadString(url);
        }

        public static async Task WriteTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            await using FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true);
            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
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

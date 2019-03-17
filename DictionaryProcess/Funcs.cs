using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace DictionaryProcess
{
    public class Funcs
    {
        private static readonly Dictionary<string, string> PascalCaseDictionary = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> TranslateDictionary = new Dictionary<string, string>();

        private static readonly Dictionary<string, string> CheckDuplicateCHS = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> CheckDuplicateCommon = new Dictionary<string, string>();

        public static string AddComments(string xml) => $"<!-- {xml} -->";

        public static string MakeChsItem(string name, string comment = "")
        {
            var pascalCase = PascalCase(name);
            var key = pascalCase.ToLower();
            if (CheckDuplicateCHS.ContainsKey(key))
            {
                return AddComments(string.Join(Environment.NewLine,
                    $"duplicate in chs : {pascalCase} with {name} origin {CheckDuplicateCHS[key]}",
                    (MakeItem(pascalCase, name, comment))));
            }

            CheckDuplicateCHS.Add(key, name);
            return MakeItem(pascalCase, name, comment);
        }

        public static string MakeCommonItem(string name, string comment = "")
        {
            var pascalCase = PascalCase(name);
            var key = pascalCase.ToLower();
            if (CheckDuplicateCommon.ContainsKey(key))
            {
                return AddComments(string.Join(Environment.NewLine,
                    $"duplicate in common : {pascalCase} with {name} origin {CheckDuplicateCommon[key]}",
                    MakeItem(pascalCase, Translate(name), comment)));
            }

            CheckDuplicateCommon.Add(key, name);
            return MakeItem(pascalCase, Translate(name), comment);
        }

        public static string MakeItem(string key, string val, string comment = "") =>
            $"<data name=\"{key}\" xml:space=\"preserve\">"
            + $"<value>{val}</value>"
            + $"<comment>{comment}</comment>"
            + "</data>";

        public static string PascalCase(string key)
        {
            if (PascalCaseDictionary.ContainsKey(key)) return PascalCaseDictionary[key];
            key = key?.Trim("@,?.|\\/".ToCharArray());
            var pascalCase = HttpGet(string.Format("http://localhost/Translator/api/PascalCase?key={0}", key))
                .Trim('"', '.', '-', '+', ',', '\'', '?')
                .Replace("'", string.Empty);
            PascalCaseDictionary.Add(key, pascalCase);
            return pascalCase;
        }

        public static string Translate(string key)
        {
            if (TranslateDictionary.ContainsKey(key)) return PascalCaseDictionary[key];
            key = key?.Trim("@,?.|\\/".ToCharArray());
            var pascalCase = HttpGet(string.Format("http://localhost/Translator/api/Translate?key={0}", key))
                .Trim('"', '.', '-', '+', ',', '\'', '?')
                .Replace("'", string.Empty);
            TranslateDictionary.Add(key, pascalCase);
            return pascalCase;
        }

        private static HttpWebResponse CreateGetHttpResponse(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            return request.GetResponse() as HttpWebResponse;
        }

        private static string HttpGet(string url)
        {
            try
            {
                Stream responseStream = CreateGetHttpResponse(url).GetResponseStream();
                string buf = string.Empty;
                if (responseStream != null)
                {
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("UTF-8")))
                    {
                        buf = reader.ReadToEnd();
                    }

                    responseStream.Close();
                }

                return buf;
            }
            catch (Exception e)
            {
                File.WriteAllText(
                    $"D:\\log\\Generator_Err_{DateTime.Now.ToString("yyyy MMMM dd")}_{DateTime.Now.Ticks}.log",
                    JsonConvert.SerializeObject(new
                    {
                        project = "Generator",
                        method = nameof(HttpGet),
                        url,
                        e
                    }));
                return string.Empty;
            }
        }

        public static void Show(IEnumerable<string> chs)
        {
            Console.WriteLine(string.Join(Environment.NewLine, chs));
        }
    }
}
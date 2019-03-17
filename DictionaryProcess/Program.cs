using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static DictionaryProcess.Funcs;

namespace DictionaryProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            var common_resx_path =
                @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Languages\MobilePortal.resx";
            var chs_resx_path =
                @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Languages\MobilePortal.zh-CN.resx";
            var text = File.ReadAllText(@"D:\big_dictionary.txt");
            var chs = new HashSet<string>(Regex.Matches(text, "\\\"[\u4e00-\u9fa5]+([!！。.,，：《》【】()（）{}、/\\|@$￥0-9\\-a-zA-Z]*[\u4e00-\u9fa5]*)+\\\"")
                .Select(m => m.Value))
                .OrderByDescending(k => k.Length);
            Console.WriteLine(string.Concat(Enumerable.Repeat("-", 100)));

            Show(chs.Take(1050));
            Console.ReadLine();


            File.WriteAllText(common_resx_path,
                File.ReadAllText(common_resx_path).Replace("</root>",
                    string.Join(Environment.NewLine, chs.Select(c => MakeCommonItem(c, c)))
                    + "</root>"));
            File.WriteAllText(chs_resx_path,
                File.ReadAllText(chs_resx_path).Replace("</root>",
                    string.Join(Environment.NewLine, chs.Select(c => MakeChsItem(c, c)))
                    + "</root>"));

            Console.WriteLine(string.Concat(Enumerable.Repeat("-", 100)));
            var folder = @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Mobile\";
            var cshtmls = Directory.GetFiles(folder, "*.cshtml", SearchOption.AllDirectories);
            foreach (string cshtml in cshtmls)
            {
                File.WriteAllLines(cshtml, File.ReadAllLines(cshtml).Select(ParseCshtmlLine));
            }
        }

        private static string ParseCshtmlLine(string line)
        {
            var target = line;
//            var matches = Regex.Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var matches = Regex.Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var enumerable = matches as string[] ?? matches.ToArray();
            foreach (string match in enumerable)
            {
                var matchIndex = line.IndexOf(match, StringComparison.Ordinal);
                var doubleQuotationIndex = line.IndexOf("\"", StringComparison.Ordinal);
                var doubleSlashIndex = line.IndexOf("//", StringComparison.Ordinal);
                var blockSlashIndex = line.IndexOf("/*", StringComparison.Ordinal);
                var blockAtIndex = line.IndexOf("@*", StringComparison.Ordinal);
                if (matchIndex != -1 && (
                        false
//                        || doubleQuotationIndex != -1 && doubleQuotationIndex < matchIndex
                        || doubleSlashIndex != -1 && doubleSlashIndex < matchIndex
                        || blockSlashIndex != -1 && blockSlashIndex < matchIndex
                        || blockAtIndex != -1 && blockAtIndex < matchIndex
                    ))
                {
                    continue;
                }
                target = target.Replace(match, $"@(AgileBoost.Gongyila.Mobile.App_GlobalResources.MobilePortal.{PascalCase(match)})");
            }
            return target;
        }
    }
}
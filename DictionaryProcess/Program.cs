using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NetDiff;
using static System.String;
using static System.Text.RegularExpressions.Regex;
using static DictionaryProcess.Funcs;

namespace DictionaryProcess
{
    class Program
    {
        private static string Show(string input)
        {
            Console.WriteLine(input);
            return input;
        }

        static void Main(string[] args)
        {
            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"begin.");
            var folder = @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Mobile\";
//            folder = @"E:\source\repos\GongYi.La\src\AgileBoost.Cishanla.Service.Implement\";
            folder = @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Mobile\Scripts";
            var common_resx_path =
                @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Languages\MobilePortal.resx";
            var chs_resx_path =
                @"E:\source\repos\GongYi.La\src\AgileBoost.Gongyila.Languages\MobilePortal.zh-CN.resx";

            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Reading dictionary.");

            //            var chs = GetChsByDictionaryFile();

            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Writing js files.");
            Directory.GetFiles(folder, "*.js", SearchOption.AllDirectories)
                .Where(t=>!t.Contains("node_modules"))
                .AsParallel().ForAll(
                path =>
                {
                    Console.WriteLine($"Writing cs files {path}");
                    var input = File.ReadAllLines(path);
                    var contents = Join(Environment.NewLine, input.Select(ParseJsLine));
                    var originalLength = Join(Environment.NewLine, input).Length;
                    var targetLength = contents.Length;
                    if (originalLength != targetLength) File.WriteAllText(path, contents);
                });

            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Writing cshtml files.");
            var cshtmls = Directory.GetFiles(folder, "*.cshtml", SearchOption.AllDirectories);
            foreach (string path in cshtmls)
            {
                Console.WriteLine($"Writing cshtml files {path}");
                var input = File.ReadAllLines(path);
                var contents = Join(Environment.NewLine, input.Select(ParseCshtmlLine));
                File.WriteAllText(path, contents);
            }

            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Writing cs files.");
            Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
                .Where(t => !t.Contains("ViewModel"))
                .AsParallel().ForAll(
                path =>
                {

                    Console.WriteLine($"Writing cs files {path}");
                    var input = File.ReadAllLines(path);
                    var contents = Join(Environment.NewLine, input.Select(ParseCsLine));
                    var originalLength = Join(Environment.NewLine, input).Length;
                    var targetLength = contents.Length;
                    if (originalLength != targetLength) File.WriteAllText(path, contents);
                });
            ProcessResxFile(common_resx_path, chsSet.Select(c => MakeCommonItem(c, c)));
            ProcessResxFile(chs_resx_path, chsSet.Select(c => MakeChsItem(c, c)));

            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine("END !");
            Console.ReadLine();
        }

        /// <summary>
        /// 处理资源文件
        /// </summary>
        /// <param name="common_resx_path"></param>
        /// <param name="chs">标签列表</param>
        /// <returns></returns>
        private static string ProcessResxFile(string common_resx_path, IEnumerable<string> chs)
        {
            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Writing common dictionary.");
            var common_resx_text = File.ReadAllText(common_resx_path);
            Console.WriteLine($"Got common resx file {common_resx_text.Length}");
            var common_resx_replacement = common_resx_text.Replace("</root>",
                Join(Environment.NewLine, chs)
                + "</root>");
            if (!common_resx_text.Contains(common_resx_replacement))
                File.WriteAllText(common_resx_path, common_resx_replacement);
            return common_resx_replacement;
        }

        private static ParallelQuery<string> GetChsByDictionaryFile()
        {
            var text = File.ReadAllText(@"D:\big_dictionary.txt");
            Console.WriteLine(Concat(Enumerable.Repeat("-", 100)));
            Console.WriteLine($"Parsing dictionary.");

            var pattern = "\\\"[\u4e00-\u9fa5]+([!！。.,，：《》【】()（）{}、/\\|@$￥0-9\\-a-zA-Z]*[\u4e00-\u9fa5]*)+\\\"";
            var dictionary_text_lines = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"Got dictionary text lines {dictionary_text_lines.Length}");
            var chs = dictionary_text_lines.AsParallel()
                    .Select(s => Matches(s, pattern))
                    .SelectMany(m => m.Select(t => t.Value))
                    .OrderByDescending(k => k.Length)
                    .Distinct()
                ;
            return chs;
        }
        private static HashSet<string> chsSet = new HashSet<string>();

        private static string ParseCshtmlLine(string line)
        {
            var target = line;
            //            var matches = Regex.Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var matches = Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var enumerable = matches as string[] ?? matches.ToArray();
            foreach (string match in enumerable)
            {
                chsSet.Add(match);
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

                target = target.Replace(match,
                    $"@(AgileBoost.Gongyila.Mobile.App_GlobalResources.MobilePortal.{PascalCase(match)})");
            }

            return target;
        }
        private static string ParseCsLine(string line)
        {
            var target = line.ToString();
            //            var matches = Regex.Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var matches = Matches(line,
                    "\\s*=\\s*\\\"[\u4e00-\u9fa5]+([!！。.,，：《》【】()（）{}、/\\|@$￥0-9\\-a-zA-Z]*[\u4e00-\u9fa5]*)+\\\"")
                .Select(t => t.Value);
            var enumerable = (matches as string[] ?? matches.ToArray()).Select(s =>
                s.Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim('"', ' '));
            foreach (string match in enumerable)
            {
                chsSet.Add(match);
                var matchIndex = line.IndexOf(match, StringComparison.Ordinal);
                var doubleQuotationIndex = line.IndexOf("\"", StringComparison.Ordinal);
                var sharpIndex = line.IndexOf("#", StringComparison.Ordinal);
                var doubleSlashIndex = line.IndexOf("//", StringComparison.Ordinal);
                var blockSlashIndex = line.IndexOf("/*", StringComparison.Ordinal);
                var blockAtIndex = line.IndexOf("@*", StringComparison.Ordinal);
                if (matchIndex != -1 && (
                        false
                        //                        || doubleQuotationIndex != -1 && doubleQuotationIndex < matchIndex
                        || doubleSlashIndex != -1 && doubleSlashIndex < matchIndex
                        || sharpIndex != -1 && sharpIndex < matchIndex
                        || blockSlashIndex != -1 && blockSlashIndex < matchIndex
                        || blockAtIndex != -1 && blockAtIndex < matchIndex
                    ))
                {
                    continue;
                }

                target = target.Replace($"\"{match}\"",
                    $"/* Replaced by {match} */AgileBoost.Gongyila.Languages.MobilePortal.{PascalCase(match)}");

                WriteYellow(match);
                WriteRed(line);
                WriteGreen(target);
                Console.WriteLine();
            }

            return target;
        }
        private static string ParseJsLine(string line)
        {
            var target = line.ToString();
            //            var matches = Regex.Matches(line, "[\u4e00-\u9fa5]+").Select(t => t.Value);
            var matches = Matches(line,
                    "['\"][\u4e00-\u9fa5]+([!！。.,，：《》【】()（）{}、/\\|@$￥0-9\\-a-zA-Z]*[\u4e00-\u9fa5]*)+['\"]")
                .Select(t => t.Value);
            var enumerable = (matches as string[] ?? matches.ToArray()).Select(s =>
                s.Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim('"', ' ', '\''));
            foreach (string match in enumerable)
            {
                chsSet.Add(match);
                var matchIndex = line.IndexOf(match, StringComparison.Ordinal);
                var doubleQuotationIndex = line.IndexOf("\"", StringComparison.Ordinal);
                var sharpIndex = line.IndexOf("#", StringComparison.Ordinal);
                var doubleSlashIndex = line.IndexOf("//", StringComparison.Ordinal);
                var blockSlashIndex = line.IndexOf("/*", StringComparison.Ordinal);
                var blockAtIndex = line.IndexOf("@*", StringComparison.Ordinal);
                if (matchIndex != -1 && (
                        false
                        //                        || doubleQuotationIndex != -1 && doubleQuotationIndex < matchIndex
                        || doubleSlashIndex != -1 && doubleSlashIndex < matchIndex
                        || sharpIndex != -1 && sharpIndex < matchIndex
                        || blockSlashIndex != -1 && blockSlashIndex < matchIndex
                        || blockAtIndex != -1 && blockAtIndex < matchIndex
                    ))
                {
                    continue;
                }

                var newValue = $"/* Replaced by {match} */ ResourceString.{PascalCase(match)}";
                target = target
                    .Replace($"'{match}'", newValue)
                    .Replace($"\"{match}\"", newValue)
                    .Replace(match, newValue);

                WriteYellow(match);
                WriteRed(line);
                WriteGreen(target);
                Console.WriteLine();
            }

            return target;
        }

        private static void WriteGreen(string target)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.WriteLine(target);
        }

        private static void WriteRed(string target)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine(target);
        }

        private static void WriteYellow(string target)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine(target);
        }
    }
}
namespace epub_sanitizer;

using HtmlAgilityPack;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using VersOne.Epub;

internal class Program
{
    static void Main(string[] args)
    {
        try
        {
            var sw = new Stopwatch();
            var xmlParseErrors = new List<KeyValuePair<string, string>>();
            sw.Start();
            var files = GetFileNames();
            foreach (var file in files)
            {
                if (file == null) continue;

                var book = EpubReader.ReadBook(GetFilePath(file));
                var pageCountTotal = book.ReadingOrder.Count;
                
                for (int i = 1; i < pageCountTotal; i++)
                {
                    var page = book.ReadingOrder.ToArray()[i];
                    var sanitizedPage = Sanitize(page);
                    var trimmedKey = sanitizedPage.Key.Replace("Text/", string.Empty);

                    var saveFolderPath = GetSaveFolderPath();
                    var newFile = $"{saveFolderPath}\\{trimmedKey}";
                    
                    try
                    {
                        var parsedDoc = XDocument.Parse(sanitizedPage.Content, LoadOptions.PreserveWhitespace);
                        using var writer = new StreamWriter(newFile);
                        writer.Write(sanitizedPage.Content);

                    }
                    catch (Exception ex) 
                    {
                        xmlParseErrors.Add(new KeyValuePair<string, string>(trimmedKey, ex.Message));
                    }
                    RunLocalProcess(file, newFile);
                }
            }
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalSeconds} sek");
            foreach (var item in xmlParseErrors)
            {
                Console.WriteLine($"Item key:{item.Key} Error: {item.Value}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static EpubLocalTextContentFile Sanitize(EpubLocalTextContentFile file)
    {
        var htmlDocument = new HtmlDocument
        {
            OptionFixNestedTags = true
        };
        htmlDocument.LoadHtml(file.Content.ToString());

        var stringBuilder = new StringBuilder();
        foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
        {
            if (node.InnerText == "Read latest Chapters at Wuxia World . Site Only")
            {
                node.RemoveAllChildren();
            }
        }
        var links = htmlDocument.DocumentNode.SelectNodes("//link");
        if (links != null)
        {
            foreach (var link in links)
            {
                link.ParentNode.RemoveChild(link);
                _ = htmlDocument.DocumentNode.OuterHtml;
            }
        }

        var nodes = htmlDocument.DocumentNode.SelectNodes("//style");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.ParentNode.RemoveChild(node);
                _ = htmlDocument.DocumentNode.OuterHtml;
            }
        }
        var sb = stringBuilder.AppendLine(htmlDocument.DocumentNode.OuterHtml.ToString());
        var sanitizedFile = new EpubLocalTextContentFile(file.Key, file.ContentType, file.ContentMimeType, file.FilePath, sb.ToString());
        return sanitizedFile;
    }

    private static string GetFilePath(string fileName)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        //var contents = File.ReadAllLines(Directory.EnumerateFiles(directoryName, "*.epub")?.FirstOrDefault());

        return $"{directoryName}/books/{fileName}";
    }

    private static string GetSaveFolderPath()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        Directory.CreateDirectory($"{directoryName}/sanitized");

        return $"{directoryName}\\sanitized";
    }

    private static IEnumerable<string?> GetFileNames()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        var fileNames = Directory.GetFiles($"{directoryName}/books", "*.epub").Select(Path.GetFileName);

        return fileNames;
    }

    private static void RunLocalProcess(string epubFileName, string newFile)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        var fileName = $"{directoryName}/scripts/app.py -i {epubFileName} -i {newFile}";

        ProcessStartInfo start = new()
        {
            FileName = @"C:\Program Files\Python312\python.exe",
            Arguments = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        string result = "";
        using (var process = Process.Start(start)!)
        {
            result = process!.StandardOutput.ReadToEnd();
            process.Kill();
        }
        Console.WriteLine(result);
    }
    public static string StripHTML(string input)
    {

        return Regex.Replace(input, "<.*?>", String.Empty);
    }
}
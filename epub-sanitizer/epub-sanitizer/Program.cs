namespace epub_sanitizer;

using HtmlAgilityPack;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using VersOne.Epub;
using VersOne.Epub.Options;

internal class Program
{
    // Make program userfriendly (Low prio feature)
    //  - Input filepath to any location on disc containing books
    //  - Store filapath location on config file
    //      - Update filepath only if input differs 

    // Get all files in Books folder and get all book names/titles

    // Create list of filepaths

    // Create a job for each book
    //  
    //  Feature? Detection stragety of book source/type with common defects
    //  - create temp copy of book
    //  - read each page
    //  - detect defects
    //  - sanitize defect
    //  - save overwrite page

    // Add to paralelles pool

    // Run all jobs simultaneously

    // Done

    static void Main(string[] args)
    {
        try
        {
            var sw = new Stopwatch();
            sw.Start();
            var saveLocationPath = GetSaveFolderPath();
            var files = GetFileNames();

            foreach ( var file in files )
            {
                if (file == null)
                {
                    continue;
                }
                var book = EpubReader.ReadBook(GetFilePath(file));
                for (int i = 1; i < book.ReadingOrder.Count; i++)
                {
                    ReadFile(book.ReadingOrder.ToArray()[i]);
                }
            }
            sw.Stop();
            Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalSeconds} sek");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
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

        return $"{directoryName}/sanitized";
    }

    private static IEnumerable<string?> GetFileNames()
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        var fileNames = Directory.GetFiles($"{directoryName}/books", "*.epub").Select(Path.GetFileName);

        return fileNames;
    }

    private static void ReadFile(EpubLocalTextContentFile file)
    {
        Console.WriteLine(file.Content);
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

        var nodes = htmlDocument.DocumentNode.SelectNodes("//style");
        if (nodes != null)
        {
            foreach (var node in nodes)
            {
                node.ParentNode.RemoveChild(node);
                _ = htmlDocument.DocumentNode.OuterHtml;
            }
        }

        Console.WriteLine(stringBuilder.AppendLine(htmlDocument.DocumentNode.OuterHtml.ToString()));
    }

    public static string StripHTML(string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }
}
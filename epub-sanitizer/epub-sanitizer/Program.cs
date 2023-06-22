namespace epub_sanitizer;

using System.Text.RegularExpressions;
using System.Text;
using VersOne.Epub;
using HtmlAgilityPack;

internal class Program
{
    static void Main(string[] args)
    {
        EpubBook book = EpubReader.ReadBook(GetFilePath());
        for (int i = 1; i < book.ReadingOrder.Count; i++)
        {
            ReadFile(book.ReadingOrder.ToArray()[i]);
        }
    }

    private static string GetFilePath()
    {
        var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directoryName = Path.GetDirectoryName(location);
        //var contents = File.ReadAllLines(Directory.EnumerateFiles(directoryName, "*.epub")?.FirstOrDefault());

        return $"{directoryName}/Books/Would_Yo...xiaWorld.epub";
    }

    private static void ReadFile(EpubLocalTextContentFile file)
    {
        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.OptionFixNestedTags = true;
        htmlDocument.LoadHtml(file.Content.ToString());

        StringBuilder stringBuilder = new StringBuilder();
        foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//text()"))
        {

            if (node.InnerText == "Read latest Chapters at Wuxia World . Site Only")
            {
                node.RemoveAllChildren();

            }
        }

        var nodes = htmlDocument.DocumentNode.SelectNodes("//style");

        foreach (var node in nodes)
            node.ParentNode.RemoveChild(node);
        _ = htmlDocument.DocumentNode.OuterHtml;

        Console.WriteLine(stringBuilder.AppendLine(htmlDocument.DocumentNode.OuterHtml.ToString()));
    }

    public static string StripHTML(string input)
    {
        return Regex.Replace(input, "<.*?>", String.Empty);
    }
}
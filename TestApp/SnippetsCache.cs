using System;
using System.Collections.Generic;
using System.Linq;

namespace TestApp
{
    internal static class SnippetsCache
    {
        public static Dictionary<string, string> Snippets = Enumerable.Range(1, 100)
            .ToDictionary(i => $"Snippet {i}", i => i switch
            {
                1 => "Console.WriteLine(\"Hello World\");",
                2 => "var numbers = new int[] { 1, 2, 3, 4, 5 }; var sum = numbers.Sum(); Console.WriteLine(sum);",
                3 => "var list = new List<string> { \"apple\", \"banana\", \"cherry\" }; list.ForEach(Console.WriteLine);",
                4 => "var dict = new Dictionary<string, int> { { \"one\", 1 }, { \"two\", 2 } }; foreach (var kvp in dict) { Console.WriteLine($\"{kvp.Key}: {kvp.Value}\"); }",
                5 => "var date = DateTime.Now; Console.WriteLine(date.ToString(\"yyyy-MM-dd\"));",
                _ => $"Console.WriteLine(\"Hello World {i}\");"
            }, StringComparer.OrdinalIgnoreCase);
    }
}

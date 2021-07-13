namespace Fountain
{
    using System.Collections.Generic;
    using System.Linq;

    class CommandLineParser
    {
        public IReadOnlyDictionary<string, string?> Parse(IReadOnlyCollection<string> args)
        {
            // --option => ["--option"] = null
            // value => ["value"] = null
            // --key=value => ["--key"] = "value"
            var dictionary = new Dictionary<string, string?>();
            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                    continue;
                if (arg.StartsWith("--"))
                {
                    if (arg.Contains('='))
                    {
                        var pieces = arg.Split('=');
                        dictionary[pieces[0]] = string.Join('=', pieces.Skip(1));
                    }
                    else
                    {
                        dictionary[arg] = null;
                    }
                }
                else
                {
                    dictionary[arg] = null;
                }
            }
            return dictionary;
        }
    }
}
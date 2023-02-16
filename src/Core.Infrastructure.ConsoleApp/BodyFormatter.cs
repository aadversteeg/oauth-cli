using System.Collections.Generic;
using System.Linq;

namespace Core.Infrastructure.ConsoleApp
{
    public class BodyFormatter
    {
        public static string Format(IReadOnlyDictionary<string, string> formFields)
        {
            return string.Join(
                '&',
                formFields
                    .Where(kv => !string.IsNullOrEmpty(kv.Value))
                    .Select(kv => kv.Key + '=' + kv.Value));
        }
    }
}

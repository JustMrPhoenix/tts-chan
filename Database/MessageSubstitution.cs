using System.Linq;
using System.Text.RegularExpressions;

namespace TTS_Chan.Database
{
    public class MessageSubstitution
    {
        public int MessageSubstitutionId { get; set; }
        public string Pattern { get; set; }
        public string Replacement { get; set; }
        public bool IsRegex { get; set; }
        public string Comment { get; set; }
        public bool IsEnabled { get; set; }

        public static string PerformAll(string source)
        {
            var substitutions = DatabaseManager.Context.MessageSubstitutions.Where(ms => ms.IsEnabled && ms.Pattern != null).ToList();
            foreach (var messageSubstitution in substitutions.TakeWhile(_ => source.Length != 0))
            {
                source = messageSubstitution.IsRegex ? Regex.Replace(source, messageSubstitution.Pattern, messageSubstitution.Replacement) : source.Replace(messageSubstitution.Pattern, messageSubstitution.Replacement);
            }

            return source;
        }
    }
}

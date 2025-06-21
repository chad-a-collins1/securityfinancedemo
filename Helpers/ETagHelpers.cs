
namespace HighThroughputApi.Helpers
{ 
    static class ETagHelpers
    {
        public static string ToEtag(this byte[] rowVersion)
            => $"\"{Convert.ToBase64String(rowVersion)}\"";

        public static bool Matches(this HttpRequest req, string current)
            => req.Headers.IfMatch.Any(h => h == "*" || h == current);
    }
}
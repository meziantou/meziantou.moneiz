namespace Meziantou.Moneiz.Core
{
    public sealed class DatabaseConfiguration
    {
        public string? GitHubToken { get; set; }
        public string? GitHubRepository { get; set; } = "moneiz-db";
        public string? GitHubSha { get; set; }
    }
}

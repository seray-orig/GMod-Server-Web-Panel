namespace GMServerWebPanel.API.Settings
{
    public class JwtSettings
    {
        public required string Key { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public DateTime? NotBefore { get; set; }
        public int ExpiresMinutes { get; set; }
        public int ExpiresDays { get; set; }
    }
}

namespace Backend.Models
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = "ArrowInstruments";
        public string Audience { get; set; } = "ArrowInstrumentsAdmin";
        public int ExpiryHours { get; set; } = 8;
    }
}

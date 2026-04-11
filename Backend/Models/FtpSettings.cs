namespace Backend.Models
{
    public class FtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 21;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RemotePath { get; set; } = "/public_html/product_images/";
    }
}

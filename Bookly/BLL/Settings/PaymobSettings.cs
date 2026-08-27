namespace BLL.Settings
{
    public class PaymobSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string HmacSecret { get; set; } = string.Empty;
        public int IntegrationId { get; set; }
        public int WalletIntegrationId { get; set; }
        public string BaseUrl { get; set; } = "https://accept-alpha.paymob.com";
    }
}
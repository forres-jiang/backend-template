namespace My.XXX.Shared
{
    public class JwtConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }

        public string Audience { get; set; }
        /// <summary>
        /// Access token在多少分钟过期
        /// </summary>
        public int ExpiryInMinutes { get; set; }
        /// <summary>
        /// Refresh token在多少分钟过期
        /// </summary>
        public int RefreshExpiryInMinutes { get; set; }
    }
}
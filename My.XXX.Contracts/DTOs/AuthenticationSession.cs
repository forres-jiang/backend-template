namespace My.XXX.Contracts.DTOs;

public sealed class AuthenticationSession
{
    public LoginUser User { get; set; }
    public TokenPair Tokens { get; set; }
}

public sealed class TokenPair
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiryInMinutes { get; set; }
}

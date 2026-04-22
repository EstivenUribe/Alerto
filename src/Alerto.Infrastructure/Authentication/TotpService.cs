using Alerto.Application.Common.Interfaces;
using OtpNet;

namespace Alerto.Infrastructure.Authentication;

public sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        return Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
    }

    public string BuildProvisioningUri(string issuer, string username, string secret)
    {
        var url = new OtpUri(OtpType.Totp, secret, username, issuer);
        return url.ToString();
    }

    public bool ValidateCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }
}

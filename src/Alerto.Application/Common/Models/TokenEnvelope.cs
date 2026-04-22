namespace Alerto.Application.Common.Models;

public sealed record TokenEnvelope(string Token, DateTime ExpiresAtUtc);

public sealed record TwoFactorTicketPayload(Guid UserId, string Username, string Role);

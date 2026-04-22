using Alerto.Application.Common.Interfaces;
using Alerto.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace Alerto.Infrastructure.Authentication;

public sealed class MachineClientValidator : IMachineClientValidator
{
    private readonly MachineClientOptions _options;

    public MachineClientValidator(IOptions<MachineClientOptions> options)
    {
        _options = options.Value;
    }

    public bool IsValid(string clientId, string clientSecret, out MachineClientDefinition? clientDefinition)
    {
        var client = _options.Clients.SingleOrDefault(candidate =>
            string.Equals(candidate.ClientId, clientId, StringComparison.Ordinal) &&
            string.Equals(candidate.ClientSecret, clientSecret, StringComparison.Ordinal));

        if (client is null)
        {
            clientDefinition = null;
            return false;
        }

        clientDefinition = new MachineClientDefinition(client.ClientId, client.DisplayName, client.Role, client.Scope);
        return true;
    }
}

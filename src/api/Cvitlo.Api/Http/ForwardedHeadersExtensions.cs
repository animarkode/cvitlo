using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Cvitlo.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Cvitlo.Api.Http;

public static class ForwardedHeadersExtensions
{
    public static IServiceCollection AddCvitloForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ForwardedHeadersOptions>().Configure(options =>
        {
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
            var section = configuration.GetSection(ConfigurationConstants.Sections.ForwardedHeaders);
            var failures = new List<string>();

            foreach (var proxy in section.GetSection("KnownProxies").GetChildren())
            {
                if (!IPAddress.TryParse(proxy.Value, out var address))
                {
                    failures.Add($"{proxy.Path}: '{proxy.Value}' must be a valid IPv4 or IPv6 address.");
                    continue;
                }

                options.KnownProxies.Add(address);
            }

            foreach (var network in section.GetSection("KnownNetworks").GetChildren())
            {
                if (!TryParseNetwork(network.Value, out var parsedNetwork))
                {
                    failures.Add($"{network.Path}: '{network.Value}' must be a valid CIDR network "
                                 + "with a prefix length of 0–32 for IPv4 or 0–128 for IPv6.");
                    continue;
                }

                options.KnownNetworks.Add(parsedNetwork!);
            }

            if (failures.Count > 0)
            {
                throw new OptionsValidationException(Microsoft.Extensions.Options.Options.DefaultName, typeof(ForwardedHeadersOptions), failures);
            }

            // Empty framework trust lists mean trust everyone, so disable processing explicitly.
            options.ForwardedHeaders = options.KnownProxies.Count + options.KnownNetworks.Count == 0
                ? ForwardedHeaders.None
                : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        }).ValidateOnStart();

        return services;
    }

    private static bool TryParseNetwork(string? value, out IPNetwork? network)
    {
        network = null;
        var parts = value?.Split('/');
        if (parts is not { Length: 2 }
            || !IPAddress.TryParse(parts[0], out var address)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var prefixLength))
        {
            return false;
        }

        var maximumPrefixLength = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (prefixLength > maximumPrefixLength)
        {
            return false;
        }

        network = new IPNetwork(address, prefixLength);
        return true;
    }
}

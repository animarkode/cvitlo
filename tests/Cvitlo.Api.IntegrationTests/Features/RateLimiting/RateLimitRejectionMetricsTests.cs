using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Net;
using Cvitlo.Common.RateLimiting;
using FastEndpoints.Testing;
using Shouldly;
using static Cvitlo.Api.IntegrationTests.Features.RateLimiting.RateLimitingTestConstants;

namespace Cvitlo.Api.IntegrationTests.Features.RateLimiting;

// IP octets 192-193 reserved for this class.
[Collection<CollectionRateLimiting>]
[Priority(22)]
public class RateLimitRejectionMetricsTests(WebApp app) : TestBase
{
    [Fact]
    public async Task Rejection_NamedPolicy_ShouldRecordThePolicyName()
    {
        using var client = app.CreateClient(new ClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, "203.0.113.192");
        using var rejections = new RejectionRecorder();

        for (var i = 0; i < AuthExternalPermitLimit; i++)
        {
            using var allowed = await client.GetAsync(LoginUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        }

        rejections.Policies.ShouldBeEmpty();

        using var rejected = await client.GetAsync(LoginUri, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        rejections.Policies.ShouldBe([RateLimitPolicies.AuthExternal]);
    }

    [Fact]
    public async Task Rejection_GlobalLimiterOnly_ShouldRecordGlobal()
    {
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TestClientIpStartupFilter.HeaderName, "203.0.113.193");
        using var rejections = new RejectionRecorder();

        for (var i = 0; i < AnonymousPermitLimit; i++)
        {
            using var allowed = await client.GetAsync(UserUri, TestContext.Current.CancellationToken);
            allowed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        using var rejected = await client.GetAsync(UserUri, TestContext.Current.CancellationToken);
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);

        rejections.Policies.ShouldBe(["global"]);
    }

    private sealed class RejectionRecorder : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly ConcurrentQueue<string?> _policies = new();

        public RejectionRecorder()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, RateLimitingMetrics.MeterName, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, RateLimitingMetrics.RejectionsCounterName, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            {
                foreach (var tag in tags)
                {
                    if (string.Equals(tag.Key, "policy", StringComparison.Ordinal))
                    {
                        _policies.Enqueue(tag.Value as string);
                    }
                }
            });
            _listener.Start();
        }

        public IReadOnlyCollection<string?> Policies => _policies;

        public void Dispose() => _listener.Dispose();
    }
}

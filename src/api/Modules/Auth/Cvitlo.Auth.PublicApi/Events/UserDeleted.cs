using FastEndpoints;

namespace Cvitlo.Auth.PublicApi.Events;

/// <summary>
/// Published after Keycloak has deleted a user, so every module can remove the data it owns for that user.
/// </summary>
/// <remarks>
/// The event may arrive more than once, so handlers must be idempotent.
/// </remarks>
/// <param name="Subject">The Keycloak <c>sub</c> of the deleted user.</param>
public sealed record UserDeleted(string Subject) : IEvent;

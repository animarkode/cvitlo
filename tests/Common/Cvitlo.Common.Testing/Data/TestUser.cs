namespace Cvitlo.Common.Testing.Data;

/// <summary>A Keycloak user created for a test, with the password it signs in with.</summary>
public sealed record TestUser(string Email, string Password, string FirstName, string LastName);

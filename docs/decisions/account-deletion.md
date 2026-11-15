# Account deletion

`DELETE /api/v1/auth/account` is handled by `DeleteAccountCommandHandler`
(`src/api/Modules/Auth/Cvitlo.Auth.Application/Features/Account/DeleteAccount/`). It deletes the
Keycloak user, tells the other modules to erase the user's data through the `UserDeleted` event, and
ends every web session. The order of those steps is deliberate.

## Order and failure handling

- **Keycloak first.** If the Keycloak call fails, nothing else has happened and the person can retry.
- **Past the point of no return.** Once the Keycloak user is gone the deletion cannot be undone, so the
  rest no longer depends on the request and runs with `CancellationToken.None`: a cancelled request
  must not leave sessions open or data behind.
- **Sessions end before `UserDeleted` is published.** That way no other tab or device can write data
  after the erasure.
- **A failing module does not fail the request.** The Keycloak user is already deleted, so the failure
  is logged and the pending row below is kept for a retry.

## The pending row

A `PendingAccountErasure` row is saved before the Keycloak call and removed only when everything is
done. A failed publish or a crash therefore leaves a durable record, which `PendingErasureSweeper`
finishes (hourly, and only for rows older than `MinAge`, so it never races a running request).

If the Keycloak call fails the row stays too: a timeout can hide a deletion Keycloak did make. The
sweep asks Keycloak whether the user still exists. If so, the deletion never happened and the row is
dropped; if not, it ends the sessions, publishes `UserDeleted` and drops the row. Any failure keeps the
row for the next run.

## Re-authentication

Deleting needs a sign-in less than five minutes old (`MaxSignInAge`), otherwise the answer is 403
`Account.ReauthenticationRequired`. The age is read from the `auth_time` claim of the stored ID token.
Keycloak issued that token to the API over the back channel, so the browser cannot choose it; a token
refresh keeps `auth_time` and only a new sign-in changes it.

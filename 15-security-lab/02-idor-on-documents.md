# 02 - IDOR on Documents

IDOR stands for **Insecure Direct Object Reference**. It happens when an API accepts a direct object identifier, such as `documentId`, and returns or modifies the object without verifying that the authenticated caller is allowed to access that specific object.

In interviews, IDOR is one of the clearest ways to test whether someone understands that authentication is not authorization.

## Learning goals

By the end, you should be able to:

- Explain IDOR in one sentence.
- Spot controller actions that query by id without tenant/user constraints.
- Distinguish identity, tenant membership, role, and object-level permission.
- Decide when to return `403` vs `404`.
- Implement resource-based authorization in ASP.NET Core.
- Add integration tests for same-tenant, cross-tenant, and unauthorized document access.

## Broken scenario

A document app stores records like:

| Field | Example |
| --- | --- |
| `Id` | `doc_123` |
| `TenantId` | `tenant_a` |
| `OwnerUserId` | `user_1` |
| `Title` | `Q4 Planning` |
| `Body` | Confidential content |

The controller route is:

```text
GET /api/documents/{id}
```

The developer adds `[Authorize]`, queries `Documents.FindAsync(id)`, and returns the row. The endpoint is authenticated, but any signed-in user who can guess or obtain another document id can ask for that document.

Open the intentionally unsafe sample:

- [`idor-docs/VulnerableDocsController.cs`](idor-docs/VulnerableDocsController.cs)

## High-level exploit explanation

The failure is not that IDs exist in URLs. IDs are normal. The failure is that the server treats knowledge of an ID as proof of permission.

At a high level:

1. User A authenticates.
2. User A calls a route with User B's document id.
3. The server fetches the document by primary key only.
4. The server returns it because it never checked tenant or user access.

The same flaw can affect:

- `GET` document downloads;
- `PUT` edits;
- `DELETE` operations;
- export jobs;
- signed URL generation;
- search results;
- background job status pages;
- admin notes or billing pages.

This lab does not teach guessing or enumerating identifiers. The defensive lesson is that every object read/write must be scoped to the caller's allowed resources.

## Broken-code review checklist

Look for:

- `[Authorize]` with no object-level check.
- Query by `id` only: `FindAsync(id)`, `SingleAsync(x => x.Id == id)`.
- Tenant id accepted from route/body instead of derived from claims/session.
- Service methods named `GetById` used from controllers without caller context.
- Update/delete methods that attach an entity by id and save immediately.
- Search endpoints that filter only after loading rows.
- "Admin" bypasses without audited policy.
- Returning different error shapes that leak object existence unintentionally.

## Hardened design

Open the hardened sample:

- [`idor-docs/HardenedDocsController.cs`](idor-docs/HardenedDocsController.cs)

The hardened pattern:

1. Authenticate the caller.
2. Derive `userId` and `tenantId` from validated claims, not the request body.
3. Query documents with tenant and membership predicates.
4. Centralize authorization in a policy/service.
5. Apply the same check to read, update, delete, download, and job actions.
6. Audit denied access without leaking sensitive details to the client.

## Authorization model options

Choose intentionally:

| Model | Use when | Example check |
| --- | --- | --- |
| Tenant scoped | All tenant members can read tenant docs | `doc.TenantId == caller.TenantId` |
| Owner scoped | Only owner can read/edit | `doc.OwnerUserId == caller.UserId` |
| ACL scoped | Specific users/groups can read/edit | `DocumentPermissions.Any(user, doc, action)` |
| Role + object scoped | Tenant admins can manage tenant docs | `role == TenantAdmin && doc.TenantId == caller.TenantId` |
| Share-link scoped | External access via short-lived grant | `grant.DocumentId == id && grant.NotExpired` |

Do not hide this logic in the frontend. The API must enforce it.

## 403 vs 404

Two common patterns:

- Return `403` when the caller knows the object exists but lacks permission.
- Return `404` for cross-tenant objects to avoid confirming existence.

For multi-tenant SaaS, many teams return `404` for objects outside the tenant boundary and `403` for same-tenant objects where the user lacks a role. Pick a consistent policy, document it, and test it.

## Test cases

Create integration tests for:

- anonymous user gets `401`;
- tenant A user can read tenant A allowed document;
- tenant A user cannot read tenant B document;
- tenant A non-owner cannot edit owner-only document;
- tenant admin can edit same-tenant document;
- tenant admin cannot edit another tenant's document;
- request body `tenantId` cannot override claim tenant;
- bulk list endpoint returns only authorized documents;
- download endpoint applies the same authorization as metadata endpoint;
- denied attempts produce safe audit logs.

## Interview answer framework

1. **Bug:** "The endpoint fetched by document id without checking object-level access."
2. **Impact:** "An authenticated user could access another user's or tenant's document if they know the id."
3. **Fix:** "Derive caller context from validated claims and include tenant/permission constraints in the query or resource policy."
4. **Trade-off:** "Returning 404 can reduce existence leaks; returning 403 can aid UX. Multi-tenant boundaries often use 404."
5. **Tests:** "Cross-tenant and cross-user tests for every document action, not just GET."

## Production checklist

- [ ] Every repository/service method that returns protected objects accepts caller context or an authorization scope.
- [ ] Document list queries are tenant-filtered before pagination.
- [ ] Update/delete operations include tenant/user predicates.
- [ ] File blob paths are not derived directly from untrusted IDs.
- [ ] Background job IDs are scoped to the caller.
- [ ] Audit logs include caller id, tenant id, object id, action, decision, and reason code.
- [ ] Metrics track denied object access spikes.
- [ ] Security tests cover predictable and random identifiers.


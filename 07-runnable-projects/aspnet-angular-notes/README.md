# ASP.NET Core + Angular Notes Mini Project

This project is a compact CRUD application designed for interviews. It demonstrates the mechanics
of a production-style notes app without burying the core ideas in generated boilerplate:

- ASP.NET Core Minimal API.
- PostgreSQL persistence.
- Password hashing and JWT login.
- Authenticated, user-scoped notes CRUD.
- Angular standalone component with token storage and optimistic UI updates.
- Docker Compose for local API, web, and database dependencies.

## Architecture

```text
Angular Notes UI
  | register/login
  | Authorization: Bearer <jwt>
  v
ASP.NET Core Minimal API
  | validates JWT
  | scopes every query to sub/user id
  v
PostgreSQL
```

## Local services

| Service | Port | Purpose |
| --- | ---: | --- |
| `web` | `4200` | Angular dev server |
| `api` | `8081` | Minimal API |
| `postgres` | `5433` | Notes database |

## Quick start

```bash
cd 07-runnable-projects/aspnet-angular-notes
docker compose up --build
```

Open <http://localhost:4200>.

## Demo flow

1. Register a user with email and password.
2. The API hashes the password and returns a JWT.
3. The Angular component stores the token in `localStorage`.
4. Create a note.
5. Edit the note title/body.
6. Filter notes by search text.
7. Delete the note.
8. Sign out and show that protected API calls fail without the token.

## API endpoints

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `GET` | `/health` | No | API status |
| `POST` | `/auth/register` | No | Create account and issue token |
| `POST` | `/auth/login` | No | Validate password and issue token |
| `GET` | `/api/notes?query=` | JWT | List notes for the current user |
| `POST` | `/api/notes` | JWT | Create a note |
| `PUT` | `/api/notes/{id}` | JWT | Update one owned note |
| `DELETE` | `/api/notes/{id}` | JWT | Delete one owned note |

## Data model

The API creates tables at startup for interview convenience:

```sql
users(id, email, password_hash, created_at)
notes(id, user_id, title, body, is_pinned, created_at, updated_at)
```

In production, replace startup DDL with migrations. Good migration practices:

- Migrations are reviewed and run by CI/CD or an explicit deployment job.
- Destructive migrations are split into expand/backfill/contract phases.
- Large backfills are batched and resumable.
- Rollback strategy is documented.

## JWT design

The token includes:

- `sub`: stable user id.
- `email`: user email for display.
- `jti`: token id for audit and possible revocation.
- issuer and audience checks.
- expiration.

Production additions:

- Refresh tokens with rotation.
- Account lockout and rate limiting.
- Email verification and password reset.
- Strong password policy or external identity provider.
- Secret stored in a secret manager, not compose files.

## Authorization rule

Every note query includes `where user_id = @user_id`. This is the most important security rule in
the sample. Hiding another user's notes in the UI is not authorization; the server must enforce
ownership on reads, updates, and deletes.

## Validation rules

- Email must look like an email and is normalized to lowercase.
- Password must be at least 8 characters in the sample.
- Note title is required and capped at 160 characters.
- Note body is capped at 10,000 characters.
- Search query is trimmed and parameterized.

## Angular component responsibilities

The standalone component in `web/notes.component.ts` handles:

- Register/login form.
- Token persistence.
- Authenticated fetch wrapper.
- Notes list loading.
- Create/update/delete actions.
- Inline editing.
- Error and loading state.

For a larger Angular app, split it into:

- `AuthService`
- `NotesService`
- `NotesListComponent`
- `NoteEditorComponent`
- route guards and HTTP interceptors.

## Interview talking points

Be ready to explain:

- Why password hashing must use a purpose-built algorithm.
- Why JWT validation must check issuer, audience, signing key, and expiry.
- How you would revoke tokens.
- Why database ownership checks matter even if the UI filters records.
- How optimistic UI updates differ from server-confirmed updates.
- How to test authenticated APIs.
- How to handle schema migrations safely.
- How to add pagination for large note lists.

## Test checklist

Unit tests:

- Email normalization.
- Password verification success/failure.
- JWT contains expected claims.
- Note validation rejects blank titles.

Integration tests:

- Register then list notes returns empty list.
- Creating a note returns it in the authenticated user's list.
- User A cannot update/delete User B's note.
- Missing/expired token returns 401.
- Search query is parameterized and scoped.

Manual tests:

- Refresh page and verify token restores session.
- Sign out and verify notes disappear.
- Use two accounts in separate browsers.
- Try editing a note with a forged id.

## Production hardening

- Use HTTPS everywhere.
- Move JWT secret into a managed secret store.
- Add rate limiting on auth endpoints.
- Add structured logs and request correlation ids.
- Add OpenTelemetry traces.
- Add pagination, sorting, and ETags.
- Use migrations instead of startup DDL.
- Run containers as non-root users.
- Add dependency, container, and SAST scanning.


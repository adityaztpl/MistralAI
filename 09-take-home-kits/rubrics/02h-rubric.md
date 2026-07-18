# Rubric: 2-Hour Notes API

Use this rubric to self-grade before submission. A strong 2-hour take-home is small, correct, runnable, and easy to review.

## Scorecard

| Area | Weight | Strong signal | Weak signal |
|---|---:|---|---|
| API contract | 20% | Clear REST endpoints, correct status codes, stable response shapes | Ad hoc routes, inconsistent JSON, unclear update/delete semantics |
| Persistence | 15% | Durable or well-documented storage, clean data model, UTC timestamps | Hidden in-memory state without explanation, local time, no IDs |
| Validation/errors | 20% | Input validation, consistent problem responses, 404 handling | Accepts invalid data, crashes, ambiguous errors |
| Query behavior | 10% | Search/filter/pagination works predictably | List endpoint dumps everything or filters incorrectly |
| Tests | 20% | Integration tests cover happy path and failures | No tests or only trivial unit tests |
| README/reviewer experience | 15% | One-command setup, assumptions, trade-offs, curl examples | Reviewer must reverse-engineer setup |

## Detailed levels

### Excellent

- Reviewer can run API and tests in under 5 minutes.
- Endpoints match documented contract.
- Validation is explicit and tested.
- Tags are normalized and stable.
- Pagination metadata is correct.
- Timestamps are UTC and update correctly.
- Tests cover create, list/filter, update, delete/404, and validation.
- README clearly states timebox trade-offs.

### Good

- Core CRUD works.
- Basic validation and 404 handling exist.
- Tests cover most happy paths.
- README is enough to run locally.
- Some non-critical polish is missing, such as rich OpenAPI examples.

### Needs improvement

- App runs only in the author's IDE.
- No tests or tests do not exercise API behavior.
- Invalid input creates bad data.
- `GET /notes` cannot filter or paginate.
- Response shape changes between endpoints.
- README omits assumptions and setup details.

## Red flags

- Secrets committed.
- Generated binaries or dependency folders committed.
- API returns raw stack traces.
- No way to run tests.
- Over-engineered architecture that hides simple logic.
- Data loss or incorrect updates on normal requests.

## Reviewer questions to prepare for

- Why did you choose this persistence approach?
- Why PUT vs PATCH?
- How would you add authentication?
- How would you support concurrent edits?
- How would this change for 1 million notes?
- Which tests would you add next?

## Self-grade checklist

```text
[ ] Create returns 201 and Location.
[ ] Get unknown note returns 404.
[ ] Invalid title returns validation error.
[ ] List supports at least archived filter and pagination.
[ ] Update changes updatedAt, not createdAt.
[ ] Tags are normalized.
[ ] Tests run from command line.
[ ] README explains trade-offs.
```

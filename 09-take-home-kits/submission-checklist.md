# Take-Home Submission Checklist

Use this before sending any take-home. The goal is to remove reviewer friction and make your engineering judgment obvious.

## 1. Repository hygiene

```text
[ ] No secrets committed.
[ ] No node_modules, bin/obj, .venv, build artifacts, or local DB files unless intentionally seeded.
[ ] .env.example exists and is accurate.
[ ] README is at repo root.
[ ] License or usage note included if relevant.
[ ] Git history is reasonably clean if the company will inspect it.
```

## 2. Run experience

```text
[ ] Fresh clone instructions work.
[ ] Setup commands are copy-pasteable.
[ ] App can run without IDE-specific steps.
[ ] Tests can run from command line.
[ ] Seed data or sample docs are included.
[ ] Ports are documented.
[ ] External services are optional or have Docker Compose instructions.
```

## 3. README structure

Recommended README:

```markdown
# Project name

## What it does

## Quick start

## Demo flow

## Architecture

## API contract

## Tests and evaluation

## Design decisions

## Known limitations

## What I would do next
```

## 4. Design note

Keep this concise but specific:

- Requirements you implemented.
- Requirements you intentionally scoped out.
- Main architecture and data flow.
- Important trade-offs.
- Failure modes.
- Security considerations.
- Testing strategy.

## 5. API quality

```text
[ ] Endpoint names are consistent.
[ ] Request/response DTOs are stable.
[ ] Correct HTTP status codes are used.
[ ] Validation errors are useful.
[ ] Unknown IDs return 404.
[ ] Auth/security assumptions are documented.
[ ] Cancellation/timeouts are considered for long-running calls.
```

## 6. Frontend quality

```text
[ ] Loading state exists.
[ ] Error state exists.
[ ] Empty state exists.
[ ] Form validation exists.
[ ] Repeated submissions are controlled.
[ ] UI shows enough information for reviewer to verify behavior.
[ ] Accessibility basics: labels, buttons, keyboard behavior.
```

## 7. GenAI quality

```text
[ ] Provider keys stay server-side.
[ ] Prompt templates are readable and versionable.
[ ] Retrieved sources are shown or logged.
[ ] Citations are grounded in retrieved documents.
[ ] Unsupported questions return uncertainty.
[ ] Token/context limits are handled.
[ ] Prompt injection risk is mentioned or tested.
[ ] Small eval set is included for RAG/agent projects.
```

## 8. Data quality

```text
[ ] Schema/migrations are included when applicable.
[ ] Indexes support important queries.
[ ] Timestamps are UTC.
[ ] IDs are generated safely.
[ ] Seed/sample data is deterministic.
[ ] Destructive actions are protected or clearly documented.
```

## 9. Testing

Minimum:

```text
[ ] Happy path test.
[ ] Validation/error test.
[ ] Persistence or API integration test.
[ ] Domain-specific edge case test.
```

For RAG/agent work:

```text
[ ] Retrieval expected-source test.
[ ] No-answer test.
[ ] Unsafe action blocked test.
[ ] Prompt injection or adversarial content test.
```

## 10. Final self-review

Ask yourself:

- Could a reviewer run this in 5-10 minutes?
- Is the primary feature obvious from the demo flow?
- Did I explain trade-offs instead of hiding gaps?
- Did I test the riskiest behavior?
- Does the project show the level of role I am applying for?

## 11. Submission note template

```text
Hi <name>,

Thanks for the opportunity. I completed the take-home here: <repo/link>.

Quick notes:
- Run instructions are in the README.
- I focused on <top priorities> because of the timebox.
- The main trade-offs and next steps are documented under "Design decisions" and "Known limitations".
- Tests can be run with <command>.

Best,
<your name>
```

## 12. Follow-up interview prep

Prepare 2-minute answers for:

- Walk me through the architecture.
- What was the hardest trade-off?
- What would you improve with another day?
- How would you make this production-ready?
- How did you test correctness?
- What failure mode worries you most?

Use:

```text
Context -> Decision -> Trade-off -> Result -> Next improvement
```

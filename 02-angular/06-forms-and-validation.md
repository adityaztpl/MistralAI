# Angular Forms and Validation Deep Dive: Typed Reactive Forms, Dynamic Forms, Accessibility, and Interviews

Angular forms are more than controls and validators. Production form design includes typed values, dynamic rules, accessibility, server validation, and testability.

Interview framing:

> Start with the mental model, name the modern Angular API, explain the trade-off, then give a concrete production example.

---

## 1. Choosing a forms approach

Template-driven forms are good for simple static forms. Reactive forms are stronger for typed values, dynamic controls, custom validation, and tests.

```ts
readonly form = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]] });
```

Interview checklist:

- Choose by complexity.
- Mention typed reactive forms.
- Include server validation.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 2. Typed forms

Typed forms let TypeScript know control and form value shapes. `nonNullable` prevents unhelpful `null` values when null is not meaningful.

```ts
const value = this.form.getRawValue();
```

Interview checklist:

- Explain `value` versus `getRawValue`.
- Know disabled control behavior.
- Avoid `any`.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 3. Control state

Forms track validity and interaction state: touched, dirty, pending, disabled, valid, and invalid.

```ts
return control.invalid && (control.touched || this.submitted());
```

Interview checklist:

- Touched differs from dirty.
- Pending means async validation.
- Show errors at the right time.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 4. Custom validators

Synchronous validators are pure functions returning `null` when valid or a descriptive error object when invalid.

```ts
export const strongPassword: ValidatorFn = (control) => /[A-Z]/.test(control.value) ? null : { strongPassword: true };
```

Interview checklist:

- Keep validators pure.
- Use error payloads.
- Do not mutate controls.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 5. Cross-field validation

Cross-field rules belong on the group because they depend on multiple controls.

```ts
export const matchValidator: ValidatorFn = (group) => group.get('a')?.value === group.get('b')?.value ? null : { mismatch: true };
```

Interview checklist:

- Put rule on group.
- Display near relevant fields.
- Avoid destructive child error overwrites.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 6. Async validators

Async validators check server-backed rules such as uniqueness and must complete.

```ts
return timer(300).pipe(switchMap(() => api.available(control.value)), map((ok) => ok ? null : { taken: true }), take(1));
```

Interview checklist:

- Debounce server checks.
- Handle failures.
- Expose pending state.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 7. Dynamic forms

`FormArray` handles ordered repeated groups; `FormRecord` handles dynamic keyed controls.

```ts
aliases = this.fb.array([this.fb.nonNullable.control('', Validators.required)]);
```

Interview checklist:

- Use FormArray for order.
- Use FormRecord for dynamic keys.
- Track controls stably.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 8. Conditional controls

Conditional UI must keep enabled state, validators, value, and errors consistent.

```ts
control.disable({ emitEvent: false });
control.clearValidators();
control.updateValueAndValidity({ emitEvent: false });
```

Interview checklist:

- Avoid feedback loops.
- Call updateValueAndValidity.
- Understand omitted disabled values.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 9. Accessibility

Accessible forms need labels, associated errors, visible instructions, useful focus behavior, and assistive-technology-friendly invalid state.

```ts
<input aria-describedby="email-error" [attr.aria-invalid]="emailInvalid()" />
```

Interview checklist:

- Every input has a label.
- Associate errors.
- Provide summary for long forms.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## 10. Testing forms

Test validators as pure functions and component forms through user-like interactions.

```ts
form.controls.email.setValue('bad');
expect(form.controls.email.hasError('email')).toBeTrue();
```

Interview checklist:

- Test valid submit.
- Test invalid submit.
- Test server error mapping.

Deep-dive notes:

- Name the owner of the state or behavior before choosing an API.
- Prefer standalone APIs in new code while understanding legacy NgModule code.
- Keep examples typed; avoid `any` unless explaining why it is unsafe.
- Explain how the template is notified when state changes.
- Explain how the code fails: stale data, duplicate requests, memory leaks, or inaccessible UI.
- Include the testing boundary: pure function, service, component, integration, or E2E.
- Include the production boundary: browser, server, network, cache, or third-party library.
- Mention migration concerns when older Angular patterns may still exist.

Common production notes:

- Keep state ownership explicit.
- Prefer typed APIs and narrow component contracts.
- Model loading, empty, success, and error states deliberately.
- Test the smallest boundary that gives confidence.
- Mention accessibility, performance, and security when the feature touches users.

Interview traps:

- Do not present a framework feature as a security boundary unless the server enforces it.
- Do not optimize before describing how you would measure the problem.
- Do not confuse current state with an event stream.
- Do not hide side effects inside derivations.

---

## Final interview drill

Practice answering each topic in this order:

1. Define the concept in one sentence.
2. Explain when you use it.
3. Explain when you avoid it.
4. Write or describe a small Angular 17+ standalone example.
5. Name a testing strategy.
6. Name one production pitfall.

---

## Appendix A. Validator recipe catalog

Required text:

```ts
name: ['', Validators.required],
```

Number range:

```ts
quantity: [1, [Validators.required, Validators.min(1), Validators.max(99)]],
```

Pattern with readable error:

```ts
sku: ['', [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/)]],
```

Cross-field date range:

```ts
export const dateRangeValidator: ValidatorFn = (group) => {
  const start = group.get('startDate')?.value;
  const end = group.get('endDate')?.value;

  if (!start || !end) {
    return null;
  }

  return new Date(start) <= new Date(end) ? null : { dateRange: true };
};
```

Async uniqueness:

```ts
export function uniqueEmail(api: UsersApi): AsyncValidatorFn {
  return (control) =>
    timer(300).pipe(
      switchMap(() => api.emailAvailable(control.value)),
      map((available) => (available ? null : { emailTaken: true })),
      catchError(() => of({ emailCheckFailed: true })),
      take(1),
    );
}
```

## Appendix B. Server error mapping strategy

Server validation response shape:

```ts
interface ValidationProblem {
  fieldErrors: Record<string, string[]>;
  formErrors: string[];
}
```

Mapping pattern:

```ts
for (const [path, messages] of Object.entries(problem.fieldErrors)) {
  const control = this.form.get(path);

  if (control) {
    control.setErrors({ ...control.errors, server: messages });
    control.markAsTouched();
  }
}
```

Checklist:

- Preserve field paths from the backend.
- Put business-rule errors at form level.
- Clear stale server errors when the user edits the field.
- Do not expose internal backend exception text.
- Keep server validation authoritative.

## Appendix C. Accessibility review

For every form, verify:

- Each control has a visible label or accessible name.
- Required fields are communicated before submit.
- Error text explains how to fix the value.
- `aria-describedby` connects controls to error/help text.
- `aria-invalid` reflects invalid visible state.
- Long forms include an error summary after failed submit.
- Focus management helps keyboard and screen-reader users.
- Disabled submit buttons do not hide the reason submission is blocked.

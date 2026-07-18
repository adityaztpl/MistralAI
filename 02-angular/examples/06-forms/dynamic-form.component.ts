import { JsonPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, FormArray, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { startWith } from 'rxjs';

export interface EmergencyContactFormValue {
  name: string;
  relationship: string;
  phone: string;
}

export interface ProfileFormValue {
  firstName: string;
  lastName: string;
  email: string;
  wantsNewsletter: boolean;
  emergencyContacts: EmergencyContactFormValue[];
}

function atLeastOneEmergencyContact(control: AbstractControl): ValidationErrors | null {
  const contacts = control.value as EmergencyContactFormValue[] | null;
  return contacts && contacts.length > 0 ? null : { atLeastOneContact: true };
}

@Component({
  selector: 'app-dynamic-profile-form',
  standalone: true,
  imports: [JsonPipe, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>First name <input formControlName="firstName" /></label>
      @if (showControlError(form.controls.firstName, 'required')) {
        <p>First name is required.</p>
      }

      <label>Last name <input formControlName="lastName" /></label>
      <label>Email <input type="email" formControlName="email" /></label>

      <label>
        <input type="checkbox" formControlName="wantsNewsletter" />
        Send product updates
      </label>

      <section formArrayName="emergencyContacts">
        <h2>Emergency contacts</h2>
        @if (submitted() && emergencyContacts.hasError('atLeastOneContact')) {
          <p role="alert">Add at least one emergency contact.</p>
        }

        @for (contact of emergencyContacts.controls; track contact) {
          <fieldset [formGroupName]="$index">
            <legend>Contact {{ $index + 1 }}</legend>
            <label>Name <input formControlName="name" /></label>
            <label>Relationship <input formControlName="relationship" /></label>
            <label>Phone <input formControlName="phone" /></label>
            <button type="button" (click)="removeContact($index)">Remove</button>
          </fieldset>
        }

        <button type="button" (click)="addContact()">Add contact</button>
      </section>

      <button type="submit">Save profile</button>
    </form>

    <aside>
      <h2>Live preview</h2>
      <p>{{ previewName() }}</p>
      <pre>{{ rawValue() | json }}</pre>
    </aside>
  `,
})
export class DynamicProfileFormComponent {
  private readonly fb = inject(FormBuilder);
  readonly submitted = signal(false);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    wantsNewsletter: [true],
    emergencyContacts: this.fb.array([this.createContactGroup()], {
      validators: [atLeastOneEmergencyContact],
    }),
  });

  readonly rawValue = toSignal(
    this.form.valueChanges.pipe(startWith(this.form.getRawValue())),
    { initialValue: this.form.getRawValue() },
  );

  readonly previewName = computed(() => {
    const value = this.rawValue();
    return [value.firstName, value.lastName].filter(Boolean).join(' ') || 'New user';
  });

  get emergencyContacts(): FormArray {
    return this.form.controls.emergencyContacts;
  }

  addContact(): void {
    this.emergencyContacts.push(this.createContactGroup());
  }

  removeContact(index: number): void {
    this.emergencyContacts.removeAt(index);
    this.emergencyContacts.markAsTouched();
  }

  showControlError(control: AbstractControl, errorCode: string): boolean {
    return control.hasError(errorCode) && (control.touched || this.submitted());
  }

  submit(): void {
    this.submitted.set(true);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value: ProfileFormValue = this.form.getRawValue();
    console.log('Profile saved', value);
  }

  private createContactGroup() {
    return this.fb.nonNullable.group({
      name: ['', Validators.required],
      relationship: ['', Validators.required],
      phone: ['', [Validators.required, Validators.pattern(/^[0-9+() -]{7,}$/)]],
    });
  }
}

import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';

export interface ProductDraft {
  name: string;
  sku: string;
  price: number;
  description: string;
}

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="product-form">
      <label>
        Name
        <input formControlName="name" />
      </label>
      @if (isInvalid('name')) {
        <p class="error">Name is required.</p>
      }

      <label>
        SKU
        <input formControlName="sku" />
      </label>
      @if (form.controls.sku.touched && form.controls.sku.hasError('pattern')) {
        <p class="error">Use uppercase letters, numbers, and dashes only.</p>
      }

      <label>
        Price
        <input type="number" formControlName="price" />
      </label>
      @if (form.controls.price.touched && form.controls.price.hasError('min')) {
        <p class="error">Price must be at least zero.</p>
      }

      <label>
        Description
        <textarea rows="4" formControlName="description"></textarea>
      </label>

      <button type="submit" [disabled]="form.invalid">Save product</button>
    </form>
  `,
  styles: [
    `
      .product-form {
        display: grid;
        gap: 0.75rem;
        max-width: 32rem;
      }

      .error {
        color: #b91c1c;
        margin: 0;
      }
    `,
  ],
})
export class ProductFormComponent {
  saved = output<ProductDraft>();

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    sku: ['', [Validators.required, Validators.pattern(/^[A-Z0-9-]+$/)]],
    price: [0, [Validators.required, Validators.min(0)]],
    description: [''],
  });

  constructor(private readonly fb: FormBuilder) {}

  isInvalid(controlName: 'name' | 'sku' | 'price' | 'description'): boolean {
    const control = this.form.controls[controlName];
    return control.touched && control.invalid;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saved.emit(this.form.getRawValue());
  }
}

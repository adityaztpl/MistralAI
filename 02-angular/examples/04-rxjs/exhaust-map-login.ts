import { HttpClient } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, Injectable, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, Subject, catchError, exhaustMap, finalize, tap } from 'rxjs';

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  login(credentials: LoginCredentials) {
    return this.http.post<LoginResponse>('/api/login', credentials);
  }
}

@Injectable({ providedIn: 'root' })
export class AuthSessionStore {
  private readonly currentUserName = signal<string | null>(null);
  readonly userName = this.currentUserName.asReadonly();

  startSession(response: LoginResponse): void {
    sessionStorage.setItem('access_token', response.accessToken);
    this.currentUserName.set(response.displayName);
  }
}

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()">
      <label>Email <input type="email" formControlName="email" /></label>
      <label>Password <input type="password" formControlName="password" /></label>

      @if (error()) {
        <p role="alert">{{ error() }}</p>
      }

      <button type="submit" [disabled]="form.invalid || loading()">
        {{ loading() ? 'Signing in...' : 'Sign in' }}
      </button>
    </form>
  `,
})
export class LoginFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly session = inject(AuthSessionStore);
  private readonly destroyRef = inject(DestroyRef);
  private readonly submitClicks = new Subject<LoginCredentials>();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  constructor() {
    this.submitClicks
      .pipe(
        exhaustMap((credentials) => {
          this.loading.set(true);
          this.error.set(null);

          return this.authApi.login(credentials).pipe(
            tap((response) => this.session.startSession(response)),
            catchError(() => {
              this.error.set('Invalid email or password.');
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitClicks.next(this.form.getRawValue());
  }
}

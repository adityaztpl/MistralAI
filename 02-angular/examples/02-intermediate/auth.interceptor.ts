import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export abstract class AuthTokenStore {
  abstract getAccessToken(): string | null;
  abstract clear(): void;
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStore = inject(AuthTokenStore);
  const router = inject(Router);
  const token = tokenStore.getAccessToken();

  const authRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(authRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        tokenStore.clear();
        void router.navigate(['/login'], {
          queryParams: { sessionExpired: 'true' },
        });
      }

      return throwError(() => error);
    }),
  );
};

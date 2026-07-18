import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
};

export type AuthState = {
  user: AuthUser | null;
  accessToken: string | null;
  status: 'idle' | 'checking' | 'authenticated' | 'anonymous';
  error: string | null;
};

export type SignInCredentials = {
  email: string;
  password: string;
};

export type SignInResponse = {
  user: AuthUser;
  accessToken: string;
};

const initialState: AuthState = {
  user: null,
  accessToken: null,
  status: 'idle',
  error: null,
};

export const signIn = createAsyncThunk<
  SignInResponse,
  SignInCredentials,
  { rejectValue: string }
>('auth/signIn', async (credentials, { rejectWithValue }) => {
  const response = await fetch('/api/session', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    return rejectWithValue('Invalid email or password');
  }

  return (await response.json()) as SignInResponse;
});

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    sessionRestored(state, action: PayloadAction<SignInResponse>) {
      state.user = action.payload.user;
      state.accessToken = action.payload.accessToken;
      state.status = 'authenticated';
      state.error = null;
    },
    signedOut(state) {
      state.user = null;
      state.accessToken = null;
      state.status = 'anonymous';
      state.error = null;
    },
    authErrorCleared(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(signIn.pending, (state) => {
        state.status = 'checking';
        state.error = null;
      })
      .addCase(signIn.fulfilled, (state, action) => {
        state.user = action.payload.user;
        state.accessToken = action.payload.accessToken;
        state.status = 'authenticated';
        state.error = null;
      })
      .addCase(signIn.rejected, (state, action) => {
        state.user = null;
        state.accessToken = null;
        state.status = 'anonymous';
        state.error = action.payload ?? action.error.message ?? 'Sign in failed';
      });
  },
});

export const { authErrorCleared, sessionRestored, signedOut } = authSlice.actions;
export const authReducer = authSlice.reducer;

export function selectCurrentUser(state: { auth: AuthState }) {
  return state.auth.user;
}

export function selectIsAuthenticated(state: { auth: AuthState }) {
  return state.auth.status === 'authenticated' && state.auth.user !== null;
}

export function selectCanAccessAdmin(state: { auth: AuthState }) {
  return state.auth.user?.roles.includes('admin') ?? false;
}

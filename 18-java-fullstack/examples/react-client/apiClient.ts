const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";

let accessTokenProvider: () => string | null = () => null;

export function setAccessTokenProvider(provider: () => string | null) {
  accessTokenProvider = provider;
}

export type ProblemDetails = {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};

type RequestOptions = Omit<RequestInit, "body"> & {
  body?: unknown;
  skipAuth?: boolean;
};

export const apiClient = {
  get: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: "GET" }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "POST", body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "PUT", body }),
  patch: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "PATCH", body }),
  delete: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: "DELETE" }),
};

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const token = options.skipAuth ? null : accessTokenProvider();
  const headers = new Headers(options.headers);

  if (!headers.has("Content-Type") && options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 401) {
    window.dispatchEvent(new CustomEvent("auth:expired"));
  }

  if (!response.ok) {
    throw await parseError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

async function parseError(response: Response): Promise<ProblemDetails> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/problem+json") || contentType.includes("application/json")) {
    return response.json() as Promise<ProblemDetails>;
  }

  return {
    title: response.statusText || "Request failed",
    status: response.status,
    detail: await response.text(),
  };
}

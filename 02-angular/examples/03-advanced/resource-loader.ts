import { computed, signal } from '@angular/core';

export type ResourceStatus = 'idle' | 'loading' | 'success' | 'error';

export interface ResourceState<T> {
  status: ResourceStatus;
  data: T | null;
  error: unknown;
}

export interface ResourceLoader<TParams, TResult> {
  state: () => ResourceState<TResult>;
  data: () => TResult | null;
  isLoading: () => boolean;
  error: () => unknown;
  load(params: TParams): Promise<TResult | null>;
  reset(): void;
}

export function createResourceLoader<TParams, TResult>(
  loader: (params: TParams, signal: AbortSignal) => Promise<TResult>,
): ResourceLoader<TParams, TResult> {
  const state = signal<ResourceState<TResult>>({
    status: 'idle',
    data: null,
    error: null,
  });

  let abortController: AbortController | null = null;
  let requestId = 0;

  const data = computed(() => state().data);
  const isLoading = computed(() => state().status === 'loading');
  const error = computed(() => state().error);

  async function load(params: TParams): Promise<TResult | null> {
    abortController?.abort();

    const currentRequestId = ++requestId;
    abortController = new AbortController();
    state.set({ status: 'loading', data: state().data, error: null });

    try {
      const result = await loader(params, abortController.signal);

      if (currentRequestId !== requestId) {
        return null;
      }

      state.set({ status: 'success', data: result, error: null });
      return result;
    } catch (caughtError) {
      if (currentRequestId !== requestId || abortController.signal.aborted) {
        return null;
      }

      state.set({ status: 'error', data: null, error: caughtError });
      return null;
    }
  }

  function reset(): void {
    abortController?.abort();
    requestId++;
    state.set({ status: 'idle', data: null, error: null });
  }

  return {
    state: state.asReadonly(),
    data,
    isLoading,
    error,
    load,
    reset,
  };
}

export const productResource = createResourceLoader(async (id: number, signal) => {
  const response = await fetch(`/api/products/${id}`, { signal });

  if (!response.ok) {
    throw new Error(`Product request failed with ${response.status}`);
  }

  return (await response.json()) as {
    id: number;
    name: string;
    price: number;
  };
});

import { renderHook, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { useFetch } from '../02-intermediate/useFetch';

function mockJsonResponse(body: unknown, init?: ResponseInit) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status: 200,
      headers: {
        'Content-Type': 'application/json',
      },
      ...init,
    }),
  );
}

describe('useFetch', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('starts in loading state and stores fetched data', async () => {
    const fetchMock = vi
      .spyOn(globalThis, 'fetch')
      .mockResolvedValueOnce(
        await mockJsonResponse({ id: 'u1', name: 'Ada Lovelace' }),
      );

    const { result } = renderHook(() =>
      useFetch<{ id: string; name: string }>('/api/users/u1'),
    );

    expect(result.current).toEqual({
      status: 'loading',
      data: null,
      error: null,
    });

    await waitFor(() => {
      expect(result.current.status).toBe('success');
    });

    expect(result.current).toEqual({
      status: 'success',
      data: { id: 'u1', name: 'Ada Lovelace' },
      error: null,
    });
    expect(fetchMock).toHaveBeenCalledWith('/api/users/u1', {
      signal: expect.any(AbortSignal),
    });
  });

  it('exposes an error state for non-2xx responses', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce(
      await mockJsonResponse({ message: 'Nope' }, { status: 500 }),
    );

    const { result } = renderHook(() => useFetch('/api/users/u1'));

    await waitFor(() => {
      expect(result.current.status).toBe('error');
    });

    expect(result.current.error).toEqual(new Error('Request failed with 500'));
  });

  it('stays idle when disabled', () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch');

    const { result } = renderHook(() =>
      useFetch('/api/users/u1', { enabled: false }),
    );

    expect(result.current).toEqual({
      status: 'idle',
      data: null,
      error: null,
    });
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('aborts the previous request when the url changes', async () => {
    const abortSignals: AbortSignal[] = [];

    vi.spyOn(globalThis, 'fetch').mockImplementation((_, init) => {
      if (init?.signal instanceof AbortSignal) {
        abortSignals.push(init.signal);
      }

      return new Promise<Response>(() => {
        // Keep the request pending so rerender cleanup must abort it.
      });
    });

    const { rerender } = renderHook(({ url }) => useFetch(url), {
      initialProps: { url: '/api/users/u1' },
    });

    rerender({ url: '/api/users/u2' });

    await waitFor(() => {
      expect(abortSignals).toHaveLength(2);
    });

    expect(abortSignals[0].aborted).toBe(true);
    expect(abortSignals[1].aborted).toBe(false);
  });
});

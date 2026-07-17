import { useEffect, useState } from 'react';

type FetchState<T> =
  | { status: 'idle'; data: null; error: null }
  | { status: 'loading'; data: T | null; error: null }
  | { status: 'success'; data: T; error: null }
  | { status: 'error'; data: null; error: Error };

type UseFetchOptions<T> = {
  enabled?: boolean;
  parse?: (response: Response) => Promise<T>;
};

async function defaultParse<T>(response: Response): Promise<T> {
  return (await response.json()) as T;
}

export function useFetch<T>(
  url: string,
  { enabled = true, parse = defaultParse<T> }: UseFetchOptions<T> = {},
) {
  const [state, setState] = useState<FetchState<T>>({
    status: enabled ? 'loading' : 'idle',
    data: null,
    error: null,
  });

  useEffect(() => {
    if (!enabled) {
      setState({ status: 'idle', data: null, error: null });
      return;
    }

    const controller = new AbortController();
    setState((current) => ({
      status: 'loading',
      data: current.status === 'success' ? current.data : null,
      error: null,
    }));

    fetch(url, { signal: controller.signal })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Request failed with ${response.status}`);
        }

        return parse(response);
      })
      .then((data) => setState({ status: 'success', data, error: null }))
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return;
        }

        setState({
          status: 'error',
          data: null,
          error: error instanceof Error ? error : new Error(String(error)),
        });
      });

    return () => controller.abort();
  }, [enabled, parse, url]);

  return state;
}

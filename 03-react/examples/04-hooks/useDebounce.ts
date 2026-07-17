import { useEffect, useState } from 'react';

/**
 * Returns a value that updates only after it has stayed unchanged for delayMs.
 *
 * Debounce is time-based. It is different from React's useDeferredValue, which is
 * scheduler-based and allows React to prioritize urgent rendering work.
 */
export function useDebounce<T>(value: T, delayMs: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      setDebouncedValue(value);
    }, delayMs);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [delayMs, value]);

  return debouncedValue;
}

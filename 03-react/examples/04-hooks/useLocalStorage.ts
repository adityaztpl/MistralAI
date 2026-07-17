import { useCallback, useEffect, useState } from 'react';

type Serializer<T> = {
  parse: (value: string) => T;
  stringify: (value: T) => string;
};

type UseLocalStorageOptions<T> = {
  serializer?: Serializer<T>;
  initializeWithValue?: boolean;
};

const jsonSerializer: Serializer<unknown> = {
  parse: JSON.parse,
  stringify: JSON.stringify,
};

function isBrowser() {
  return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
}

/**
 * Synchronizes a React value with localStorage.
 *
 * Notes:
 * - Keeps SSR safe by not touching window during module evaluation.
 * - Handles malformed stored values by falling back to the initial value.
 * - Listens for cross-tab storage updates.
 */
export function useLocalStorage<T>(
  key: string,
  initialValue: T | (() => T),
  {
    serializer = jsonSerializer as Serializer<T>,
    initializeWithValue = true,
  }: UseLocalStorageOptions<T> = {},
) {
  const getInitialValue = useCallback(() => {
    return initialValue instanceof Function ? initialValue() : initialValue;
  }, [initialValue]);

  const readValue = useCallback((): T => {
    const fallbackValue = getInitialValue();

    if (!isBrowser()) {
      return fallbackValue;
    }

    try {
      const storedValue = window.localStorage.getItem(key);
      return storedValue === null ? fallbackValue : serializer.parse(storedValue);
    } catch {
      return fallbackValue;
    }
  }, [getInitialValue, key, serializer]);

  const [storedValue, setStoredValue] = useState<T>(() =>
    initializeWithValue ? readValue() : getInitialValue(),
  );

  const setValue = useCallback(
    (valueOrUpdater: T | ((currentValue: T) => T)) => {
      setStoredValue((currentValue) => {
        const nextValue =
          valueOrUpdater instanceof Function
            ? valueOrUpdater(currentValue)
            : valueOrUpdater;

        if (isBrowser()) {
          window.localStorage.setItem(key, serializer.stringify(nextValue));
        }

        return nextValue;
      });
    },
    [key, serializer],
  );

  const removeValue = useCallback(() => {
    if (isBrowser()) {
      window.localStorage.removeItem(key);
    }

    setStoredValue(getInitialValue());
  }, [getInitialValue, key]);

  useEffect(() => {
    setStoredValue(readValue());
  }, [readValue]);

  useEffect(() => {
    if (!isBrowser()) {
      return;
    }

    function handleStorage(event: StorageEvent) {
      if (event.key === key) {
        setStoredValue(readValue());
      }
    }

    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, [key, readValue]);

  return {
    value: storedValue,
    setValue,
    removeValue,
  } as const;
}

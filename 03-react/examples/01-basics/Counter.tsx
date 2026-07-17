import { useState } from 'react';

type CounterProps = {
  initialValue?: number;
  step?: number;
};

export function Counter({ initialValue = 0, step = 1 }: CounterProps) {
  const [count, setCount] = useState(initialValue);

  return (
    <section aria-labelledby="counter-heading">
      <h2 id="counter-heading">Counter</h2>
      <p>Current count: {count}</p>

      <button type="button" onClick={() => setCount((value) => value - step)}>
        Decrement
      </button>
      <button type="button" onClick={() => setCount(initialValue)}>
        Reset
      </button>
      <button type="button" onClick={() => setCount((value) => value + step)}>
        Increment
      </button>
    </section>
  );
}

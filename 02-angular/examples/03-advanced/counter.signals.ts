import { computed, effect, signal, untracked } from '@angular/core';

export interface CounterSnapshot {
  count: number;
  doubled: number;
  isEven: boolean;
}

export function createCounterStore(initialValue = 0) {
  const count = signal(initialValue);
  const step = signal(1);

  const doubled = computed(() => count() * 2);
  const isEven = computed(() => count() % 2 === 0);
  const snapshot = computed<CounterSnapshot>(() => ({
    count: count(),
    doubled: doubled(),
    isEven: isEven(),
  }));

  effect(() => {
    const current = count();
    const currentStep = untracked(step);
    console.debug(`Counter changed to ${current}; current step is ${currentStep}`);
  });

  return {
    count: count.asReadonly(),
    step: step.asReadonly(),
    doubled,
    isEven,
    snapshot,
    increment(): void {
      count.update((value) => value + step());
    },
    decrement(): void {
      count.update((value) => value - step());
    },
    reset(): void {
      count.set(initialValue);
    },
    setStep(nextStep: number): void {
      step.set(Math.max(1, Math.trunc(nextStep)));
    },
  };
}

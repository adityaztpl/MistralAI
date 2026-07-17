import { Injectable, computed, signal } from '@angular/core';

export interface Todo {
  id: number;
  title: string;
  completed: boolean;
}

@Injectable({ providedIn: 'root' })
export class TodoService {
  private readonly todos = signal<Todo[]>([
    { id: 1, title: 'Review Angular components', completed: true },
    { id: 2, title: 'Practice dependency injection', completed: false },
  ]);

  readonly all = this.todos.asReadonly();
  readonly remaining = computed(() =>
    this.todos().filter((todo) => !todo.completed),
  );
  readonly completedCount = computed(
    () => this.todos().filter((todo) => todo.completed).length,
  );

  add(title: string): void {
    const trimmedTitle = title.trim();

    if (!trimmedTitle) {
      return;
    }

    this.todos.update((todos) => [
      ...todos,
      {
        id: Date.now(),
        title: trimmedTitle,
        completed: false,
      },
    ]);
  }

  toggle(id: number): void {
    this.todos.update((todos) =>
      todos.map((todo) =>
        todo.id === id ? { ...todo, completed: !todo.completed } : todo,
      ),
    );
  }

  remove(id: number): void {
    this.todos.update((todos) => todos.filter((todo) => todo.id !== id));
  }
}

import { Injectable, computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

interface Todo {
  id: number;
  title: string;
  completed: boolean;
}

@Injectable()
class TodoStore {
  private readonly todos = signal<Todo[]>([]);
  private nextId = 1;

  readonly all = this.todos.asReadonly();
  readonly remaining = computed(() => this.todos().filter((todo) => !todo.completed));
  readonly completedCount = computed(() => this.todos().filter((todo) => todo.completed).length);

  add(title: string): void {
    const trimmedTitle = title.trim();

    if (!trimmedTitle) {
      return;
    }

    this.todos.update((todos) => [
      ...todos,
      { id: this.nextId++, title: trimmedTitle, completed: false },
    ]);
  }

  toggle(id: number): void {
    this.todos.update((todos) =>
      todos.map((todo) => (todo.id === id ? { ...todo, completed: !todo.completed } : todo)),
    );
  }

  remove(id: number): void {
    this.todos.update((todos) => todos.filter((todo) => todo.id !== id));
  }

  clearCompleted(): void {
    this.todos.update((todos) => todos.filter((todo) => !todo.completed));
  }
}

describe(TodoStore.name, () => {
  let store: TodoStore;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [TodoStore] });
    store = TestBed.inject(TodoStore);
  });

  it('adds trimmed todo titles', () => {
    store.add('  Learn signals  ');

    expect(store.all()).toEqual([
      { id: 1, title: 'Learn signals', completed: false },
    ]);
  });

  it('ignores empty titles', () => {
    store.add('   ');

    expect(store.all()).toEqual([]);
  });

  it('toggles completion and updates computed counts', () => {
    store.add('Write tests');
    store.toggle(1);

    expect(store.remaining()).toEqual([]);
    expect(store.completedCount()).toBe(1);
  });

  it('removes todos by id', () => {
    store.add('Keep');
    store.add('Remove');

    store.remove(2);

    expect(store.all()).toEqual([{ id: 1, title: 'Keep', completed: false }]);
  });

  it('clears completed todos only', () => {
    store.add('Done');
    store.add('Still active');
    store.toggle(1);

    store.clearCompleted();

    expect(store.all()).toEqual([
      { id: 2, title: 'Still active', completed: false },
    ]);
  });
});

import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';

interface TodoItem {
  id: number;
  title: string;
  completed: boolean;
}

class TodoStoreStub {
  private readonly todos = signal<TodoItem[]>([
    { id: 1, title: 'Learn standalone testing', completed: false },
  ]);

  readonly all = this.todos.asReadonly();
  readonly remainingCount = computed(() => this.todos().filter((todo) => !todo.completed).length);

  add(title: string): void {
    this.todos.update((todos) => [
      ...todos,
      { id: todos.length + 1, title, completed: false },
    ]);
  }

  toggle(id: number): void {
    this.todos.update((todos) =>
      todos.map((todo) => (todo.id === id ? { ...todo, completed: !todo.completed } : todo)),
    );
  }
}

@Component({
  selector: 'app-tested-todos',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form (ngSubmit)="addTodo()">
      <label for="todo-title">Todo title</label>
      <input id="todo-title" name="title" [(ngModel)]="draftTitle" />
      <button type="submit">Add todo</button>
    </form>

    <p>Remaining: {{ store.remainingCount() }}</p>

    <ul>
      @for (todo of store.all(); track todo.id) {
        <li>
          <label>
            <input type="checkbox" [checked]="todo.completed" (change)="store.toggle(todo.id)" />
            {{ todo.title }}
          </label>
        </li>
      }
    </ul>
  `,
})
class TestedTodosComponent {
  readonly store = inject(TodoStoreStub);
  draftTitle = '';

  addTodo(): void {
    const title = this.draftTitle.trim();

    if (!title) {
      return;
    }

    this.store.add(title);
    this.draftTitle = '';
  }
}

describe(TestedTodosComponent.name, () => {
  let fixture: ComponentFixture<TestedTodosComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestedTodosComponent],
      providers: [TodoStoreStub],
    }).compileComponents();

    fixture = TestBed.createComponent(TestedTodosComponent);
    fixture.detectChanges();
  });

  it('renders the initial todo and remaining count', () => {
    expect(fixture.nativeElement.textContent).toContain('Learn standalone testing');
    expect(fixture.nativeElement.textContent).toContain('Remaining: 1');
  });

  it('adds a todo from the form', () => {
    const input: HTMLInputElement = fixture.nativeElement.querySelector('#todo-title');
    input.value = 'Write component tests';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Write component tests');
    expect(fixture.nativeElement.textContent).toContain('Remaining: 2');
  });

  it('toggles a todo when the checkbox changes', () => {
    const checkbox: HTMLInputElement = fixture.nativeElement.querySelector('input[type="checkbox"]');
    checkbox.click();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Remaining: 0');
  });
});

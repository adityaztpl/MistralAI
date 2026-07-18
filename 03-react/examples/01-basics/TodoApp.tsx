import { useMemo, useState, type FormEvent } from 'react';

type Todo = {
  id: string;
  title: string;
  completed: boolean;
};

const initialTodos: Todo[] = [
  { id: 'learn-jsx', title: 'Learn JSX', completed: true },
  { id: 'practice-hooks', title: 'Practice hooks', completed: false },
];

export function TodoApp() {
  const [todos, setTodos] = useState<Todo[]>(initialTodos);
  const [newTitle, setNewTitle] = useState('');

  const remainingCount = useMemo(
    () => todos.filter((todo) => !todo.completed).length,
    [todos],
  );

  function addTodo(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const title = newTitle.trim();

    if (!title) {
      return;
    }

    setTodos((current) => [
      ...current,
      {
        id: crypto.randomUUID(),
        title,
        completed: false,
      },
    ]);
    setNewTitle('');
  }

  function toggleTodo(id: string) {
    setTodos((current) =>
      current.map((todo) =>
        todo.id === id ? { ...todo, completed: !todo.completed } : todo,
      ),
    );
  }

  function removeTodo(id: string) {
    setTodos((current) => current.filter((todo) => todo.id !== id));
  }

  return (
    <main>
      <h1>Todo App</h1>
      <p>{remainingCount} remaining</p>

      <form onSubmit={addTodo}>
        <label htmlFor="new-todo">New todo</label>
        <input
          id="new-todo"
          value={newTitle}
          onChange={(event) => setNewTitle(event.target.value)}
        />
        <button type="submit">Add</button>
      </form>

      <ul>
        {todos.map((todo) => (
          <li key={todo.id}>
            <label>
              <input
                type="checkbox"
                checked={todo.completed}
                onChange={() => toggleTodo(todo.id)}
              />
              <span style={{ textDecoration: todo.completed ? 'line-through' : 'none' }}>
                {todo.title}
              </span>
            </label>
            <button type="button" onClick={() => removeTodo(todo.id)}>
              Remove
            </button>
          </li>
        ))}
      </ul>
    </main>
  );
}

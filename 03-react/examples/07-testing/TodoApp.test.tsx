import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { TodoApp } from '../01-basics/TodoApp';

describe('TodoApp', () => {
  beforeEach(() => {
    vi.spyOn(crypto, 'randomUUID').mockReturnValue(
      '00000000-0000-4000-8000-000000000000',
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders initial todos and remaining count', () => {
    render(<TodoApp />);

    expect(screen.getByRole('heading', { name: /todo app/i })).toBeInTheDocument();
    expect(screen.getByText('Learn JSX')).toBeInTheDocument();
    expect(screen.getByText('Practice hooks')).toBeInTheDocument();
    expect(screen.getByText('1 remaining')).toBeInTheDocument();
  });

  it('adds a todo from the form', async () => {
    const user = userEvent.setup();
    render(<TodoApp />);

    await user.type(screen.getByLabelText(/new todo/i), 'Write React tests');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(screen.getByText('Write React tests')).toBeInTheDocument();
    expect(screen.getByLabelText(/new todo/i)).toHaveValue('');
    expect(screen.getByText('2 remaining')).toBeInTheDocument();
  });

  it('does not add blank todos', async () => {
    const user = userEvent.setup();
    render(<TodoApp />);

    await user.type(screen.getByLabelText(/new todo/i), '   ');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(screen.getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByText('1 remaining')).toBeInTheDocument();
  });

  it('toggles a todo completion state', async () => {
    const user = userEvent.setup();
    render(<TodoApp />);

    await user.click(screen.getByRole('checkbox', { name: /practice hooks/i }));

    expect(screen.getByText('0 remaining')).toBeInTheDocument();
  });

  it('removes a todo', async () => {
    const user = userEvent.setup();
    render(<TodoApp />);

    const practiceHooksItem = screen.getByText('Practice hooks').closest('li');
    expect(practiceHooksItem).not.toBeNull();

    await user.click(
      within(practiceHooksItem as HTMLElement).getByRole('button', {
        name: /remove/i,
      }),
    );

    expect(screen.queryByText('Practice hooks')).not.toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(1);
  });
});

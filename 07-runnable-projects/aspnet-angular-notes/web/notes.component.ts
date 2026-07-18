import { CommonModule } from "@angular/common";
import { Component, OnInit, computed, signal } from "@angular/core";
import { FormsModule } from "@angular/forms";

type UserDto = {
  id: string;
  email: string;
};

type AuthResponse = {
  token: string;
  expiresAt: string;
  user: UserDto;
};

type NoteDto = {
  id: string;
  title: string;
  body: string;
  isPinned: boolean;
  createdAt: string;
  updatedAt: string;
};

type NoteDraft = {
  title: string;
  body: string;
  isPinned: boolean;
};

const API_BASE_URL =
  (globalThis as unknown as { NG_APP_API_BASE_URL?: string }).NG_APP_API_BASE_URL ?? "http://localhost:8081";

@Component({
  selector: "app-notes",
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <main class="shell">
      <header class="hero">
        <div>
          <p class="eyebrow">ASP.NET Core + Angular + JWT</p>
          <h1>Notes CRUD</h1>
          <p>
            Register or sign in, then create, search, pin, edit, and delete notes scoped to your JWT subject.
          </p>
        </div>
        <aside class="session" *ngIf="user(); else signedOut">
          <strong>{{ user()?.email }}</strong>
          <span>Token expires {{ expiresAt() | date: "shortTime" }}</span>
          <button type="button" class="secondary" (click)="signOut()">Sign out</button>
        </aside>
        <ng-template #signedOut>
          <aside class="session muted">Signed out</aside>
        </ng-template>
      </header>

      <p class="status" [class.error]="error()">{{ error() || status() }}</p>

      <section class="grid" *ngIf="!token(); else notesApp">
        <form class="card" (ngSubmit)="register()">
          <h2>Register</h2>
          <label>
            Email
            <input name="registerEmail" [(ngModel)]="registerEmail" autocomplete="email" />
          </label>
          <label>
            Password
            <input name="registerPassword" [(ngModel)]="registerPassword" type="password" />
          </label>
          <button type="submit">Create account</button>
        </form>

        <form class="card" (ngSubmit)="login()">
          <h2>Sign in</h2>
          <label>
            Email
            <input name="loginEmail" [(ngModel)]="loginEmail" autocomplete="email" />
          </label>
          <label>
            Password
            <input name="loginPassword" [(ngModel)]="loginPassword" type="password" />
          </label>
          <button type="submit">Sign in</button>
        </form>
      </section>

      <ng-template #notesApp>
        <section class="grid">
          <form class="card" (ngSubmit)="createNote()">
            <h2>Create note</h2>
            <label>
              Title
              <input name="newTitle" [(ngModel)]="draft.title" maxlength="160" />
            </label>
            <label>
              Body
              <textarea name="newBody" [(ngModel)]="draft.body" rows="8"></textarea>
            </label>
            <label class="checkbox">
              <input name="newPinned" [(ngModel)]="draft.isPinned" type="checkbox" />
              Pin note
            </label>
            <button type="submit" [disabled]="loading()">Create</button>
          </form>

          <section class="card">
            <div class="toolbar">
              <h2>Your notes</h2>
              <input
                name="query"
                [(ngModel)]="query"
                (ngModelChange)="loadNotes()"
                placeholder="Search title or body"
              />
            </div>

            <p class="muted" *ngIf="filteredNotes().length === 0">No notes match the current filter.</p>

            <article class="note" *ngFor="let note of filteredNotes(); trackBy: trackById">
              <div class="note-header">
                <input [(ngModel)]="editCache[note.id].title" [name]="'title-' + note.id" />
                <label class="checkbox small">
                  <input
                    [(ngModel)]="editCache[note.id].isPinned"
                    [name]="'pin-' + note.id"
                    type="checkbox"
                  />
                  pinned
                </label>
              </div>

              <textarea [(ngModel)]="editCache[note.id].body" [name]="'body-' + note.id" rows="5"></textarea>

              <footer class="note-actions">
                <span>Updated {{ note.updatedAt | date: "medium" }}</span>
                <button type="button" class="secondary" (click)="saveNote(note.id)">Save</button>
                <button type="button" class="danger" (click)="deleteNote(note.id)">Delete</button>
              </footer>
            </article>
          </section>
        </section>
      </ng-template>
    </main>
  `,
  styles: [
    `
      .shell {
        max-width: 1120px;
        margin: 0 auto;
        padding: 32px;
        color: #172033;
        font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, sans-serif;
      }

      .hero {
        display: flex;
        align-items: flex-start;
        justify-content: space-between;
        gap: 24px;
        margin-bottom: 18px;
      }

      .eyebrow {
        margin: 0;
        color: #7c3aed;
        font-weight: 800;
        letter-spacing: 0.08em;
        text-transform: uppercase;
      }

      h1 {
        margin: 8px 0;
        font-size: 42px;
      }

      h2 {
        margin-top: 0;
      }

      .session,
      .status {
        display: grid;
        gap: 6px;
        border: 1px solid #ddd6fe;
        border-radius: 16px;
        background: #f5f3ff;
        padding: 12px 14px;
      }

      .status {
        margin-bottom: 18px;
      }

      .error {
        border-color: #fecaca;
        background: #fef2f2;
        color: #991b1b;
      }

      .grid {
        display: grid;
        grid-template-columns: minmax(300px, 0.8fr) minmax(420px, 1.2fr);
        gap: 22px;
        align-items: start;
      }

      .card,
      .note {
        border: 1px solid #e2e8f0;
        border-radius: 18px;
        background: white;
        box-shadow: 0 12px 30px rgba(15, 23, 42, 0.06);
        padding: 18px;
      }

      .note {
        margin-top: 14px;
      }

      label {
        display: grid;
        gap: 6px;
        margin-bottom: 12px;
        font-weight: 700;
      }

      input,
      textarea {
        width: 100%;
        box-sizing: border-box;
        border: 1px solid #cbd5e1;
        border-radius: 12px;
        padding: 10px 12px;
        font: inherit;
      }

      textarea {
        resize: vertical;
      }

      button {
        border: 0;
        border-radius: 12px;
        background: #7c3aed;
        color: white;
        cursor: pointer;
        font-weight: 800;
        padding: 10px 14px;
      }

      button:disabled {
        cursor: not-allowed;
        opacity: 0.55;
      }

      .secondary {
        border: 1px solid #cbd5e1;
        background: white;
        color: #334155;
      }

      .danger {
        background: #dc2626;
      }

      .checkbox {
        display: flex;
        align-items: center;
        gap: 8px;
      }

      .checkbox input {
        width: auto;
      }

      .small {
        margin: 0;
        color: #475569;
        font-size: 14px;
      }

      .toolbar,
      .note-header,
      .note-actions {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
      }

      .toolbar input {
        max-width: 280px;
      }

      .note-actions {
        margin-top: 10px;
      }

      .muted {
        color: #64748b;
      }
    `,
  ],
})
export class NotesComponent implements OnInit {
  registerEmail = "demo@example.com";
  registerPassword = "CorrectHorseBatteryStaple";
  loginEmail = "demo@example.com";
  loginPassword = "CorrectHorseBatteryStaple";
  query = "";
  draft: NoteDraft = {
    title: "Interview prep note",
    body: "Explain why every notes query must include user_id from the JWT subject.",
    isPinned: true,
  };

  token = signal<string | null>(localStorage.getItem("notes.token"));
  user = signal<UserDto | null>(readJson<UserDto>("notes.user"));
  expiresAt = signal<string | null>(localStorage.getItem("notes.expiresAt"));
  notes = signal<NoteDto[]>([]);
  loading = signal(false);
  status = signal("Ready.");
  error = signal("");
  editCache: Record<string, NoteDraft> = {};

  filteredNotes = computed(() => {
    const text = this.query.trim().toLowerCase();
    if (!text) {
      return this.notes();
    }

    return this.notes().filter(
      (note) => note.title.toLowerCase().includes(text) || note.body.toLowerCase().includes(text),
    );
  });

  ngOnInit(): void {
    if (this.token()) {
      void this.loadNotes();
    }
  }

  async register(): Promise<void> {
    const response = await this.request<AuthResponse>("/auth/register", {
      method: "POST",
      body: JSON.stringify({ email: this.registerEmail, password: this.registerPassword }),
      anonymous: true,
    });
    this.acceptAuth(response);
    await this.loadNotes();
  }

  async login(): Promise<void> {
    const response = await this.request<AuthResponse>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email: this.loginEmail, password: this.loginPassword }),
      anonymous: true,
    });
    this.acceptAuth(response);
    await this.loadNotes();
  }

  async loadNotes(): Promise<void> {
    if (!this.token()) {
      return;
    }

    const params = this.query.trim() ? `?query=${encodeURIComponent(this.query.trim())}` : "";
    const notes = await this.request<NoteDto[]>(`/api/notes${params}`);
    this.notes.set(notes);
    this.editCache = Object.fromEntries(
      notes.map((note) => [
        note.id,
        { title: note.title, body: note.body, isPinned: note.isPinned },
      ]),
    );
  }

  async createNote(): Promise<void> {
    const created = await this.request<NoteDto>("/api/notes", {
      method: "POST",
      body: JSON.stringify(this.draft),
    });
    this.notes.set([created, ...this.notes()]);
    this.editCache[created.id] = {
      title: created.title,
      body: created.body,
      isPinned: created.isPinned,
    };
    this.draft = { title: "", body: "", isPinned: false };
    this.status.set("Note created.");
  }

  async saveNote(id: string): Promise<void> {
    const updated = await this.request<NoteDto>(`/api/notes/${id}`, {
      method: "PUT",
      body: JSON.stringify(this.editCache[id]),
    });
    this.notes.set(this.notes().map((note) => (note.id === id ? updated : note)));
    this.editCache[id] = {
      title: updated.title,
      body: updated.body,
      isPinned: updated.isPinned,
    };
    this.status.set("Note saved.");
  }

  async deleteNote(id: string): Promise<void> {
    await this.request<void>(`/api/notes/${id}`, { method: "DELETE" });
    this.notes.set(this.notes().filter((note) => note.id !== id));
    delete this.editCache[id];
    this.status.set("Note deleted.");
  }

  signOut(): void {
    localStorage.removeItem("notes.token");
    localStorage.removeItem("notes.user");
    localStorage.removeItem("notes.expiresAt");
    this.token.set(null);
    this.user.set(null);
    this.expiresAt.set(null);
    this.notes.set([]);
    this.status.set("Signed out.");
  }

  trackById(_: number, note: NoteDto): string {
    return note.id;
  }

  private acceptAuth(response: AuthResponse): void {
    localStorage.setItem("notes.token", response.token);
    localStorage.setItem("notes.user", JSON.stringify(response.user));
    localStorage.setItem("notes.expiresAt", response.expiresAt);
    this.token.set(response.token);
    this.user.set(response.user);
    this.expiresAt.set(response.expiresAt);
    this.status.set(`Signed in as ${response.user.email}.`);
  }

  private async request<T>(
    path: string,
    options: RequestInit & { anonymous?: boolean } = {},
  ): Promise<T> {
    this.loading.set(true);
    this.error.set("");

    const headers = new Headers(options.headers);
    headers.set("Content-Type", "application/json");
    if (!options.anonymous && this.token()) {
      headers.set("Authorization", `Bearer ${this.token()}`);
    }

    try {
      const response = await fetch(`${API_BASE_URL}${path}`, {
        ...options,
        headers,
      });

      if (!response.ok) {
        throw new Error(await response.text());
      }

      if (response.status === 204) {
        return undefined as T;
      }

      return (await response.json()) as T;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Request failed.";
      this.error.set(message);
      throw error;
    } finally {
      this.loading.set(false);
    }
  }
}

function readJson<T>(key: string): T | null {
  const value = localStorage.getItem(key);
  if (!value) {
    return null;
  }

  try {
    return JSON.parse(value) as T;
  } catch {
    localStorage.removeItem(key);
    return null;
  }
}


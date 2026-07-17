import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-hello',
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="hello-card">
      <h1>{{ greeting() }}</h1>

      <label>
        Name
        <input
          name="name"
          [ngModel]="name()"
          (ngModelChange)="name.set($event)"
        />
      </label>

      <button type="button" (click)="toggleExcited()">
        {{ excited() ? 'Calm down' : 'Get excited' }}
      </button>

      @if (name().trim().length === 0) {
        <p class="hint">Type your name to personalize the greeting.</p>
      } @else {
        <p>Welcome to modern Angular standalone components.</p>
      }
    </section>
  `,
  styles: [
    `
      .hello-card {
        display: grid;
        gap: 0.75rem;
        max-width: 24rem;
      }

      .hint {
        color: #6b7280;
      }
    `,
  ],
})
export class HelloComponent {
  name = signal('Angular learner');
  excited = signal(false);

  greeting = computed(() => {
    const punctuation = this.excited() ? '!' : '.';
    return `Hello, ${this.name() || 'friend'}${punctuation}`;
  });

  toggleExcited(): void {
    this.excited.update((value) => !value);
  }
}

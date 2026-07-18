import { bootstrapApplication } from "@angular/platform-browser";
import { provideAnimations } from "@angular/platform-browser/animations";
import { NotesComponent } from "../notes.component";

bootstrapApplication(NotesComponent, {
  providers: [provideAnimations()],
}).catch((error) => console.error(error));


# AI skills and agents

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Use AI to accelerate Remotion composition work while validating code, assets, prompts, and render behavior.

## Mental model

AI can generate storyboards, scene JSON, caption edits, style variants, and component drafts. The production boundary is still deterministic rendering and code review. Treat AI output as proposed source code or data that must be linted, reviewed, rendered, and secured.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Give AI assistants a constrained composition schema and brand rules.
2. Ask for scene data or small components before full-project rewrites.
3. Render stills and short ranges after every generated change.
4. Validate generated props with Zod or another schema layer.
5. Keep secrets, credentials, and private customer media out of prompts unless the environment is approved.
6. Build agent tasks around repeatable checks: create variants, update captions, produce thumbnails, or generate render props.


## Example


```ts
import {z} from 'zod';

export const aiSceneSchema = z.object({
  kind: z.enum(['title', 'stat', 'quote', 'outro']),
  durationInFrames: z.number().int().min(15).max(300),
  headline: z.string().max(80),
  accentColor: z.string().regex(/^#[0-9a-fA-F]{6}$/),
});

export const validateAiScene = (input: unknown) => aiSceneSchema.parse(input);
```

```text
Good agent task
- Generate three 9:16 title-card variants using this schema.
- Keep copy under 55 characters.
- Use only these brand colors.
- Output JSON only.
- Do not modify render infrastructure.
```


## Checklist


- AI output is schema-validated before preview or render.
- Generated code goes through normal review and linting.
- Assets have licenses and provenance recorded.
- The prompt cannot request secrets or unsafe filesystem/network access.
- Every generated visual is rendered as a still before batch export.


## Pitfalls


- AI-generated code can introduce nondeterministic timers, random values, or remote asset dependencies.
- Generated copy may violate brand, legal, or platform rules.
- Agents can over-edit infrastructure when asked for a visual change; scope prompts carefully.
- Training or prompt logs may become sensitive if they include customer media or unreleased campaigns.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Remotion AI docs: <https://www.remotion.dev/docs/ai/>
- Prompts: <https://remotion.dev/prompts>
- Security: <https://www.remotion.dev/docs/security>

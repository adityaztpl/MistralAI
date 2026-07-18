# Brownfield install

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Add Remotion to an existing React, Next.js, or Vite project without destabilizing the host application.

## Mental model

A brownfield install should isolate video compositions from application UI while sharing safe design tokens and components where useful. The goal is to add a Remotion entry point, compositions, scripts, and render dependencies without turning the whole app into a render bundle.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Identify whether Remotion will live in the same package, a workspace package, or a separate repository.
2. Add Remotion dependencies and a dedicated entry file such as `src/remotion/index.ts`.
3. Register compositions that import only render-safe modules.
4. Add scripts for Studio, still, draft render, and final render.
5. Audit shared app components for browser APIs, data fetching, auth context, and routing assumptions.
6. Add a small smoke render to prove the integration before migrating real scenes.


## Example


```text
src/
  app/                  existing product app
  components/           shared visual components
  remotion/
    index.ts            Remotion registerRoot entry
    Root.tsx            Composition registry
    compositions/
      ProductPromo.tsx
```

```jsonc
{
  "scripts": {
    "remotion": "remotion studio src/remotion/index.ts",
    "render:smoke": "remotion render src/remotion/index.ts Smoke out/smoke.mp4 --frames=0-30"
  }
}
```


## Checklist


- Remotion has a dedicated entry point.
- Shared components are render-safe and do not require live app providers unless explicitly supplied.
- Environment variables used in renders are documented.
- Output folders are ignored by Git.
- Existing app routes and bundling are not changed unnecessarily.


## Pitfalls


- Importing the full app root can pull in auth, routing, analytics, and browser-only side effects.
- Next.js server components and Remotion components have different runtime assumptions.
- CSS reset differences can change video layout if composition styles are not scoped.
- Installing packages without matching Remotion versions can create peer dependency issues.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Brownfield install: <https://www.remotion.dev/docs/brownfield>
- Installation: <https://www.remotion.dev/docs/install>
- Register root: <https://www.remotion.dev/docs/register-root>

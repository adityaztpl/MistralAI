# Remotion Studio workflow

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Use Studio for fast iteration, prop validation, frame inspection, and review handoff.

## Mental model

Studio is the creative cockpit. It lets developers and designers inspect compositions, scrub exact frames, change props, open the quick switcher, and trigger local renders. It should be treated as a preview and review surface, while production exports should be repeatable through CLI or API.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Start Studio from a package script and keep the command stable across the team.
2. Register compositions with clear ids and useful `defaultProps`.
3. Use schemas for editable props so Studio input is safe and discoverable.
4. Scrub scene boundaries and add comments or markers in docs for important frames.
5. Render drafts from Studio only for creative review.
6. Convert approved settings into CLI or server-render parameters before release.


## Example


```jsonc
{
  "scripts": {
    "start": "remotion studio",
    "render:approved": "remotion render src/index.ts ApprovedSpot out/approved.mp4 --props=./props/approved.json"
  }
}
```

```tsx
<Composition
  id="ApprovedSpot"
  component={ApprovedSpot}
  durationInFrames={240}
  fps={30}
  width={1920}
  height={1080}
  defaultProps={{brand: 'Acme', theme: 'dark'}}
/>
```


## Checklist


- Composition ids are stable and descriptive.
- Default props represent a realistic happy path.
- Important frame ranges are documented for QA.
- Studio-only previews are not the only release artifact path.
- Designers know how to reproduce the same preview locally.


## Pitfalls


- Changing props in Studio but not saving them to JSON can lead to unreproducible approvals.
- Relying on local media files outside `public` or configured paths breaks teammates and render workers.
- Preview playback performance is not the same as final render throughput.
- Multiple compositions with vague ids make automation error-prone.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Studio: <https://www.remotion.dev/docs/studio>
- Studio shortcuts: <https://www.remotion.dev/docs/studio/shortcuts>
- Interactivity: <https://www.remotion.dev/docs/studio/interactivity>

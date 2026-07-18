# Editor starter, timeline, and recorder

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Understand how Remotion can power product-facing video editors, timeline UIs, and recording workflows.

## Mental model

Remotion is often embedded into tools where users assemble clips, captions, scenes, and brand settings. The editor UI manipulates structured data; Remotion previews that data and renders final artifacts. Timeline and recorder concepts are product layers around the rendering engine.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Model the project as data: scenes, tracks, assets, captions, transitions, and global settings.
2. Use Player for preview in the editor, not full render on every edit.
3. Convert timeline data into Remotion `Sequence` and component props.
4. Persist edits as JSON with versioned schemas.
5. Use recorder flows to capture video/audio inputs, then transcode or normalize before rendering.
6. Send final renders to a backend worker or cloud render provider.


## Example


```ts
type TimelineScene = {
  id: string;
  from: number;
  durationInFrames: number;
  type: 'title' | 'clip' | 'stats' | 'outro';
  props: Record<string, unknown>;
};
```

```tsx
import {Sequence} from 'remotion';

export const TimelineComposition = ({scenes}: {scenes: TimelineScene[]}) => (
  <>
    {scenes.map((scene) => (
      <Sequence key={scene.id} from={scene.from} durationInFrames={scene.durationInFrames}>
        <SceneRenderer scene={scene} />
      </Sequence>
    ))}
  </>
);
```


## Checklist


- Timeline data is serializable and versioned.
- Preview uses the same scene renderer as final export.
- User uploads are scanned, normalized, and permission-checked.
- Timeline edits are debounced and autosaved.
- Render jobs reference a snapshot of the timeline, not a mutable draft.


## Pitfalls


- Rendering on every timeline drag is too expensive; preview should be lightweight.
- Mutable editor state can change after a render starts unless jobs snapshot inputs.
- Uploaded media can have variable frame rates, codecs, and audio channels that need normalization.
- Product editors need undo/redo and asset cleanup, not only Remotion code.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Editor starter: <https://www.remotion.dev/docs/editor-starter>
- Timeline: <https://www.remotion.dev/docs/timeline>
- Recorder: <https://www.remotion.dev/docs/recorder>
- Player: <https://www.remotion.dev/docs/player/>

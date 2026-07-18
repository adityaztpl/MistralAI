# Captions overview

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Design a caption pipeline from transcript to timed on-screen text and exported subtitle files.

## Mental model

Captions are timed text. Remotion renders them by comparing the current frame time with caption start and end times. A robust pipeline stores normalized captions, separates display styling from transcript data, and keeps export formats in sync with the video render.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Import transcript data from a human editor, ASR provider, Whisper, ElevenLabs, or an SRT/VTT file.
2. Normalize all timing to seconds or milliseconds at ingestion, then convert to frames at display time.
3. Split captions into readable chunks; do not show entire paragraphs.
4. Store raw transcript, normalized captions, and display settings separately.
5. Preview captions in the final aspect ratio and platform safe area.
6. Export sidecar subtitles when the publishing platform supports them.


## Example


```ts
type Caption = {
  text: string;
  startMs: number;
  endMs: number;
};

export const getActiveCaption = (captions: Caption[], frame: number, fps: number) => {
  const timeMs = (frame / fps) * 1000;
  return captions.find((caption) => timeMs >= caption.startMs && timeMs < caption.endMs);
};
```

```tsx
import {useCurrentFrame, useVideoConfig} from 'remotion';

export const SimpleCaption = ({captions}: {captions: Caption[]}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const caption = getActiveCaption(captions, frame, fps);
  return <div className="caption">{caption?.text}</div>;
};
```


## Checklist


- Caption timings are stored in one canonical unit.
- Caption chunks are short enough to read on mobile.
- Font, contrast, shadow/stroke, and safe area are tested over busy footage.
- The render and subtitle export use the same source data.
- Speaker labels and sound cues are included when accessibility requires them.


## Pitfalls


- Word timings from ASR are often imperfect; allow editorial correction.
- Long captions can overflow vertical layouts.
- Burned-in captions cannot be turned off, so they should not be the only accessibility strategy when sidecars are possible.
- Using frame numbers as persistent transcript storage makes retiming harder if FPS changes.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Captions guide: <https://www.remotion.dev/docs/captions/>
- Importing captions: <https://www.remotion.dev/docs/captions/importing>
- Displaying captions: <https://www.remotion.dev/docs/captions/displaying>

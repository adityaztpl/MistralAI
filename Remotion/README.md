# Remotion Knowledge Dump

Remotion is a React-based framework for making videos programmatically. Instead
of placing clips on a visual-only timeline, you express each frame of a video as
React UI. Remotion renders those frames with browser technology and turns them
into videos, GIFs, image sequences, still images, or interactive previews.

Official docs: <https://www.remotion.dev/docs/>

## What Remotion is

Remotion lets you build motion graphics, explainers, social videos, dynamic
personalized videos, product demos, automated reports, and video editing tools
using the web stack:

- React components describe visual scenes.
- TypeScript types describe inputs and reusable scene contracts.
- CSS, SVG, Canvas, WebGL, Three.js, HTML media, and fonts can all be used.
- Frame-based hooks such as `useCurrentFrame()` and `useVideoConfig()` make time
  deterministic.
- Primitives such as `<Composition>`, `<AbsoluteFill>`, `<Sequence>`,
  `<Series>`, `interpolate()`, `spring()`, and `interpolateColors()` are the
  core language of Remotion animations.

At its simplest, a Remotion video is a React component registered as a
`<Composition>`:

```tsx
// src/Root.tsx
import {Composition} from 'remotion';
import {LaunchVideo} from './LaunchVideo';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="LaunchVideo"
        component={LaunchVideo}
        durationInFrames={180}
        fps={30}
        width={1920}
        height={1080}
        defaultProps={{
          productName: 'Orbit Notes',
          tagline: 'Your workspace, in motion',
        }}
      />
    </>
  );
};
```

```tsx
// src/LaunchVideo.tsx
import {
  AbsoluteFill,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type LaunchVideoProps = {
  productName: string;
  tagline: string;
};

export const LaunchVideo: React.FC<LaunchVideoProps> = ({
  productName,
  tagline,
}) => {
  const frame = useCurrentFrame();
  const {fps, durationInFrames} = useVideoConfig();

  const entrance = spring({
    frame,
    fps,
    config: {damping: 14, stiffness: 120},
  });

  const fadeOut = interpolate(
    frame,
    [durationInFrames - 24, durationInFrames],
    [1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );

  return (
    <AbsoluteFill
      style={{
        background: 'linear-gradient(135deg, #111827, #2563eb)',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        opacity: fadeOut,
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1
        style={{
          fontSize: 112,
          margin: 0,
          transform: `scale(${entrance})`,
        }}
      >
        {productName}
      </h1>
      <p style={{fontSize: 42, marginTop: 24}}>{tagline}</p>
    </AbsoluteFill>
  );
};
```

## Quickstart

The official Remotion starter is `create-video`:

```bash
npx create-video@latest
```

For a blank project, the docs also show:

```bash
npx create-video@latest --yes --blank my-video
cd my-video
npm i
npm run dev
```

Common scripts in a generated project:

```bash
npm run dev      # Start Remotion Studio
npm run build    # Render the default composition
npx remotion render src/index.ts MyComp out/video.mp4
```

## System requirements note

Check the latest requirements in the official docs before setting up CI or a
rendering server: <https://www.remotion.dev/docs/>

At the time of this dump, the official getting-started page notes that Remotion
requires at least Node.js 16 or Bun 1.0.3, that macOS 15 or later is required
for supported macOS use, and that Linux distributions need a sufficiently recent
Libc plus additional browser/rendering packages. Alpine Linux and nixOS are
listed as unsupported in the official requirements.

## Folder map

```text
/workspace/Remotion
├── README.md
├── 00-overview.md
├── 01-fundamentals
│   ├── README.md
│   ├── 01-what-is-remotion.md
│   ├── 02-frames-fps-duration.md
│   ├── 03-project-structure.md
│   ├── 04-absolute-fill-and-layout.md
│   ├── 05-hooks-useCurrentFrame-useVideoConfig.md
│   └── 06-first-animation.md
├── 02-compositions
│   ├── README.md
│   ├── 01-composition-component.md
│   ├── 02-still-and-folder.md
│   ├── 03-multiple-compositions.md
│   ├── 04-calculate-metadata.md
│   └── 05-default-props-and-schema.md
└── 03-timing-animation
    ├── README.md
    ├── 01-interpolate.md
    ├── 02-spring.md
    ├── 03-easing.md
    ├── 04-interpolate-colors.md
    ├── 05-transforms-and-motion.md
    └── 06-delay-render-and-prefetch.md
```

## Learning path

1. **Understand the model**
   - Read `00-overview.md`.
   - Then read `01-fundamentals/01-what-is-remotion.md`.
   - Internalize that time is a frame number, not a mutable timer.

2. **Get comfortable with frame math**
   - Read `01-fundamentals/02-frames-fps-duration.md`.
   - Practice converting seconds to frames with `seconds * fps`.
   - Use `durationInFrames` and `fps` from `useVideoConfig()` instead of hard
     coding timing everywhere.

3. **Build simple layouts**
   - Read `01-fundamentals/04-absolute-fill-and-layout.md`.
   - Use `<AbsoluteFill>` for full-frame layers.
   - Use normal CSS flex, grid, positioning, and SVG for design.

4. **Animate one property**
   - Read `01-fundamentals/06-first-animation.md`.
   - Use `interpolate()` for deterministic property mapping.
   - Add `spring()` for physical motion.

5. **Register and organize render targets**
   - Read `02-compositions/01-composition-component.md`.
   - Add more than one `<Composition>`.
   - Use `<Still>` for static images and `<Folder>` to organize Studio.

6. **Make props and durations dynamic**
   - Read `02-compositions/04-calculate-metadata.md`.
   - Read `02-compositions/05-default-props-and-schema.md`.
   - Use serializable default props and schemas for configurable videos.

7. **Control timing at scene scale**
   - Read the entire `03-timing-animation` section.
   - Use `<Sequence>` for offsets and trimming.
   - Use `<Series>` when scenes should play one after another.
   - Use `delayRender()` and `continueRender()` for async data readiness.

## Official links

- Docs: <https://www.remotion.dev/docs/>
- Getting started: <https://www.remotion.dev/docs/>
- `<Composition>`: <https://www.remotion.dev/docs/composition>
- `useCurrentFrame()`: <https://www.remotion.dev/docs/use-current-frame>
- `useVideoConfig()`: <https://www.remotion.dev/docs/use-video-config>
- `interpolate()`: <https://www.remotion.dev/docs/interpolate>
- `spring()`: <https://www.remotion.dev/docs/spring>
- `<Sequence>`: <https://www.remotion.dev/docs/sequence>
- `<Series>`: <https://www.remotion.dev/docs/series>
- Player: <https://www.remotion.dev/docs/player>
- Lambda rendering: <https://www.remotion.dev/docs/lambda>
- Cloud Run rendering: <https://www.remotion.dev/docs/cloudrun>

## Mental model in one example

```tsx
import {AbsoluteFill, Sequence, Series, useCurrentFrame} from 'remotion';

const Scene: React.FC<{label: string; color: string}> = ({label, color}) => {
  const localFrame = useCurrentFrame();

  return (
    <AbsoluteFill
      style={{
        backgroundColor: color,
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontSize: 80,
      }}
    >
      {label}: local frame {localFrame}
    </AbsoluteFill>
  );
};

export const TimelineExample: React.FC = () => {
  return (
    <AbsoluteFill>
      <Series>
        <Series.Sequence durationInFrames={60}>
          <Scene label="Intro" color="#0f172a" />
        </Series.Sequence>
        <Series.Sequence durationInFrames={90}>
          <Scene label="Demo" color="#1d4ed8" />
        </Series.Sequence>
        <Series.Sequence durationInFrames={45}>
          <Scene label="CTA" color="#be123c" />
        </Series.Sequence>
      </Series>

      <Sequence from={30} durationInFrames={120}>
        <div
          style={{
            position: 'absolute',
            bottom: 64,
            left: 64,
            padding: '18px 28px',
            borderRadius: 999,
            background: 'rgba(255, 255, 255, 0.18)',
            color: 'white',
            fontSize: 28,
          }}
        >
          Overlay starts at composition frame 30
        </div>
      </Sequence>
    </AbsoluteFill>
  );
};
```

Key detail: `useCurrentFrame()` is local to the sequence context. The overlay
above starts at global frame 30, but a component inside that sequence sees frame
`0` when it mounts.

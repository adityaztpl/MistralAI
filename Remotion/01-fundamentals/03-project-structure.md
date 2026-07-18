# 03 - Project Structure

A generated Remotion project is a React/TypeScript project with a Remotion entry
point, a root component that registers compositions, and normal source files for
scene components, assets, styles, and utilities.

## Typical starter layout

Exact templates vary, but a common structure looks like this:

```text
my-video/
├── package.json
├── remotion.config.ts
├── tsconfig.json
├── public/
│   ├── logo.png
│   └── audio/
│       └── bed.mp3
└── src/
    ├── index.ts
    ├── Root.tsx
    ├── compositions/
    │   ├── ProductLaunch.tsx
    │   └── SquareCut.tsx
    ├── components/
    │   ├── LowerThird.tsx
    │   ├── Logo.tsx
    │   └── DebugTimecode.tsx
    ├── scenes/
    │   ├── Intro.tsx
    │   ├── FeatureDemo.tsx
    │   └── Outro.tsx
    ├── lib/
    │   ├── colors.ts
    │   └── timing.ts
    └── style.css
```

## Entry point

The entry point registers the root component:

```tsx
// src/index.ts
import {registerRoot} from 'remotion';
import {RemotionRoot} from './Root';

registerRoot(RemotionRoot);
```

This is the connection between Remotion's render process and your React tree.

## Root component

The root component returns one or more `<Composition>` and `<Still>` entries.

```tsx
// src/Root.tsx
import {Composition, Folder, Still} from 'remotion';
import {ProductLaunch} from './compositions/ProductLaunch';
import {SquareCut} from './compositions/SquareCut';
import {Thumbnail} from './compositions/Thumbnail';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Folder name="Videos">
        <Composition
          id="ProductLaunch"
          component={ProductLaunch}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={240}
          defaultProps={{
            productName: 'Orbit Notes',
          }}
        />

        <Composition
          id="SquareCut"
          component={SquareCut}
          width={1080}
          height={1080}
          fps={30}
          durationInFrames={180}
          defaultProps={{
            productName: 'Orbit Notes',
          }}
        />
      </Folder>

      <Folder name="Images">
        <Still
          id="Thumbnail"
          component={Thumbnail}
          width={1280}
          height={720}
          defaultProps={{
            productName: 'Orbit Notes',
          }}
        />
      </Folder>
    </>
  );
};
```

## Composition files

Composition files are high-level timelines. They usually orchestrate scenes and
global layers rather than containing every design detail inline.

```tsx
// src/compositions/ProductLaunch.tsx
import {AbsoluteFill, Sequence, Series} from 'remotion';
import {DebugTimecode} from '../components/DebugTimecode';
import {FeatureDemo} from '../scenes/FeatureDemo';
import {Intro} from '../scenes/Intro';
import {Outro} from '../scenes/Outro';

type ProductLaunchProps = {
  productName: string;
};

export const ProductLaunch: React.FC<ProductLaunchProps> = ({productName}) => {
  return (
    <AbsoluteFill style={{backgroundColor: '#020617'}}>
      <Series>
        <Series.Sequence durationInFrames={60} name="Intro">
          <Intro productName={productName} />
        </Series.Sequence>
        <Series.Sequence durationInFrames={120} name="Feature demo">
          <FeatureDemo />
        </Series.Sequence>
        <Series.Sequence durationInFrames={60} name="Outro">
          <Outro productName={productName} />
        </Series.Sequence>
      </Series>

      <Sequence from={0} durationInFrames={240} showInTimeline={false}>
        <DebugTimecode />
      </Sequence>
    </AbsoluteFill>
  );
};
```

## Scene files

Scenes should be reusable and focused. They can use `useCurrentFrame()` and
`useVideoConfig()` locally, especially when each scene is wrapped in its own
`<Series.Sequence>`.

```tsx
// src/scenes/Intro.tsx
import {
  AbsoluteFill,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type IntroProps = {
  productName: string;
};

export const Intro: React.FC<IntroProps> = ({productName}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const logoScale = spring({
    frame,
    fps,
    config: {damping: 12, stiffness: 110},
  });

  const subtitleOpacity = interpolate(frame, [20, 38], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        alignItems: 'center',
        justifyContent: 'center',
        color: 'white',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1 style={{fontSize: 110, margin: 0, transform: `scale(${logoScale})`}}>
        {productName}
      </h1>
      <p style={{fontSize: 38, opacity: subtitleOpacity}}>
        Notes that move with your team
      </p>
    </AbsoluteFill>
  );
};
```

## Component files

Components are normal React components. The most reusable ones should not need
to know about composition timing unless timing is part of their job.

```tsx
// src/components/Badge.tsx
type BadgeProps = {
  children: React.ReactNode;
  color: string;
};

export const Badge: React.FC<BadgeProps> = ({children, color}) => {
  return (
    <div
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        borderRadius: 999,
        padding: '10px 18px',
        background: color,
        color: 'white',
        fontWeight: 700,
      }}
    >
      {children}
    </div>
  );
};
```

## Utility files

Timing helpers and design constants keep compositions readable.

```ts
// src/lib/timing.ts
export const sec = (seconds: number, fps: number): number => {
  return Math.round(seconds * fps);
};

export const progressBetween = (
  frame: number,
  start: number,
  end: number,
): number => {
  if (frame <= start) {
    return 0;
  }

  if (frame >= end) {
    return 1;
  }

  return (frame - start) / (end - start);
};
```

```ts
// src/lib/colors.ts
export const colors = {
  ink: '#020617',
  slate: '#0f172a',
  blue: '#2563eb',
  cyan: '#22d3ee',
  white: '#ffffff',
};
```

## Static assets

Assets usually live in `public/` and are referenced with `staticFile()`:

```tsx
import {Img, staticFile} from 'remotion';

export const LogoImage: React.FC = () => {
  return (
    <Img
      src={staticFile('logo.png')}
      style={{
        width: 180,
        height: 180,
        objectFit: 'contain',
      }}
    />
  );
};
```

Audio and video assets use Remotion media components:

```tsx
import {Audio, staticFile} from 'remotion';

export const MusicBed: React.FC = () => {
  return <Audio src={staticFile('audio/bed.mp3')} volume={0.35} />;
};
```

## Project organization guidelines

- Keep `Root.tsx` focused on registration.
- Keep composition files focused on timeline and high-level layout.
- Keep scene files focused on visual story beats.
- Keep reusable UI in `components/`.
- Keep timing math in small utilities when reused.
- Keep assets in `public/`.
- Use TypeScript `type` aliases for props, especially with `defaultProps`.

## Example render command

Once a composition is registered, render it by ID:

```bash
npx remotion render src/index.ts ProductLaunch out/product-launch.mp4
```

With input props:

```bash
npx remotion render src/index.ts ProductLaunch out/acme.mp4 \
  --props='{"productName":"Acme Cloud"}'
```

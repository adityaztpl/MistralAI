# 02 - Compositions

In Remotion, a composition is a registered render target. A React component can
exist anywhere in your source tree, but Remotion Studio and the CLI only know how
to preview or render it after it has been registered with `<Composition>`.

This section covers:

- `<Composition>`
- `<Still>`
- `<Folder>`
- multiple compositions from shared scene code
- `calculateMetadata()`
- `defaultProps`
- schema-driven props with Zod

## Composition in one sentence

A composition gives a React component an ID, dimensions, FPS, duration, and
default props.

```tsx
import {Composition} from 'remotion';
import {LaunchVideo} from './LaunchVideo';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="LaunchVideo"
      component={LaunchVideo}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={240}
      defaultProps={{
        productName: 'Orbit Notes',
      }}
    />
  );
};
```

## Composition responsibilities

Use composition registration to define:

- What is renderable.
- The stable render ID.
- Pixel dimensions.
- Frame rate.
- Duration.
- Default input props.
- Optional schema for visual editing and validation.
- Optional metadata calculation for dynamic duration, dimensions, codec, and
  more.

Do not overload `Root.tsx` with scene implementation. Keep the registered
component separate:

```tsx
// Good root shape
<Composition id="A" component={A} {...config} />;
<Composition id="B" component={B} {...config} />;
```

```tsx
// Harder to maintain
<Composition
  id="EverythingInline"
  component={() => (
    <AbsoluteFill>
      {/* hundreds of lines */}
    </AbsoluteFill>
  )}
  {...config}
/>;
```

## Example section map

```text
02-compositions/
├── README.md
├── 01-composition-component.md
├── 02-still-and-folder.md
├── 03-multiple-compositions.md
├── 04-calculate-metadata.md
└── 05-default-props-and-schema.md
```

## Practical root template

```tsx
import {Composition, Folder, Still} from 'remotion';
import {z} from 'zod';
import {HeroVideo} from './compositions/HeroVideo';
import {HeroStill} from './compositions/HeroStill';

const heroSchema = z.object({
  headline: z.string(),
  subline: z.string(),
  accentColor: z.string(),
});

const defaultHeroProps: z.infer<typeof heroSchema> = {
  headline: 'Ship launch videos faster',
  subline: 'React components, frame-accurate animation, automated rendering',
  accentColor: '#38bdf8',
};

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Folder name="Hero campaign">
        <Composition
          id="HeroVideo"
          component={HeroVideo}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={210}
          schema={heroSchema}
          defaultProps={defaultHeroProps}
        />

        <Still
          id="HeroStill"
          component={HeroStill}
          width={1920}
          height={1080}
          schema={heroSchema}
          defaultProps={defaultHeroProps}
        />
      </Folder>
    </>
  );
};
```

## Learning goals

After this section, you should be able to:

- Register a video composition.
- Register a still image target.
- Organize Studio's sidebar with folders.
- Share scene code between multiple formats.
- Dynamically calculate metadata from props or data.
- Use typed default props and schema validation.

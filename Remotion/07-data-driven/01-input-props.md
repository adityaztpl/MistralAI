# Input props

Input props let one composition render many outputs. You define a typed React component, register it with `defaultProps`, then override those defaults from the CLI, Studio render dialog, Player, or renderer APIs.

## Typed component props

```tsx
import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

export type TitleCardProps = {
  title: string;
  subtitle: string;
  backgroundColor: string;
};

export const TitleCard: React.FC<TitleCardProps> = ({
  title,
  subtitle,
  backgroundColor,
}) => {
  const frame = useCurrentFrame();
  const y = interpolate(frame, [0, 30], [60, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        backgroundColor,
        justifyContent: 'center',
        alignItems: 'center',
        padding: 80,
      }}
    >
      <h1 style={{fontSize: 96, transform: `translateY(${y}px)`, margin: 0}}>
        {title}
      </h1>
      <p style={{fontSize: 38, opacity: 0.72}}>{subtitle}</p>
    </AbsoluteFill>
  );
};
```

## Register with `defaultProps`

```tsx
import React from 'react';
import {Composition} from 'remotion';
import {TitleCard, type TitleCardProps} from './TitleCard';

const defaultTitleCardProps: TitleCardProps = {
  title: 'Launch faster',
  subtitle: 'Automated videos with React',
  backgroundColor: '#dbeafe',
};

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="title-card"
      component={TitleCard}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={120}
      defaultProps={defaultTitleCardProps}
    />
  );
};
```

`defaultProps` provide a usable Studio preview and a fallback for renders that do not pass input props.

## CLI input props

Inline JSON:

```bash
npx remotion render title-card out/title-card.mp4 \
  --props='{"title":"Black Friday","subtitle":"40% off annual plans","backgroundColor":"#fef3c7"}'
```

JSON file:

```json
{
  "title": "Partner webinar",
  "subtitle": "Thursday at 10 AM",
  "backgroundColor": "#dcfce7"
}
```

```bash
npx remotion render title-card out/webinar.mp4 --props=./props/webinar.json
```

## Prop resolution

During rendering, three inputs matter:

1. `defaultProps`: fallback values from `<Composition>`.
2. `inputProps`: values passed by CLI, Studio render dialog, or renderer API.
3. `calculateMetadata()`: optional transform of props and composition metadata.

The final result is passed to your component as normal React props.

## Root-level access with `getInputProps()`

Most components do **not** need `getInputProps()`. Use regular props in your composition component. Use `getInputProps()` only when the root component needs to know input data, for example to register different compositions.

```tsx
import React from 'react';
import {Composition, getInputProps} from 'remotion';
import {TitleCard} from './TitleCard';

type RootInput = {
  vertical?: boolean;
};

export const RemotionRoot: React.FC = () => {
  const input = getInputProps() as RootInput;
  const vertical = input.vertical ?? false;

  return (
    <Composition
      id="title-card"
      component={TitleCard}
      width={vertical ? 1080 : 1920}
      height={vertical ? 1920 : 1080}
      fps={30}
      durationInFrames={120}
      defaultProps={{
        title: 'Launch faster',
        subtitle: 'React-powered video',
        backgroundColor: '#dbeafe',
      }}
    />
  );
};
```

In Player and client-side rendering contexts, `getInputProps()` is not available; pass props through the Player's `inputProps` instead.

## Serializable props

`defaultProps` and renderer `inputProps` must be JSON-serializable plain objects. Avoid:

- Functions
- Class instances
- DOM nodes
- Large binary blobs
- Huge arrays that slow Studio and render selection

Allowed special cases include `Date`, `Map`, `Set`, and `staticFile()` in `defaultProps`, which Remotion serializes.

## Passing components normally

A composition component is still a React component. You can reuse it inside other scenes.

```tsx
import React from 'react';
import {Series} from 'remotion';
import {TitleCard} from './TitleCard';

export const CombinedVideo: React.FC = () => {
  return (
    <Series>
      <Series.Sequence durationInFrames={120}>
        <TitleCard
          title="Part one"
          subtitle="Direct component props"
          backgroundColor="#dbeafe"
        />
      </Series.Sequence>
      <Series.Sequence durationInFrames={120}>
        <TitleCard
          title="Part two"
          subtitle="Another scene"
          backgroundColor="#fee2e2"
        />
      </Series.Sequence>
    </Series>
  );
};
```

## Checklist

- [ ] Define a `type` for props; avoid `interface` if you want best Remotion default prop inference.
- [ ] Register every prop-taking composition with `defaultProps`.
- [ ] Keep props JSON-friendly and compact.
- [ ] Pass the same `inputProps` to `selectComposition()` and `renderMedia()`.
- [ ] Use `calculateMetadata()` for transformation, validation, and dynamic duration.

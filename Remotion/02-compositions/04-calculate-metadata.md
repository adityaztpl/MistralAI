# 04 - `calculateMetadata()`

`calculateMetadata()` lets a composition determine metadata dynamically. Instead
of hard-coding every duration, dimension, or render setting, you can calculate
values from input props or fetched data.

Use it when:

- duration depends on the number of scenes,
- dimensions depend on a selected format,
- props need normalization before rendering,
- render settings depend on content,
- async data is needed before metadata is known.

## Static metadata vs calculated metadata

Static:

```tsx
<Composition
  id="StaticVideo"
  component={StaticVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
/>;
```

Calculated:

```tsx
<Composition
  id="DynamicVideo"
  component={DynamicVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
  calculateMetadata={({props}) => {
    return {
      durationInFrames: props.slides.length * 90,
    };
  }}
  defaultProps={{
    slides: [
      {title: 'Intro', body: 'Welcome'},
      {title: 'Demo', body: 'See it work'},
    ],
  }}
/>;
```

The static values act as defaults; calculated metadata can override supported
fields.

## Example: duration from slides

```tsx
import {Composition} from 'remotion';

type Slide = {
  title: string;
  body: string;
};

type SlideDeckVideoProps = {
  slides: Slide[];
};

const secondsPerSlide = 3;
const fps = 30;

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="SlideDeck"
      component={SlideDeckVideo}
      width={1920}
      height={1080}
      fps={fps}
      durationInFrames={secondsPerSlide * fps}
      defaultProps={{
        slides: [
          {title: 'Plan', body: 'Structure the story'},
          {title: 'Build', body: 'Animate with React'},
          {title: 'Render', body: 'Export repeatably'},
        ],
      }}
      calculateMetadata={({props}) => {
        return {
          durationInFrames: props.slides.length * secondsPerSlide * fps,
        };
      }}
    />
  );
};
```

Composition implementation:

```tsx
import {AbsoluteFill, Series} from 'remotion';

export const SlideDeckVideo: React.FC<SlideDeckVideoProps> = ({slides}) => {
  return (
    <AbsoluteFill style={{background: '#020617'}}>
      <Series>
        {slides.map((slide) => (
          <Series.Sequence key={slide.title} durationInFrames={90}>
            <SlideScene slide={slide} />
          </Series.Sequence>
        ))}
      </Series>
    </AbsoluteFill>
  );
};
```

## Example: dimensions from format prop

```tsx
type Format = 'landscape' | 'square' | 'vertical';

type FormatVideoProps = {
  format: Format;
  headline: string;
};

const dimensions: Record<Format, {width: number; height: number}> = {
  landscape: {width: 1920, height: 1080},
  square: {width: 1080, height: 1080},
  vertical: {width: 1080, height: 1920},
};

<Composition
  id="FormatVideo"
  component={FormatVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
  defaultProps={{
    format: 'landscape',
    headline: 'Adaptive by metadata',
  }}
  calculateMetadata={({props}) => {
    return dimensions[props.format];
  }}
/>;
```

Inside the component:

```tsx
import {AbsoluteFill, useVideoConfig} from 'remotion';

export const FormatVideo: React.FC<FormatVideoProps> = ({headline}) => {
  const {width, height} = useVideoConfig();
  const isVertical = height > width;

  return (
    <AbsoluteFill
      style={{
        background: '#111827',
        color: 'white',
        padding: isVertical ? 72 : 120,
        boxSizing: 'border-box',
        justifyContent: 'center',
      }}
    >
      <h1 style={{fontSize: isVertical ? 92 : 116}}>{headline}</h1>
    </AbsoluteFill>
  );
};
```

## Example: normalize props

`calculateMetadata()` can return transformed props. This is useful for deriving
values once and keeping the component simpler.

```tsx
type InputProps = {
  items: string[];
  secondsPerItem: number;
};

type NormalizedProps = InputProps & {
  itemCount: number;
  uppercaseItems: string[];
};

<Composition
  id="ItemsVideo"
  component={ItemsVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={90}
  defaultProps={{
    items: ['Plan', 'Build', 'Render'],
    secondsPerItem: 2,
  }}
  calculateMetadata={({props}) => {
    return {
      durationInFrames: props.items.length * props.secondsPerItem * 30,
      props: {
        ...props,
        itemCount: props.items.length,
        uppercaseItems: props.items.map((item) => item.toUpperCase()),
      } satisfies NormalizedProps,
    };
  }}
/>;
```

The component can receive normalized values:

```tsx
export const ItemsVideo: React.FC<NormalizedProps> = ({
  uppercaseItems,
  itemCount,
}) => {
  return (
    <div>
      {itemCount} items: {uppercaseItems.join(', ')}
    </div>
  );
};
```

## Async metadata

Metadata can be calculated asynchronously. Use this when metadata depends on a
remote manifest or local data source available at render time.

```tsx
type Manifest = {
  title: string;
  scenes: Array<{
    id: string;
    durationInFrames: number;
  }>;
};

type ManifestVideoProps = {
  manifestUrl: string;
  title?: string;
  scenes?: Manifest['scenes'];
};

<Composition
  id="ManifestVideo"
  component={ManifestVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={120}
  defaultProps={{
    manifestUrl: 'https://example.com/video-manifest.json',
  }}
  calculateMetadata={async ({props}) => {
    const response = await fetch(props.manifestUrl);

    if (!response.ok) {
      throw new Error(`Could not load manifest: ${response.status}`);
    }

    const manifest = (await response.json()) as Manifest;
    const durationInFrames = manifest.scenes.reduce(
      (sum, scene) => sum + scene.durationInFrames,
      0,
    );

    return {
      durationInFrames,
      props: {
        ...props,
        title: manifest.title,
        scenes: manifest.scenes,
      },
    };
  }}
/>;
```

Keep async metadata stable. If the manifest changes between preview and render,
the output changes too.

## Error handling

Fail early when metadata is invalid:

```tsx
calculateMetadata={({props}) => {
  if (props.slides.length === 0) {
    throw new Error('SlideDeck requires at least one slide');
  }

  const durationInFrames = props.slides.reduce((sum, slide) => {
    if (slide.durationInFrames <= 0) {
      throw new Error(`Slide ${slide.title} has invalid duration`);
    }

    return sum + slide.durationInFrames;
  }, 0);

  return {durationInFrames};
}}
```

This is better than rendering a broken blank video.

## Dynamic codec or render settings

Current Remotion versions support metadata fields beyond duration and dimensions,
such as default codec/sample rate in certain APIs. Use this for project-level
defaults when appropriate:

```tsx
calculateMetadata={({props}) => {
  return {
    durationInFrames: props.kind === 'short' ? 90 : 240,
    defaultCodec: props.transparent ? 'prores' : 'h264',
  };
}}
```

Always verify supported return fields in the official docs for your installed
Remotion version.

## Avoid heavy work

`calculateMetadata()` should determine metadata, not render the whole world.

Good:

```tsx
calculateMetadata={({props}) => ({
  durationInFrames: props.scenes.length * 90,
})}
```

Risky:

```tsx
calculateMetadata={async () => {
  // Avoid slow, unnecessary processing here.
  await renderEveryAssetInAdvance();
  return {durationInFrames: 180};
}}
```

## Production checklist

- Validate required props.
- Make metadata deterministic.
- Keep fetches fast and cacheable.
- Throw clear errors for impossible inputs.
- Keep static defaults sensible for Studio startup.
- Derive duration from data when data controls timeline length.
- Use schemas with calculated metadata for safer editor workflows.

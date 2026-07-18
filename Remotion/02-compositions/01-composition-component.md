# 01 - The `<Composition>` Component

`<Composition>` registers a video so it appears in Remotion Studio and can be
rendered by ID. The official docs describe it as the component used to register
a video to make it renderable and visible in the development interface sidebar.

## Minimal composition

```tsx
import {Composition} from 'remotion';
import {MyVideo} from './MyVideo';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="MyVideo"
      component={MyVideo}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={180}
    />
  );
};
```

## Required props

### `id`

Unique composition identifier:

```tsx
id="ProductLaunch"
```

The ID is shown in Studio and used by render commands:

```bash
npx remotion render src/index.ts ProductLaunch out/product-launch.mp4
```

Composition IDs should be stable and contain only supported characters such as
letters, numbers, and hyphens.

### `component` or `lazyComponent`

Pass a component directly:

```tsx
<Composition id="Direct" component={DirectVideo} {...config} />
```

Or lazy-load it:

```tsx
<Composition
  id="Lazy"
  lazyComponent={() => import('./LazyVideo')}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={150}
/>
```

When using `lazyComponent`, the imported component should be the default export.

### `width` and `height`

Pixel dimensions:

```tsx
width={1920}
height={1080}
```

Common formats:

```tsx
const formats = {
  landscape: {width: 1920, height: 1080},
  square: {width: 1080, height: 1080},
  vertical: {width: 1080, height: 1920},
};
```

### `fps`

Frames per second:

```tsx
fps={30}
```

Higher FPS creates more frames for the same duration. A 10-second video at 30
FPS is 300 frames; at 60 FPS it is 600 frames.

### `durationInFrames`

Total length:

```tsx
durationInFrames={30 * 8}
```

The first frame is `0`, and the last frame is `durationInFrames - 1`.

## Optional props

### `defaultProps`

Default props are passed to the component and can be overridden in Studio or via
render input props.

```tsx
type CampaignVideoProps = {
  headline: string;
  brandColor: string;
};

const defaultProps: CampaignVideoProps = {
  headline: 'Launch faster',
  brandColor: '#2563eb',
};

<Composition
  id="CampaignVideo"
  component={CampaignVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={210}
  defaultProps={defaultProps}
/>;
```

Official guidance: default props must be JSON-serializable, with special support
for values such as `Date`, `Map`, `Set`, and `staticFile()` in current Remotion
versions. Avoid functions, classes, DOM nodes, promises, and other
non-serializable values.

### `schema`

Pass a Zod schema to validate props and enable visual editing:

```tsx
import {z} from 'zod';

const campaignSchema = z.object({
  headline: z.string(),
  brandColor: z.string(),
});

<Composition
  id="CampaignVideo"
  component={CampaignVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={210}
  schema={campaignSchema}
  defaultProps={{
    headline: 'Launch faster',
    brandColor: '#2563eb',
  }}
/>;
```

### `calculateMetadata`

Use `calculateMetadata()` when dimensions, duration, FPS, codec, or props depend
on input data. See `04-calculate-metadata.md`.

## Component typing

Use a `type` alias for props:

```tsx
type CampaignVideoProps = {
  headline: string;
  brandColor: string;
  features: string[];
};

export const CampaignVideo: React.FC<CampaignVideoProps> = ({
  headline,
  brandColor,
  features,
}) => {
  return (
    <div style={{color: brandColor}}>
      <h1>{headline}</h1>
      <ul>
        {features.map((feature) => (
          <li key={feature}>{feature}</li>
        ))}
      </ul>
    </div>
  );
};
```

Then register it:

```tsx
const defaultProps: CampaignVideoProps = {
  headline: 'Launch faster',
  brandColor: '#2563eb',
  features: ['Typed props', 'Frame-accurate motion', 'Automated render'],
};

<Composition
  id="CampaignVideo"
  component={CampaignVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={240}
  defaultProps={defaultProps}
/>;
```

## Multiple root entries

The root component can return many compositions:

```tsx
import {Composition} from 'remotion';
import {Landscape} from './Landscape';
import {Square} from './Square';
import {Vertical} from './Vertical';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="Landscape"
        component={Landscape}
        width={1920}
        height={1080}
        fps={30}
        durationInFrames={180}
      />
      <Composition
        id="Square"
        component={Square}
        width={1080}
        height={1080}
        fps={30}
        durationInFrames={180}
      />
      <Composition
        id="Vertical"
        component={Vertical}
        width={1080}
        height={1920}
        fps={30}
        durationInFrames={180}
      />
    </>
  );
};
```

## Composition as a contract

Treat each composition as a public contract:

```text
Composition ID: ProductLaunch
Dimensions:     1920x1080
FPS:            30
Duration:       240 frames
Props:          ProductLaunchProps
Output:         mp4 render target
```

This mindset helps with render automation. External services can invoke a render
by composition ID and props without knowing internal scene structure.

## Example: renderable campaign composition

```tsx
import {AbsoluteFill, Sequence, Series} from 'remotion';

type CampaignVideoProps = {
  company: string;
  headline: string;
  features: string[];
  brandColor: string;
};

export const CampaignVideo: React.FC<CampaignVideoProps> = ({
  company,
  headline,
  features,
  brandColor,
}) => {
  return (
    <AbsoluteFill style={{background: '#020617', color: 'white'}}>
      <Series>
        <Series.Sequence durationInFrames={60} name="Company intro">
          <Intro company={company} brandColor={brandColor} />
        </Series.Sequence>
        <Series.Sequence durationInFrames={120} name="Features">
          <FeatureList headline={headline} features={features} />
        </Series.Sequence>
        <Series.Sequence durationInFrames={60} name="CTA">
          <Outro company={company} brandColor={brandColor} />
        </Series.Sequence>
      </Series>

      <Sequence from={0} durationInFrames={240} showInTimeline={false}>
        <GradientVignette />
      </Sequence>
    </AbsoluteFill>
  );
};
```

This component becomes useful to Remotion once registered with
`<Composition>`.

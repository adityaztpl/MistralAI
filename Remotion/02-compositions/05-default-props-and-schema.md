# 05 - Default Props and Schema

Composition props are the main way to make Remotion videos configurable. A
template might use props for copy, colors, media URLs, datasets, formatting, or
feature flags.

`defaultProps` provide baseline values. `schema` validates and describes those
values using Zod.

## Default props

```tsx
type PromoVideoProps = {
  headline: string;
  subline: string;
  accentColor: string;
  features: string[];
};

const defaultPromoProps: PromoVideoProps = {
  headline: 'Create videos with React',
  subline: 'Design, animate, and render programmatically',
  accentColor: '#38bdf8',
  features: ['Typed props', 'Frame-based timing', 'Automated rendering'],
};

<Composition
  id="PromoVideo"
  component={PromoVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
  defaultProps={defaultPromoProps}
/>;
```

The component receives props normally:

```tsx
import {AbsoluteFill} from 'remotion';

export const PromoVideo: React.FC<PromoVideoProps> = ({
  headline,
  subline,
  accentColor,
  features,
}) => {
  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: 100,
        boxSizing: 'border-box',
        justifyContent: 'center',
      }}
    >
      <div style={{color: accentColor, fontSize: 30}}>Remotion</div>
      <h1 style={{fontSize: 96, maxWidth: 1100, margin: '18px 0'}}>
        {headline}
      </h1>
      <p style={{fontSize: 36, color: '#cbd5e1'}}>{subline}</p>
      <ul style={{fontSize: 28, marginTop: 40}}>
        {features.map((feature) => (
          <li key={feature}>{feature}</li>
        ))}
      </ul>
    </AbsoluteFill>
  );
};
```

## Props must be serializable

The official `<Composition>` docs emphasize that `defaultProps` should contain
serializable values. Avoid:

- functions,
- class instances,
- promises,
- React nodes,
- DOM nodes,
- open file handles,
- circular objects.

Good:

```tsx
defaultProps={{
  title: 'Quarterly report',
  values: [12, 18, 24],
  theme: {background: '#020617', accent: '#38bdf8'},
}}
```

Risky:

```tsx
defaultProps={{
  formatLabel: (value: number) => `${value}%`,
}}
```

If you need behavior, encode intent in data and implement behavior in the
component:

```tsx
type ChartProps = {
  valueFormat: 'percent' | 'currency' | 'plain';
};

const formatValue = (value: number, format: ChartProps['valueFormat']) => {
  if (format === 'percent') {
    return `${value}%`;
  }

  if (format === 'currency') {
    return `$${value.toLocaleString('en-US')}`;
  }

  return String(value);
};
```

## Zod schema

Schemas validate props and enable visual editing in Studio.

```tsx
import {z} from 'zod';

const promoSchema = z.object({
  headline: z.string(),
  subline: z.string(),
  accentColor: z.string(),
  features: z.array(z.string()),
});

type PromoVideoProps = z.infer<typeof promoSchema>;

<Composition
  id="PromoVideo"
  component={PromoVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
  schema={promoSchema}
  defaultProps={{
    headline: 'Create videos with React',
    subline: 'Design, animate, and render programmatically',
    accentColor: '#38bdf8',
    features: ['Typed props', 'Frame-based timing', 'Automated rendering'],
  }}
/>;
```

## Richer schema example

```tsx
import {z} from 'zod';

const sceneSchema = z.object({
  title: z.string(),
  body: z.string(),
  durationInFrames: z.number().int().positive(),
});

export const reportSchema = z.object({
  title: z.string(),
  company: z.string(),
  accentColor: z.string(),
  showDebug: z.boolean(),
  scenes: z.array(sceneSchema).min(1),
});

export type ReportVideoProps = z.infer<typeof reportSchema>;
```

Usage:

```tsx
const defaultReportProps: ReportVideoProps = {
  title: 'Q2 Growth Report',
  company: 'Northstar Analytics',
  accentColor: '#22d3ee',
  showDebug: false,
  scenes: [
    {
      title: 'Revenue',
      body: 'Revenue grew 38% quarter over quarter.',
      durationInFrames: 90,
    },
    {
      title: 'Retention',
      body: 'Retention reached a new high.',
      durationInFrames: 90,
    },
  ],
};
```

Register:

```tsx
<Composition
  id="ReportVideo"
  component={ReportVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={180}
  schema={reportSchema}
  defaultProps={defaultReportProps}
  calculateMetadata={({props}) => {
    return {
      durationInFrames: props.scenes.reduce(
        (sum, scene) => sum + scene.durationInFrames,
        0,
      ),
    };
  }}
/>;
```

## Schema with enum-style choices

```tsx
const themeSchema = z.object({
  mode: z.enum(['light', 'dark', 'midnight']),
  accentColor: z.string(),
});

type ThemeProps = z.infer<typeof themeSchema>;

const themeToBackground = (mode: ThemeProps['mode']) => {
  if (mode === 'light') {
    return '#f8fafc';
  }

  if (mode === 'dark') {
    return '#111827';
  }

  return '#020617';
};
```

This gives editors clear choices and keeps component logic type-safe.

## Input props from render commands

Default props can be overridden at render time:

```bash
npx remotion render src/index.ts PromoVideo out/acme.mp4 \
  --props='{
    "headline":"Acme ships faster",
    "subline":"Automated release videos for every launch",
    "accentColor":"#f97316",
    "features":["One template","Many variants","Reliable renders"]
  }'
```

For production, render services typically pass input props through an API rather
than a shell command.

## Props for media

Use serializable references such as URLs or `staticFile()` paths:

```tsx
type TestimonialVideoProps = {
  customerName: string;
  headshotPath: string;
  quote: string;
};

const defaultProps: TestimonialVideoProps = {
  customerName: 'Ada Lovelace',
  headshotPath: 'customers/ada.png',
  quote: 'This changed how we ship product stories.',
};
```

Component:

```tsx
import {Img, staticFile} from 'remotion';

export const Headshot: React.FC<{path: string}> = ({path}) => {
  return (
    <Img
      src={staticFile(path)}
      style={{
        width: 220,
        height: 220,
        borderRadius: 999,
        objectFit: 'cover',
      }}
    />
  );
};
```

## Validate derived constraints

Zod can validate shape, while `calculateMetadata()` can validate constraints
that depend on relationships between fields.

```tsx
const tickerSchema = z.object({
  items: z.array(z.string()).min(1),
  secondsPerItem: z.number().positive(),
});

<Composition
  id="Ticker"
  component={Ticker}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={90}
  schema={tickerSchema}
  defaultProps={{
    items: ['Design', 'Animate', 'Render'],
    secondsPerItem: 1.5,
  }}
  calculateMetadata={({props}) => {
    const durationInFrames = Math.round(
      props.items.length * props.secondsPerItem * 30,
    );

    if (durationInFrames > 30 * 60) {
      throw new Error('Ticker videos must be 60 seconds or shorter');
    }

    return {durationInFrames};
  }}
/>;
```

## Best practices

- Define a `type` or infer one from a Zod schema.
- Keep default props small enough for Studio to load quickly.
- Store large datasets externally and pass a URL or ID when appropriate.
- Avoid functions in props.
- Validate user-editable fields with schemas.
- Use `calculateMetadata()` for dynamic duration and prop normalization.
- Keep prop names stable if external systems call render APIs.

## Complete example

```tsx
import {Composition} from 'remotion';
import {z} from 'zod';
import {ProductVideo} from './ProductVideo';

const productVideoSchema = z.object({
  productName: z.string(),
  tagline: z.string(),
  accentColor: z.string(),
  bullets: z.array(z.string()).min(1).max(5),
  secondsPerBullet: z.number().positive(),
});

type ProductVideoProps = z.infer<typeof productVideoSchema>;

const defaultProductVideoProps: ProductVideoProps = {
  productName: 'Orbit Notes',
  tagline: 'Your workspace, in motion',
  accentColor: '#38bdf8',
  bullets: ['Capture ideas', 'Share context', 'Launch faster'],
  secondsPerBullet: 1.25,
};

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="ProductVideo"
      component={ProductVideo}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={150}
      schema={productVideoSchema}
      defaultProps={defaultProductVideoProps}
      calculateMetadata={({props}) => {
        return {
          durationInFrames:
            60 + Math.round(props.bullets.length * props.secondsPerBullet * 30),
        };
      }}
    />
  );
};
```

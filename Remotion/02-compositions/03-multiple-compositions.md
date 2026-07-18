# 03 - Multiple Compositions

Real Remotion projects often register multiple compositions. You might need
landscape, square, and vertical cuts; different durations; still thumbnails; or
several template variants backed by shared scene components.

## Why create multiple compositions?

Common reasons:

- Same campaign in multiple aspect ratios.
- A full version and a short social cut.
- Preview/debug compositions for individual scenes.
- Per-client render targets.
- Stills and videos in one project.
- Separate templates for different data shapes.

## Same component, different dimensions

You can register the same component multiple times with different IDs and
dimensions:

```tsx
import {Composition} from 'remotion';
import {CampaignVideo} from './CampaignVideo';

const defaultProps = {
  headline: 'Launch reports automatically',
  accentColor: '#38bdf8',
};

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="Campaign-Landscape"
        component={CampaignVideo}
        width={1920}
        height={1080}
        fps={30}
        durationInFrames={180}
        defaultProps={defaultProps}
      />
      <Composition
        id="Campaign-Square"
        component={CampaignVideo}
        width={1080}
        height={1080}
        fps={30}
        durationInFrames={180}
        defaultProps={defaultProps}
      />
      <Composition
        id="Campaign-Vertical"
        component={CampaignVideo}
        width={1080}
        height={1920}
        fps={30}
        durationInFrames={180}
        defaultProps={defaultProps}
      />
    </>
  );
};
```

Inside the component, adapt to dimensions:

```tsx
import {AbsoluteFill, useVideoConfig} from 'remotion';

type CampaignVideoProps = {
  headline: string;
  accentColor: string;
};

export const CampaignVideo: React.FC<CampaignVideoProps> = ({
  headline,
  accentColor,
}) => {
  const {width, height} = useVideoConfig();
  const isVertical = height > width;
  const isSquare = height === width;

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: isVertical ? 72 : 110,
        boxSizing: 'border-box',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <div
        style={{
          fontSize: isVertical ? 92 : isSquare ? 78 : 104,
          lineHeight: 1,
          maxWidth: isVertical ? width - 144 : width * 0.7,
        }}
      >
        {headline}
      </div>
      <div
        style={{
          width: isVertical ? 160 : 240,
          height: 14,
          borderRadius: 999,
          marginTop: 36,
          background: accentColor,
        }}
      />
    </AbsoluteFill>
  );
};
```

## Shared scene, different wrappers

Sometimes responsive layout is not enough. You may want different composition
components that share smaller pieces.

```tsx
type HeroContentProps = {
  headline: string;
  accentColor: string;
};

const HeroContent: React.FC<HeroContentProps> = ({headline, accentColor}) => {
  return (
    <>
      <div style={{fontSize: 30, color: accentColor}}>New release</div>
      <h1 style={{fontSize: 'inherit', lineHeight: 1, margin: '16px 0 0'}}>
        {headline}
      </h1>
    </>
  );
};
```

Landscape wrapper:

```tsx
import {AbsoluteFill} from 'remotion';

export const LandscapeHero: React.FC<HeroContentProps> = (props) => {
  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: 110,
        boxSizing: 'border-box',
        justifyContent: 'center',
        fontSize: 110,
      }}
    >
      <div style={{maxWidth: 1100}}>
        <HeroContent {...props} />
      </div>
    </AbsoluteFill>
  );
};
```

Vertical wrapper:

```tsx
import {AbsoluteFill} from 'remotion';

export const VerticalHero: React.FC<HeroContentProps> = (props) => {
  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: 72,
        boxSizing: 'border-box',
        justifyContent: 'flex-end',
        fontSize: 92,
      }}
    >
      <div style={{marginBottom: 180}}>
        <HeroContent {...props} />
      </div>
    </AbsoluteFill>
  );
};
```

Registration:

```tsx
<Composition
  id="Hero-Landscape"
  component={LandscapeHero}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={150}
  defaultProps={defaultProps}
/>;

<Composition
  id="Hero-Vertical"
  component={VerticalHero}
  width={1080}
  height={1920}
  fps={30}
  durationInFrames={150}
  defaultProps={defaultProps}
/>;
```

## Scene preview compositions

For complex videos, register individual scenes as debug compositions:

```tsx
import {Composition, Folder} from 'remotion';
import {FeatureDemoScene} from './scenes/FeatureDemoScene';
import {IntroScene} from './scenes/IntroScene';
import {FullVideo} from './FullVideo';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition
        id="FullVideo"
        component={FullVideo}
        width={1920}
        height={1080}
        fps={30}
        durationInFrames={240}
      />

      <Folder name="Scene previews">
        <Composition
          id="Scene-Intro"
          component={IntroScene}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={60}
        />
        <Composition
          id="Scene-FeatureDemo"
          component={FeatureDemoScene}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={90}
        />
      </Folder>
    </>
  );
};
```

This makes it easy to work on a scene without scrubbing through the full video.

## A composition registry pattern

If a project has many formats, a typed registry can reduce repetition:

```tsx
type Format = {
  id: string;
  width: number;
  height: number;
  durationInFrames: number;
};

const formats: Format[] = [
  {id: 'Landscape', width: 1920, height: 1080, durationInFrames: 180},
  {id: 'Square', width: 1080, height: 1080, durationInFrames: 180},
  {id: 'Vertical', width: 1080, height: 1920, durationInFrames: 180},
];

export const RemotionRoot: React.FC = () => {
  return (
    <>
      {formats.map((format) => (
        <Composition
          key={format.id}
          id={`Campaign-${format.id}`}
          component={CampaignVideo}
          width={format.width}
          height={format.height}
          fps={30}
          durationInFrames={format.durationInFrames}
          defaultProps={{
            headline: 'One template, many outputs',
            accentColor: '#38bdf8',
          }}
        />
      ))}
    </>
  );
};
```

Keep the abstraction simple. If different formats need very different layouts,
separate components are clearer.

## Different durations

A short cut can share content but use a shorter timeline:

```tsx
const sharedProps = {
  productName: 'Orbit Notes',
  accentColor: '#2563eb',
};

<Composition
  id="ProductLaunch-Full"
  component={ProductLaunchFull}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={30 * 12}
  defaultProps={sharedProps}
/>;

<Composition
  id="ProductLaunch-Short"
  component={ProductLaunchShort}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={30 * 5}
  defaultProps={sharedProps}
/>;
```

## Render automation implications

Multiple compositions give external systems stable choices:

```ts
type RenderRequest = {
  composition:
    | 'Campaign-Landscape'
    | 'Campaign-Square'
    | 'Campaign-Vertical';
  props: {
    headline: string;
    accentColor: string;
  };
};
```

The render service does not need to know whether those outputs share components
internally. It only needs a composition ID and input props.

## Naming recommendations

- Put the campaign/template first: `Campaign-Landscape`.
- Put the variant second: `ProductLaunch-Short`.
- Use stable IDs; changing an ID can break render automation.
- Avoid vague IDs like `Video1`.
- Use folders for visual organization, not as a substitute for good IDs.

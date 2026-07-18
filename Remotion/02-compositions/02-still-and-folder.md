# 02 - `<Still>` and `<Folder>`

Video compositions are not the only render targets. Remotion also supports still
image render targets through `<Still>`, and Studio organization through
`<Folder>`.

## `<Still>`

`<Still>` registers a component for static image output. It has an ID, component,
dimensions, and optional props/schema, but no FPS or duration.

```tsx
import {Still} from 'remotion';
import {Thumbnail} from './Thumbnail';

export const RemotionRoot: React.FC = () => {
  return (
    <Still
      id="Thumbnail"
      component={Thumbnail}
      width={1280}
      height={720}
      defaultProps={{
        title: 'Build videos with React',
      }}
    />
  );
};
```

Render a still:

```bash
npx remotion still src/index.ts Thumbnail out/thumbnail.png
```

## Still component example

```tsx
import {AbsoluteFill} from 'remotion';

type ThumbnailProps = {
  title: string;
  eyebrow: string;
  accentColor: string;
};

export const Thumbnail: React.FC<ThumbnailProps> = ({
  title,
  eyebrow,
  accentColor,
}) => {
  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        fontFamily: 'Inter, Arial, sans-serif',
        padding: 80,
        boxSizing: 'border-box',
        justifyContent: 'center',
      }}
    >
      <div
        style={{
          width: 220,
          height: 12,
          borderRadius: 999,
          background: accentColor,
          marginBottom: 36,
        }}
      />
      <div style={{fontSize: 32, color: '#bfdbfe', marginBottom: 16}}>
        {eyebrow}
      </div>
      <h1 style={{fontSize: 92, lineHeight: 1.02, margin: 0, maxWidth: 980}}>
        {title}
      </h1>
    </AbsoluteFill>
  );
};
```

Register it:

```tsx
<Still
  id="LaunchThumbnail"
  component={Thumbnail}
  width={1280}
  height={720}
  defaultProps={{
    title: 'Launch videos at scale',
    eyebrow: 'Remotion workflow',
    accentColor: '#38bdf8',
  }}
/>;
```

## Still vs video composition

Use `<Still>` when:

- You need thumbnails.
- You need poster frames.
- You need social preview images.
- You need generated slides or title cards.
- There is no time-based animation.

Use `<Composition>` when:

- You need duration.
- You need FPS.
- You use `useCurrentFrame()` meaningfully.
- You render video, GIFs, or image sequences.

## Sharing code between Still and Composition

It is common to share design components between a still and a video.

```tsx
type HeroCardProps = {
  headline: string;
  accentColor: string;
  progress?: number;
};

export const HeroCard: React.FC<HeroCardProps> = ({
  headline,
  accentColor,
  progress = 1,
}) => {
  return (
    <div
      style={{
        transform: `scale(${0.9 + progress * 0.1})`,
        opacity: progress,
        border: `4px solid ${accentColor}`,
        borderRadius: 32,
        padding: 64,
        color: 'white',
      }}
    >
      <h1 style={{fontSize: 86, margin: 0}}>{headline}</h1>
    </div>
  );
};
```

Still:

```tsx
import {AbsoluteFill} from 'remotion';

export const HeroStill: React.FC<{headline: string; accentColor: string}> = (
  props,
) => {
  return (
    <AbsoluteFill
      style={{background: '#020617', alignItems: 'center', justifyContent: 'center'}}
    >
      <HeroCard {...props} progress={1} />
    </AbsoluteFill>
  );
};
```

Video:

```tsx
import {AbsoluteFill, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const HeroVideo: React.FC<{headline: string; accentColor: string}> = (
  props,
) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const progress = spring({frame, fps, config: {damping: 14}});

  return (
    <AbsoluteFill
      style={{background: '#020617', alignItems: 'center', justifyContent: 'center'}}
    >
      <HeroCard {...props} progress={progress} />
    </AbsoluteFill>
  );
};
```

## `<Folder>`

`<Folder>` organizes compositions and stills in the Remotion Studio sidebar.

```tsx
import {Composition, Folder, Still} from 'remotion';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Folder name="Campaign A">
        <Composition
          id="CampaignA-Landscape"
          component={CampaignVideo}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={180}
        />
        <Still
          id="CampaignA-Thumbnail"
          component={CampaignThumbnail}
          width={1280}
          height={720}
        />
      </Folder>

      <Folder name="Campaign B">
        <Composition
          id="CampaignB-Vertical"
          component={VerticalCampaignVideo}
          width={1080}
          height={1920}
          fps={30}
          durationInFrames={180}
        />
      </Folder>
    </>
  );
};
```

Folders do not change render behavior. They are organization for humans.

## Nested folders

```tsx
<Folder name="Client Work">
  <Folder name="Acme">
    <Composition id="Acme-Hero" component={AcmeHero} {...heroConfig} />
    <Still id="Acme-Thumbnail" component={AcmeThumbnail} {...stillConfig} />
  </Folder>
  <Folder name="Northstar">
    <Composition id="Northstar-Hero" component={NorthstarHero} {...heroConfig} />
  </Folder>
</Folder>
```

Use nesting when a project contains many render targets.

## Folder naming tips

- Use product, campaign, or feature names.
- Avoid relying on folders as part of render IDs.
- Keep composition IDs globally unique and descriptive.
- Use folders to reduce sidebar clutter.

## Combined root example

```tsx
import {Composition, Folder, Still} from 'remotion';
import {HeroStill} from './HeroStill';
import {HeroVideo} from './HeroVideo';
import {VerticalHeroVideo} from './VerticalHeroVideo';

const heroProps = {
  headline: 'Automated product videos',
  accentColor: '#22d3ee',
};

export const RemotionRoot: React.FC = () => {
  return (
    <Folder name="Hero package">
      <Composition
        id="Hero-Landscape"
        component={HeroVideo}
        width={1920}
        height={1080}
        fps={30}
        durationInFrames={180}
        defaultProps={heroProps}
      />
      <Composition
        id="Hero-Vertical"
        component={VerticalHeroVideo}
        width={1080}
        height={1920}
        fps={30}
        durationInFrames={180}
        defaultProps={heroProps}
      />
      <Still
        id="Hero-Thumbnail"
        component={HeroStill}
        width={1280}
        height={720}
        defaultProps={heroProps}
      />
    </Folder>
  );
};
```

# Instagram Reel vertical

Vertical social video has different constraints from landscape: larger type, stronger hooks, safe areas around platform UI, and fast visual beats.

## Composition specs

```tsx
<Composition
  id="InstagramReel"
  component={InstagramReel}
  durationInFrames={450}
  fps={30}
  width={1080}
  height={1920}
/>
```

## Component sketch

```tsx
import {AbsoluteFill, Sequence, interpolate, useCurrentFrame} from 'remotion';

const SafeArea = ({children}: {children: React.ReactNode}) => (
  <AbsoluteFill style={{padding: '180px 82px 240px'}}>{children}</AbsoluteFill>
);

const HookText = ({text}: {text: string}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, 12, 30], [0.92, 1.04, 1], {extrapolateRight: 'clamp'});
  return <h1 style={{fontSize: 104, lineHeight: 0.95, transform: `scale(${scale})`}}>{text}</h1>;
};

export const InstagramReel = () => (
  <AbsoluteFill style={{background: 'linear-gradient(#020617, #1e1b4b)', color: 'white', fontFamily: 'Inter'}}>
    <SafeArea>
      <Sequence from={0} durationInFrames={90}>
        <HookText text="Stop shipping boring launch videos" />
      </Sequence>
      <Sequence from={90} durationInFrames={120}>
        <h2 style={{fontSize: 84}}>Use React components as scenes.</h2>
      </Sequence>
      <Sequence from={210} durationInFrames={120}>
        <h2 style={{fontSize: 84}}>Render variants from data.</h2>
      </Sequence>
      <Sequence from={330} durationInFrames={120}>
        <h2 style={{fontSize: 92}}>Save this Remotion workflow.</h2>
      </Sequence>
    </SafeArea>
  </AbsoluteFill>
);
```

## Caption and safe-area notes

- Keep primary captions above the bottom UI zone.
- Leave the right side less crowded when posting to apps with action buttons.
- Use shorter lines than landscape videos.
- Render and review on a phone-sized screen, not only a desktop player.
- Add burned-in captions for silent autoplay and sidecar captions when the platform supports them.

## Render commands

```bash
npx remotion still src/index.ts InstagramReel out/reel-cover.png --frame=45
npx remotion render src/index.ts InstagramReel out/reel.mp4 --codec=h264 --crf=18
```

## Variations

- Add background footage with `OffthreadVideo`.
- Add animated captions from a transcript.
- Generate multiple hooks using `inputProps`.
- Render 4:5 and 1:1 cutdowns from the same scene data.

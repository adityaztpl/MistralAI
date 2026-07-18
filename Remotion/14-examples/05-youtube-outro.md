# YouTube outro

A YouTube outro usually contains a subscribe call-to-action, one or two video placeholders, a logo, and enough hold time for platform end-screen elements.

## Layout goals

- Leave visual boxes where YouTube end-screen videos can be placed.
- Keep a clear subscribe CTA and brand mark.
- Hold for at least 5-10 seconds depending on the channel template.
- Avoid text too close to edges or platform overlays.

## Component

```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

const Placeholder = ({label}: {label: string}) => (
  <div
    style={{
      height: 330,
      borderRadius: 30,
      border: '6px solid rgba(255,255,255,0.7)',
      background: 'rgba(255,255,255,0.08)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      fontSize: 38,
      fontWeight: 800,
    }}
  >
    {label}
  </div>
);

export const YoutubeOutro = ({channel = 'Acme Lab'}: {channel?: string}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 24], [0, 1], {extrapolateRight: 'clamp'});

  return (
    <AbsoluteFill style={{background: '#111827', color: 'white', padding: 90, fontFamily: 'Inter'}}>
      <div style={{opacity, display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 42, height: '100%'}}>
        <div style={{display: 'flex', flexDirection: 'column', justifyContent: 'center'}}>
          <div style={{fontSize: 42, color: '#f97316', fontWeight: 900}}>Subscribe</div>
          <h1 style={{fontSize: 94, margin: '18px 0'}}>More from {channel}</h1>
          <p style={{fontSize: 36, opacity: 0.78}}>Weekly product, design, and engineering breakdowns.</p>
        </div>
        <div style={{display: 'grid', gap: 32, alignContent: 'center'}}>
          <Placeholder label="Latest video" />
          <Placeholder label="Recommended" />
        </div>
      </div>
    </AbsoluteFill>
  );
};
```

## Render notes

```tsx
<Composition id="YoutubeOutro" component={YoutubeOutro} durationInFrames={300} fps={30} width={1920} height={1080} />
```

```bash
npx remotion render src/index.ts YoutubeOutro out/youtube-outro.mp4
```

## Production checklist

- Test the final upload in YouTube Studio with actual end-screen elements.
- Keep placeholders aligned to YouTube's clickable boxes.
- Export without important content underneath end-screen controls.
- If the outro follows a main video, make sure audio transitions cleanly.

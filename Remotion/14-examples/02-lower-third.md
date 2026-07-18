# Lower-third overlay

A lower third is a reusable overlay for names, roles, locations, captions, or source attribution. In Remotion, build it as a composition or nested component so it can be sequenced over video footage.

## Component

```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

export const LowerThird = ({
  name,
  role,
  accent = '#f97316',
}: {
  name: string;
  role: string;
  accent?: string;
}) => {
  const frame = useCurrentFrame();
  const progress = interpolate(frame, [0, 18, 120, 145], [0, 1, 1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{justifyContent: 'flex-end', padding: 80, pointerEvents: 'none'}}>
      <div
        style={{
          width: 760,
          background: 'rgba(2, 6, 23, 0.82)',
          borderLeft: `14px solid ${accent}`,
          borderRadius: 24,
          padding: '30px 38px',
          color: 'white',
          boxShadow: '0 24px 80px rgba(0, 0, 0, 0.45)',
          opacity: progress,
          transform: `translateX(${(1 - progress) * -90}px)`,
        }}
      >
        <div style={{fontSize: 54, fontWeight: 900, lineHeight: 1}}>{name}</div>
        <div style={{fontSize: 28, marginTop: 10, opacity: 0.76}}>{role}</div>
      </div>
    </AbsoluteFill>
  );
};
```

## Use with footage

```tsx
import {Sequence, OffthreadVideo, staticFile} from 'remotion';

export const InterviewClip = () => (
  <>
    <OffthreadVideo src={staticFile('interview.mp4')} />
    <Sequence from={45} durationInFrames={150}>
      <LowerThird name="Sam Rivera" role="Principal Product Designer" />
    </Sequence>
  </>
);
```

## Render notes

- Keep lower thirds inside title-safe margins.
- Use `OffthreadVideo` for video footage so frame extraction is render-friendly.
- Use `Sequence` so the overlay can appear only during the intended time range.
- For transparent exports, render the lower third alone as WebM with alpha.

```bash
npx remotion render src/index.ts InterviewClip out/interview-lower-third.mp4
npx remotion render src/index.ts LowerThirdOnly out/lower-third.webm --codec=vp8 --pixel-format=yuva420p
```

## Variations

- Add a small logo inside the card.
- Add a second line for location or handle.
- Parameterize colors by brand.
- Export a transparent overlay for external editors.

# Product promo scenes

This example shows a simple multi-scene product promo. The goal is not cinematic complexity; it is a clean architecture for scene sequencing, product props, and repeatable render commands.

## Scene plan

| Frames | Scene | Purpose |
|--------|-------|---------|
| 0-59 | Hook | State the product promise |
| 60-139 | Feature cards | Show three benefits |
| 140-209 | Social proof | Display metric or quote |
| 210-269 | CTA | End with action and URL |

## Component sketch

```tsx
import {AbsoluteFill, Sequence, interpolate, useCurrentFrame} from 'remotion';

const Hook = ({headline}: {headline: string}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 18], [0, 1], {extrapolateRight: 'clamp'});
  return <h1 style={{fontSize: 118, opacity}}>{headline}</h1>;
};

const Features = ({items}: {items: string[]}) => (
  <div style={{display: 'flex', gap: 28}}>
    {items.map((item, index) => (
      <div key={item} style={{padding: 36, borderRadius: 28, background: 'rgba(255,255,255,0.1)'}}>
        <div style={{fontSize: 28, opacity: 0.6}}>0{index + 1}</div>
        <div style={{fontSize: 40, fontWeight: 800}}>{item}</div>
      </div>
    ))}
  </div>
);

export const ProductPromo = ({
  product = 'Atlas CRM',
  headline = 'Close the loop from lead to revenue',
  features = ['Pipeline clarity', 'AI summaries', 'Forecast confidence'],
  cta = 'Book a demo',
}) => (
  <AbsoluteFill style={{background: '#050816', color: 'white', fontFamily: 'Inter', padding: 96}}>
    <div style={{fontSize: 30, color: '#38bdf8', fontWeight: 800}}>{product}</div>
    <Sequence from={0} durationInFrames={60}>
      <Hook headline={headline} />
    </Sequence>
    <Sequence from={60} durationInFrames={80}>
      <Features items={features} />
    </Sequence>
    <Sequence from={140} durationInFrames={70}>
      <h2 style={{fontSize: 88}}>32% faster follow-up</h2>
    </Sequence>
    <Sequence from={210} durationInFrames={60}>
      <h2 style={{fontSize: 96}}>{cta}</h2>
      <p style={{fontSize: 38, opacity: 0.72}}>atlas.example/demo</p>
    </Sequence>
  </AbsoluteFill>
);
```

## Composition registration

```tsx
<Composition
  id="ProductPromo"
  component={ProductPromo}
  durationInFrames={270}
  fps={30}
  width={1920}
  height={1080}
/>
```

## Render commands

```bash
npx remotion still src/index.ts ProductPromo out/product-poster.png --frame=95
npx remotion render src/index.ts ProductPromo out/product-promo.mp4 --crf=18
```

## Production improvements

- Use `inputProps` from a campaign JSON file.
- Replace magic frame numbers with named constants.
- Add transition components between scenes.
- Render one short range per scene in CI.
- Add captions or voiceover timing if the promo has narration.

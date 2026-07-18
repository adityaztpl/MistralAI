# Zod schema and Remotion Studio

Attach a Zod schema to `<Composition>` to make default props editable in Remotion Studio and keep props type-safe.

Install:

```bash
npx remotion add @remotion/zod-types zod
```

## Define schema and infer props

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {z} from 'zod';
import {zColor, zTextarea} from '@remotion/zod-types';

export const promoSchema = z.object({
  headline: z.string().min(1),
  body: zTextarea(),
  backgroundColor: zColor(),
  accentColor: zColor(),
  showLogo: z.boolean(),
  plan: z.enum(['starter', 'pro', 'enterprise']),
});

export type PromoProps = z.infer<typeof promoSchema>;

export const Promo: React.FC<PromoProps> = ({
  headline,
  body,
  backgroundColor,
  accentColor,
  showLogo,
  plan,
}) => {
  return (
    <AbsoluteFill style={{backgroundColor, color: '#0f172a', padding: 90}}>
      {showLogo ? <div style={{fontSize: 32, color: accentColor}}>ACME</div> : null}
      <h1 style={{fontSize: 92, marginBottom: 32}}>{headline}</h1>
      <p style={{fontSize: 38, maxWidth: 800, whiteSpace: 'pre-wrap'}}>{body}</p>
      <div style={{fontSize: 30, color: accentColor}}>Plan: {plan}</div>
    </AbsoluteFill>
  );
};
```

## Register schema on `<Composition>`

```tsx
import React from 'react';
import {Composition} from 'remotion';
import {Promo, promoSchema} from './Promo';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="promo"
      component={Promo}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={150}
      schema={promoSchema}
      defaultProps={{
        headline: 'Launch faster',
        body: 'Create personalized campaign videos from data.',
        backgroundColor: '#dbeafe',
        accentColor: '#2563eb',
        showLogo: true,
        plan: 'pro',
      }}
    />
  );
};
```

Studio can show controls for common Zod types:

- `z.object()`
- `z.string()`, `z.number()`, `z.boolean()`, `z.date()`
- `z.array()`
- supported nullable/optional unions
- `z.enum()`
- `zColor()`, `zTextarea()`, `zMatrix()` from `@remotion/zod-types`
- `.min()`, `.max()`, `.step()`

## Numeric controls

```tsx
import {z} from 'zod';
import {zColor} from '@remotion/zod-types';

export const chartSchema = z.object({
  value: z.number().min(0).max(100).step(1),
  label: z.string(),
  color: zColor(),
});
```

## Static assets in schemas

Remotion Studio supports static assets by typing them as strings and using `staticFile()` in your code.

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {z} from 'zod';

export const testimonialSchema = z.object({
  quote: z.string(),
  author: z.string(),
  avatarPath: z.string(),
});

export const Testimonial: React.FC<z.infer<typeof testimonialSchema>> = ({
  quote,
  author,
  avatarPath,
}) => {
  return (
    <AbsoluteFill style={{padding: 90, backgroundColor: '#f8fafc'}}>
      <Img
        src={staticFile(avatarPath)}
        style={{width: 180, height: 180, borderRadius: 90, objectFit: 'cover'}}
      />
      <blockquote style={{fontSize: 58}}>"{quote}"</blockquote>
      <p style={{fontSize: 34}}>- {author}</p>
    </AbsoluteFill>
  );
};
```

## Arrays for repeated sections

```tsx
import {z} from 'zod';
import {zColor} from '@remotion/zod-types';

export const featureSchema = z.object({
  title: z.string(),
  bullets: z.array(z.string().min(1)).min(1).max(5),
  color: zColor(),
});
```

```tsx
export const FeatureList: React.FC<z.infer<typeof featureSchema>> = ({
  title,
  bullets,
  color,
}) => {
  return (
    <AbsoluteFill style={{backgroundColor: color, padding: 90}}>
      <h1>{title}</h1>
      {bullets.map((bullet) => (
        <p key={bullet} style={{fontSize: 42}}>- {bullet}</p>
      ))}
    </AbsoluteFill>
  );
};
```

## Studio workflow

1. Open Remotion Studio.
2. Select a composition.
3. Open the right sidebar (`Cmd/Ctrl + J`).
4. Edit default props in the Props tab.
5. Use the Render dialog to render with current values as input props.
6. Save default props back to source when your root file is statically analyzable and defaults are inline.

## Schema checklist

- [ ] Top-level schema is `z.object()`.
- [ ] `defaultProps` exactly match schema requirements.
- [ ] Use `z.infer<typeof schema>` for the React prop type.
- [ ] Use Remotion zod types for colors and textareas.
- [ ] Keep schemas user-friendly for Studio editors.
- [ ] Use validation ranges to prevent broken designs.

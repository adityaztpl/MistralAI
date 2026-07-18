# Templates for marketing videos

Marketing videos are a strong use case for Remotion because copy, products, colors, format, and scenes can all be driven by JSON. The same composition can generate social ads, launch announcements, webinar promos, and product cards.

## Template prop model

```tsx
import {z} from 'zod';
import {zColor} from '@remotion/zod-types';

export const marketingTemplateSchema = z.object({
  brand: z.object({
    name: z.string(),
    logoPath: z.string(),
    primaryColor: zColor(),
    backgroundColor: zColor(),
  }),
  format: z.enum(['square', 'portrait', 'landscape']),
  scenes: z.array(
    z.discriminatedUnion('type', [
      z.object({
        type: z.literal('headline'),
        title: z.string(),
        subtitle: z.string(),
        durationInFrames: z.number().int().positive(),
      }),
      z.object({
        type: z.literal('product'),
        name: z.string(),
        imagePath: z.string(),
        price: z.string(),
        durationInFrames: z.number().int().positive(),
      }),
      z.object({
        type: z.literal('cta'),
        text: z.string(),
        url: z.string(),
        durationInFrames: z.number().int().positive(),
      }),
    ]),
  ),
});

export type MarketingTemplateProps = z.infer<typeof marketingTemplateSchema>;
```

## Scene renderer

```tsx
import React from 'react';
import type {MarketingTemplateProps} from './schema';

type Scene = MarketingTemplateProps['scenes'][number];

export const MarketingScene: React.FC<{
  scene: Scene;
  brand: MarketingTemplateProps['brand'];
}> = ({scene, brand}) => {
  switch (scene.type) {
    case 'headline':
      return <HeadlineScene scene={scene} brand={brand} />;
    case 'product':
      return <ProductScene scene={scene} brand={brand} />;
    case 'cta':
      return <CtaScene scene={scene} brand={brand} />;
  }
};
```

## Template composition

```tsx
import React from 'react';
import {AbsoluteFill, Series} from 'remotion';
import type {MarketingTemplateProps} from './schema';
import {MarketingScene} from './MarketingScene';
import {Soundtrack} from './Soundtrack';

export const MarketingTemplate: React.FC<MarketingTemplateProps> = ({
  brand,
  scenes,
}) => {
  return (
    <AbsoluteFill style={{backgroundColor: brand.backgroundColor}}>
      <Series>
        {scenes.map((scene, index) => (
          <Series.Sequence
            key={`${scene.type}-${index}`}
            durationInFrames={scene.durationInFrames}
            name={`${index + 1}. ${scene.type}`}
          >
            <MarketingScene scene={scene} brand={brand} />
          </Series.Sequence>
        ))}
      </Series>
      <Soundtrack />
    </AbsoluteFill>
  );
};
```

## Dynamic metadata

```tsx
import type {CalculateMetadataFunction} from 'remotion';
import type {MarketingTemplateProps} from './schema';

export const calculateMarketingMetadata: CalculateMetadataFunction<
  MarketingTemplateProps
> = ({props}) => {
  const durationInFrames = props.scenes.reduce(
    (sum, scene) => sum + scene.durationInFrames,
    0,
  );

  const size =
    props.format === 'portrait'
      ? {width: 1080, height: 1920}
      : props.format === 'landscape'
        ? {width: 1920, height: 1080}
        : {width: 1080, height: 1080};

  return {
    ...size,
    durationInFrames,
    props,
    defaultOutName: `${props.brand.name.toLowerCase().replace(/\s+/g, '-')}-ad`,
  };
};
```

## Registration

```tsx
import React from 'react';
import {Composition} from 'remotion';
import {MarketingTemplate} from './MarketingTemplate';
import {
  calculateMarketingMetadata,
  marketingTemplateSchema,
} from './schema';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="marketing-template"
      component={MarketingTemplate}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={180}
      schema={marketingTemplateSchema}
      calculateMetadata={calculateMarketingMetadata}
      defaultProps={{
        brand: {
          name: 'Acme',
          logoPath: 'brand/logo.png',
          primaryColor: '#2563eb',
          backgroundColor: '#eff6ff',
        },
        format: 'square',
        scenes: [
          {
            type: 'headline',
            title: 'Launch faster',
            subtitle: 'Automated campaign videos',
            durationInFrames: 90,
          },
          {
            type: 'cta',
            text: 'Start today',
            url: 'acme.example',
            durationInFrames: 75,
          },
        ],
      }}
    />
  );
};
```

## Product scene example

```tsx
import React from 'react';
import {AbsoluteFill, Img, interpolate, staticFile, useCurrentFrame} from 'remotion';

type ProductSceneProps = {
  scene: {
    type: 'product';
    name: string;
    imagePath: string;
    price: string;
  };
  brand: {
    primaryColor: string;
  };
};

export const ProductScene: React.FC<ProductSceneProps> = ({scene, brand}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, 35], [0.92, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        padding: 90,
        justifyContent: 'center',
        alignItems: 'center',
      }}
    >
      <Img
        src={staticFile(scene.imagePath)}
        style={{
          width: 620,
          height: 620,
          objectFit: 'cover',
          borderRadius: 48,
          transform: `scale(${scale})`,
        }}
      />
      <h1 style={{fontSize: 68, color: brand.primaryColor}}>{scene.name}</h1>
      <p style={{fontSize: 44}}>{scene.price}</p>
    </AbsoluteFill>
  );
};
```

## Template checklist

- [ ] Schema covers brand, format, scenes, and asset references.
- [ ] Scene durations are data-driven.
- [ ] `calculateMetadata()` computes total duration and aspect ratio.
- [ ] Asset props are strings resolved with `staticFile()` or signed URLs.
- [ ] Render worker validates payloads before calling Remotion.
- [ ] Scenes are small components with local timing.
- [ ] Output names are deterministic and human-readable.

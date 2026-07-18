# Dynamic duration and metadata with `calculateMetadata()`

`calculateMetadata` is a prop on `<Composition>`. It can transform composition metadata before rendering:

- `durationInFrames`
- `width`
- `height`
- `fps`
- final `props`
- default codec and encoding defaults
- default output name
- default sample rate

It may be async and is called once as part of composition selection, independently from render concurrency.

## Dynamic duration from props

```tsx
import React from 'react';
import {CalculateMetadataFunction, Composition} from 'remotion';
import {z} from 'zod';
import {MarketingVideo} from './MarketingVideo';

export const marketingSchema = z.object({
  scenes: z.array(
    z.object({
      title: z.string(),
      durationInFrames: z.number().int().positive(),
    }),
  ),
});

type MarketingProps = z.infer<typeof marketingSchema>;

const calculateMetadata: CalculateMetadataFunction<MarketingProps> = ({props}) => {
  return {
    durationInFrames: props.scenes.reduce(
      (sum, scene) => sum + scene.durationInFrames,
      0,
    ),
    props,
  };
};

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="marketing"
      component={MarketingVideo}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={120}
      schema={marketingSchema}
      defaultProps={{
        scenes: [{title: 'Launch faster', durationInFrames: 120}],
      }}
      calculateMetadata={calculateMetadata}
    />
  );
};
```

The static `durationInFrames` is still required, but the returned value takes precedence.

## Transform and normalize props

```tsx
import type {CalculateMetadataFunction} from 'remotion';

type ProductVideoProps = {
  productName: string;
  priceCents: number;
  currency: 'USD' | 'EUR';
  formattedPrice?: string;
};

export const calculateProductMetadata: CalculateMetadataFunction<ProductVideoProps> = ({
  props,
}) => {
  const formatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: props.currency,
  });

  return {
    props: {
      ...props,
      productName: props.productName.trim(),
      formattedPrice: formatter.format(props.priceCents / 100),
    },
  };
};
```

The component receives the transformed props.

## Async metadata from an API

```tsx
import type {CalculateMetadataFunction} from 'remotion';

type VideoProps = {
  productId: string;
  title?: string;
  imageUrl?: string;
  durationInFrames?: number;
};

export const calculateMetadata: CalculateMetadataFunction<VideoProps> = async ({
  props,
  abortSignal,
}) => {
  const response = await fetch(`https://api.example.com/products/${props.productId}`, {
    signal: abortSignal,
  });

  if (!response.ok) {
    throw new Error(`Could not fetch product ${props.productId}`);
  }

  const product = (await response.json()) as {
    title: string;
    imageUrl: string;
    seconds: number;
  };

  return {
    durationInFrames: Math.round(product.seconds * 30),
    props: {
      ...props,
      title: product.title,
      imageUrl: product.imageUrl,
      durationInFrames: Math.round(product.seconds * 30),
    },
    defaultOutName: `product-${props.productId}`,
  };
};
```

Use `abortSignal` so in-progress requests can be cancelled when Studio props change.

## Dynamic aspect ratio

```tsx
import type {CalculateMetadataFunction} from 'remotion';

type AspectProps = {
  format: 'square' | 'portrait' | 'landscape';
};

export const calculateAspectMetadata: CalculateMetadataFunction<AspectProps> = ({
  props,
}) => {
  if (props.format === 'portrait') {
    return {width: 1080, height: 1920};
  }

  if (props.format === 'landscape') {
    return {width: 1920, height: 1080};
  }

  return {width: 1080, height: 1080};
};
```

This is useful when one template supports TikTok/Reels, square feeds, and landscape ads.

## Encoding defaults per composition

```tsx
import type {CalculateMetadataFunction} from 'remotion';

type RenderPresetProps = {
  transparent: boolean;
};

export const calculateRenderDefaults: CalculateMetadataFunction<RenderPresetProps> = ({
  props,
}) => {
  if (props.transparent) {
    return {
      defaultCodec: 'prores',
      defaultVideoImageFormat: 'png',
      defaultPixelFormat: 'yuva444p10le',
      defaultProResProfile: '4444',
    };
  }

  return {
    defaultCodec: 'h264',
    defaultVideoImageFormat: 'jpeg',
    defaultSampleRate: 48000,
  };
};
```

Returned defaults have lower priority than explicit render API options but higher priority than config-level defaults.

## Rules and caveats

- Return values must be JSON-serializable plain data.
- `props` must keep the same shape as the input prop type.
- The function must resolve within the timeout.
- It runs each time props change in Studio.
- Use it for metadata and prop preparation, not per-frame rendering work.
- Heavy content should be cached outside the render process if possible.

## Checklist

- [ ] Derive duration from scene data in one place.
- [ ] Normalize optional props before the component receives them.
- [ ] Use `abortSignal` for fetches.
- [ ] Keep returned props serializable.
- [ ] Do not fetch per frame when one metadata fetch would work.
- [ ] Pass input props to `selectComposition()` so metadata sees the right data.

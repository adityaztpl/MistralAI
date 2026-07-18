# `<Img>`, `staticFile()`, remote URLs, and `delayRender()`

Remotion's `<Img>` behaves like a regular HTML `<img>` tag with one important video-rendering guarantee: Remotion waits for the image to load before rendering the current frame. That avoids flicker and missing images in exported frames.

Use it for still images, logos, photos, sprites, backgrounds, and generated images. Do **not** use it for animated GIFs; use `@remotion/gif` instead.

## Basic local image

Put files in `public/`, then reference them with `staticFile()`.

```text
my-video/
|-- public/
|   |-- brand/logo.png
|   `-- photos/product.png
|-- src/
|   `-- MyComposition.tsx
`-- package.json
```

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';

export const ProductCard: React.FC = () => {
  return (
    <AbsoluteFill
      style={{
        backgroundColor: '#f8fafc',
        justifyContent: 'center',
        alignItems: 'center',
      }}
    >
      <Img
        src={staticFile('photos/product.png')}
        style={{
          width: 720,
          borderRadius: 40,
          boxShadow: '0 30px 80px rgba(15,23,42,0.2)',
        }}
      />
    </AbsoluteFill>
  );
};
```

Why `staticFile()` instead of `"/photos/product.png"`?

- It works when the app is served from a subdirectory.
- It avoids collisions with composition routes in Studio.
- It keeps code portable across Remotion, Vite, React Router, Next.js, Studio, and rendering.
- Since Remotion 4, filenames with unsafe URI characters are encoded by `staticFile()`.

## Remote images

`<Img>` can render remote URLs directly.

```tsx
import React from 'react';
import {AbsoluteFill, Img, interpolate, useCurrentFrame} from 'remotion';

export const RemotePoster: React.FC = () => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, 45], [1.08, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{overflow: 'hidden', backgroundColor: 'black'}}>
      <Img
        src="https://images.unsplash.com/photo-1498050108023-c5249f4df085"
        style={{
          width: '100%',
          height: '100%',
          objectFit: 'cover',
          transform: `scale(${scale})`,
        }}
      />
    </AbsoluteFill>
  );
};
```

Remote image caveats:

- The URL must be reachable during rendering.
- If you use image effects that draw to canvas, CORS restrictions may apply.
- For branded or repeatable renders, prefer downloading assets into `public/` or serving from a controlled CDN.
- For slow URLs, consider raising `delayRenderTimeoutInMilliseconds` on `<Img>`.

## Handling failures

If image loading fails after retries, Remotion cancels the render unless you handle the error and replace or unmount the image. Returning a broken `<Img>` forever causes a timeout.

```tsx
import React, {useState} from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';

export const ResilientLogo: React.FC = () => {
  const [src, setSrc] = useState(() => staticFile('brand/logo.png'));

  return (
    <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
      <Img
        src={src}
        maxRetries={4}
        delayRenderTimeoutInMilliseconds={60000}
        onError={() => {
          setSrc(staticFile('brand/logo-fallback.png'));
        }}
        style={{width: 320}}
      />
    </AbsoluteFill>
  );
};
```

Key props from the Remotion docs:

- `maxRetries`: retries image loading with exponential backoff.
- `delayRenderTimeoutInMilliseconds`: customizes the internal `delayRender()` timeout.
- `delayRenderRetries`: customizes internal render-delay retries; usually prefer `maxRetries`.
- `pauseWhenLoading`: pauses the Player while an image is loading.
- `from`, `durationInFrames`, `trimBefore`, `name`, `showInTimeline`, `hidden`: inherited timing/layer props.

## Timing an image directly

In Remotion 4, `<Img>` can inherit some `<Sequence>`-like props.

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';

export const TimedLowerThird: React.FC = () => {
  return (
    <AbsoluteFill>
      <Img
        from={30}
        durationInFrames={90}
        name="Sponsor logo"
        src={staticFile('sponsors/acme.png')}
        style={{
          position: 'absolute',
          right: 72,
          bottom: 72,
          width: 220,
        }}
      />
    </AbsoluteFill>
  );
};
```

Wrapping in `<Sequence>` is still useful when you want a named group, multiple elements, premounting, or nested timing.

## Manual `delayRender()` for image metadata

`<Img>` waits for the pixels to load, but sometimes your component needs extra image metadata before layout. For example, you may want to center-crop based on natural dimensions.

```tsx
import React, {useEffect, useMemo, useState} from 'react';
import {
  AbsoluteFill,
  cancelRender,
  continueRender,
  delayRender,
  Img,
  staticFile,
} from 'remotion';

type Size = {
  width: number;
  height: number;
};

const loadImageSize = (src: string): Promise<Size> => {
  return new Promise((resolve, reject) => {
    const image = new Image();
    image.onload = () =>
      resolve({width: image.naturalWidth, height: image.naturalHeight});
    image.onerror = reject;
    image.src = src;
  });
};

export const ImageWithMeasuredLayout: React.FC = () => {
  const src = staticFile('photos/campaign-hero.jpg');
  const [size, setSize] = useState<Size | null>(null);
  const handle = useMemo(() => delayRender('Load image dimensions'), []);

  useEffect(() => {
    loadImageSize(src)
      .then((nextSize) => {
        setSize(nextSize);
        continueRender(handle);
      })
      .catch((error) => {
        cancelRender(error);
      });

    return () => {
      continueRender(handle);
    };
  }, [handle, src]);

  if (!size) {
    return null;
  }

  const isPortrait = size.height > size.width;

  return (
    <AbsoluteFill style={{backgroundColor: '#020617'}}>
      <Img
        src={src}
        style={{
          width: '100%',
          height: '100%',
          objectFit: isPortrait ? 'contain' : 'cover',
        }}
      />
    </AbsoluteFill>
  );
};
```

Best practices for manual render delays:

- Create the handle once with `useMemo()` or lazy state.
- Always call `continueRender()` in cleanup so unmounts do not leave the render blocked.
- Call `cancelRender(error)` when the asset is required and cannot load.
- Keep the async work deterministic; renders may run with concurrency.

## Effects note

`<Img effects={[...]}>` renders through a canvas instead of a native `<img>`. That enables Remotion effects but changes which native props are available. Canvas-compatible props such as `aria-*`, `data-*`, `role`, pointer handlers, and `style.objectFit` continue to matter; image-only props like `srcSet`, `sizes`, `loading`, `decoding`, `alt`, and native image refs are not supported with non-empty effects.

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {blur} from '@remotion/effects/blur';

export const BlurredBackdrop: React.FC = () => {
  return (
    <AbsoluteFill>
      <Img
        src={staticFile('photos/background.jpg')}
        effects={[blur({radius: 16})]}
        style={{width: '100%', height: '100%', objectFit: 'cover'}}
      />
    </AbsoluteFill>
  );
};
```

## Interview-style summary

Say: "`<Img>` is Remotion's render-aware image component. It delays the frame until the image is loaded, retries failures, and can be timed like a sequence. I use `staticFile()` for local assets because it produces a framework-safe URL from `public/`; remote URLs work but should be controlled for reliability and CORS. If I need my own async metadata, I wrap it in `delayRender()` and always resolve or cancel the render."

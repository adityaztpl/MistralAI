# GIF, Lottie, and Rive in Remotion

Animated assets need timeline-aware components. A regular `<img>` or CSS animation can drift from Remotion's frame clock; Remotion packages expose components that synchronize animation state with `useCurrentFrame()`.

Use:

- `<Gif>` from `@remotion/gif` for GIF files.
- `<Lottie>` from `@remotion/lottie` for Lottie JSON.
- `<RemotionRiveCanvas>` from `@remotion/rive` for `.riv` files.

## GIF with `@remotion/gif`

Install:

```bash
npx remotion add @remotion/gif
```

Basic usage:

```tsx
import React from 'react';
import {AbsoluteFill, staticFile, useVideoConfig} from 'remotion';
import {Gif} from '@remotion/gif';

export const GifSticker: React.FC = () => {
  const {width, height} = useVideoConfig();

  return (
    <AbsoluteFill style={{backgroundColor: '#111827'}}>
      <Gif
        src={staticFile('stickers/celebrate.gif')}
        width={width}
        height={height}
        fit="contain"
        playbackRate={1}
      />
    </AbsoluteFill>
  );
};
```

Important `<Gif>` props:

- `src`: URL or local asset.
- `width`, `height`: required display size.
- `fit`: `'fill'`, `'contain'`, or `'cover'`.
- `playbackRate`: speed multiplier.
- `loopBehavior`: `'loop'`, `'pause-after-finish'`, or `'unmount-after-finish'`.
- `onLoad`: receives GIF dimensions, frame delays, and decoded frames.
- `delayRenderTimeoutInMilliseconds`: useful for large GIFs.
- `requestInit`: pass fetch credentials/headers for authenticated GIFs.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Gif} from '@remotion/gif';

export const OneShotGif: React.FC = () => {
  return (
    <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
      <Gif
        src={staticFile('stickers/checkmark.gif')}
        width={420}
        height={420}
        fit="contain"
        loopBehavior="pause-after-finish"
        playbackRate={1.25}
        onLoad={({width, height, frames}) => {
          console.log(`Loaded ${frames.length} GIF frames (${width}x${height})`);
        }}
      />
    </AbsoluteFill>
  );
};
```

Remote GIFs need CORS support because the component fetches and decodes them.

## Lottie with `@remotion/lottie`

Install:

```bash
npx remotion add @remotion/lottie lottie-web
```

Lottie is useful for vector-style micro animations exported from tools like After Effects. Remotion drives Lottie by seeking to the correct frame.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {Lottie} from '@remotion/lottie';

export const LottieBadge: React.FC = () => {
  return (
    <AbsoluteFill
      style={{
        backgroundColor: '#fefce8',
        justifyContent: 'center',
        alignItems: 'center',
      }}
    >
      <Lottie
        animationData={staticFile('lottie/success.json')}
        style={{width: 520, height: 520}}
      />
    </AbsoluteFill>
  );
};
```

Many teams prefer importing JSON when bundled:

```tsx
import React from 'react';
import {AbsoluteFill} from 'remotion';
import {Lottie} from '@remotion/lottie';
import animationData from '../assets/success.json';

export const BundledLottie: React.FC = () => {
  return (
    <AbsoluteFill style={{justifyContent: 'center', alignItems: 'center'}}>
      <Lottie animationData={animationData} style={{width: 400}} />
    </AbsoluteFill>
  );
};
```

Lottie caveats from the Remotion docs:

- Remotion uses `lottie-web` and seeks with `goToAndStop()`.
- Some Lottie expressions may not be deterministic frame-by-frame and can flicker.
- Evaluate complex expression-heavy animations before committing to them.
- Remote files are supported, but local files are more reliable for repeatable renders.

## Rive with `@remotion/rive`

Install:

```bash
npx remotion add @remotion/rive
```

Use `<RemotionRiveCanvas>` to render `.riv` animations synchronized with Remotion time.

```tsx
import React from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {RemotionRiveCanvas} from '@remotion/rive';

export const RiveHero: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#eff6ff'}}>
      <RemotionRiveCanvas
        src={staticFile('rive/vehicle.riv')}
        fit="cover"
        alignment="center"
        style={{width: '100%', height: '100%'}}
      />
    </AbsoluteFill>
  );
};
```

Useful Rive props:

- `src`: local `staticFile()` URL or remote `.riv` URL.
- `fit`: `'contain'`, `'cover'`, `'fill'`, `'fit-height'`, `'fit-width'`, `'none'`, or `'scale-down'`.
- `alignment`: center/corner/edge alignment value.
- `artboard`: artboard name or index.
- `animation`: animation name or index.
- `onLoad`: access the Rive file.
- `assetLoader`: custom loading for referenced assets; memoize with `useCallback()`.
- Inherited props: `from`, `durationInFrames`, `trimBefore`, `name`, `showInTimeline`, `hidden`.

## Rive text run example

If your Rive file contains a text run named `city`, you can set it when the file loads.

```tsx
import React, {useCallback} from 'react';
import {AbsoluteFill, staticFile} from 'remotion';
import {RemotionRiveCanvas} from '@remotion/rive';
import type {File} from '@rive-app/canvas-advanced';

export const PersonalizedRive: React.FC<{city: string}> = ({city}) => {
  const onLoad = useCallback(
    (file: File) => {
      const artboard = file.defaultArtboard();
      const run = artboard.textRun('city');
      run.text = city;
    },
    [city],
  );

  return (
    <AbsoluteFill>
      <RemotionRiveCanvas
        src={staticFile('rive/city-card.riv')}
        onLoad={onLoad}
        fit="contain"
        style={{width: '100%', height: '100%'}}
      />
    </AbsoluteFill>
  );
};
```

## Ref example for Rive internals

```tsx
import React, {useEffect, useRef} from 'react';
import {RemotionRiveCanvas, type RiveCanvasRef} from '@remotion/rive';

export const RiveDebug: React.FC = () => {
  const ref = useRef<RiveCanvasRef>(null);

  useEffect(() => {
    if (!ref.current) {
      return;
    }

    console.log(ref.current.getArtboard());
    console.log(ref.current.getAnimationInstance());
    console.log(ref.current.getRenderer());
    console.log(ref.current.getCanvas());
  }, []);

  return (
    <RemotionRiveCanvas
      ref={ref}
      src="https://cdn.rive.app/animations/vehicles.riv"
      style={{width: '100%', height: '100%'}}
    />
  );
};
```

## Composition pattern: mixed animated overlays

```tsx
import React from 'react';
import {AbsoluteFill, Sequence, staticFile} from 'remotion';
import {Gif} from '@remotion/gif';
import {Lottie} from '@remotion/lottie';
import {RemotionRiveCanvas} from '@remotion/rive';

export const AnimatedAssetStack: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#020617'}}>
      <Sequence durationInFrames={90}>
        <Lottie
          animationData={staticFile('lottie/opening-burst.json')}
          style={{width: '100%', height: '100%'}}
        />
      </Sequence>

      <Sequence from={60} durationInFrames={120}>
        <RemotionRiveCanvas
          src={staticFile('rive/product-card.riv')}
          fit="contain"
          style={{width: '100%', height: '100%'}}
        />
      </Sequence>

      <Sequence from={150} durationInFrames={60}>
        <Gif
          src={staticFile('stickers/sparkle.gif')}
          width={300}
          height={300}
          fit="contain"
          loopBehavior="pause-after-finish"
          style={{position: 'absolute', right: 80, top: 80}}
        />
      </Sequence>
    </AbsoluteFill>
  );
};
```

## Decision notes

- Use GIFs for existing sticker assets, but prefer Lottie/Rive for scalable vector animation when possible.
- Use Lottie for After Effects style vector exports and simple expressions.
- Use Rive for interactive/state-machine-authored vector assets that need artboards, text runs, or asset loading.
- Put large JSON or `.riv` files in `public/` if you do not need them in the JS bundle.
- Keep animated assets deterministic by driving them from Remotion time, not `setInterval()` or wall-clock time.

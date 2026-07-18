# Fonts and asset loading

Fonts are media too. If a font loads after a frame is rendered, text can shift, reflow, or render with a fallback face in the final video. In Remotion, treat fonts like required render dependencies: reference them with `staticFile()`, load them explicitly, and block rendering with `delayRender()` until they are ready.

## Folder layout

```text
public/
|-- fonts/
|   |-- Inter-Regular.woff2
|   `-- Inter-Bold.woff2
|-- images/
|   `-- paper-texture.png
`-- data/
   `-- captions.json
```

`staticFile()` resolves files relative to the `public/` folder next to the `package.json` that contains `remotion`.

## Load a font with `FontFace`

```tsx
import React, {useEffect, useMemo, useState} from 'react';
import {
  AbsoluteFill,
  cancelRender,
  continueRender,
  delayRender,
  staticFile,
} from 'remotion';

const fontFamily = 'InterLocal';

export const FontLoadedTitle: React.FC = () => {
  const [ready, setReady] = useState(false);
  const handle = useMemo(() => delayRender('Load Inter font'), []);

  useEffect(() => {
    const regular = new FontFace(
      fontFamily,
      `url(${staticFile('fonts/Inter-Regular.woff2')}) format('woff2')`,
      {weight: '400'},
    );
    const bold = new FontFace(
      fontFamily,
      `url(${staticFile('fonts/Inter-Bold.woff2')}) format('woff2')`,
      {weight: '700'},
    );

    Promise.all([regular.load(), bold.load()])
      .then((faces) => {
        for (const face of faces) {
          document.fonts.add(face);
        }
        setReady(true);
        continueRender(handle);
      })
      .catch((error) => cancelRender(error));

    return () => {
      continueRender(handle);
    };
  }, [handle]);

  if (!ready) {
    return null;
  }

  return (
    <AbsoluteFill
      style={{
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#0f172a',
        color: 'white',
        fontFamily,
      }}
    >
      <h1 style={{fontSize: 110, lineHeight: 1, margin: 0, fontWeight: 700}}>
        Fonts are render dependencies
      </h1>
      <p style={{fontSize: 34, opacity: 0.75}}>
        Load them before frames are captured.
      </p>
    </AbsoluteFill>
  );
};
```

## Reusable hook for multiple compositions

```tsx
import {useEffect, useMemo, useState} from 'react';
import {cancelRender, continueRender, delayRender, staticFile} from 'remotion';

type FontSpec = {
  family: string;
  file: string;
  weight?: string;
  style?: string;
};

export const useStaticFonts = (fonts: FontSpec[]) => {
  const [loaded, setLoaded] = useState(false);
  const handle = useMemo(() => delayRender('Load static fonts'), []);

  useEffect(() => {
    Promise.all(
      fonts.map((font) => {
        const face = new FontFace(
          font.family,
          `url(${staticFile(font.file)}) format('woff2')`,
          {
            weight: font.weight,
            style: font.style,
          },
        );

        return face.load().then((loadedFace) => {
          document.fonts.add(loadedFace);
          return loadedFace;
        });
      }),
    )
      .then(() => {
        setLoaded(true);
        continueRender(handle);
      })
      .catch((error) => cancelRender(error));

    return () => continueRender(handle);
  }, [fonts, handle]);

  return loaded;
};
```

Use it with memoized font descriptors:

```tsx
import React, {useMemo} from 'react';
import {AbsoluteFill} from 'remotion';
import {useStaticFonts} from './use-static-fonts';

export const BrandedScene: React.FC = () => {
  const fonts = useMemo(
    () => [
      {family: 'BrandSans', file: 'fonts/BrandSans-Regular.woff2', weight: '400'},
      {family: 'BrandSans', file: 'fonts/BrandSans-Bold.woff2', weight: '700'},
    ],
    [],
  );
  const loaded = useStaticFonts(fonts);

  if (!loaded) {
    return null;
  }

  return (
    <AbsoluteFill style={{fontFamily: 'BrandSans', padding: 96}}>
      <h1 style={{fontSize: 92}}>Q4 launch results</h1>
    </AbsoluteFill>
  );
};
```

## Loading JSON assets

For small JSON, importing can be fine. For content that changes often or should stay outside the JS bundle, put it in `public/` and fetch it with a render delay.

```tsx
import React, {useEffect, useMemo, useState} from 'react';
import {
  AbsoluteFill,
  cancelRender,
  continueRender,
  delayRender,
  staticFile,
} from 'remotion';

type Caption = {
  startFrame: number;
  text: string;
};

export const CaptionPreview: React.FC = () => {
  const [captions, setCaptions] = useState<Caption[] | null>(null);
  const handle = useMemo(() => delayRender('Load captions'), []);

  useEffect(() => {
    fetch(staticFile('data/captions.json'))
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Could not load captions: ${response.status}`);
        }
        return response.json() as Promise<Caption[]>;
      })
      .then((data) => {
        setCaptions(data);
        continueRender(handle);
      })
      .catch((error) => cancelRender(error));

    return () => continueRender(handle);
  }, [handle]);

  if (!captions) {
    return null;
  }

  return (
    <AbsoluteFill style={{padding: 80, backgroundColor: '#f8fafc'}}>
      {captions.slice(0, 5).map((caption) => (
        <div key={caption.startFrame} style={{fontSize: 34, marginBottom: 20}}>
          {caption.startFrame}: {caption.text}
        </div>
      ))}
    </AbsoluteFill>
  );
};
```

## Preloading in the Player

When embedding a video in a React app with `@remotion/player`, preload assets to avoid the first play stalling.

```tsx
import {preloadAudio, preloadFont, preloadImage, preloadVideo} from '@remotion/preload';

const cleanupImage = preloadImage('/static/logo.png');
const cleanupVideo = preloadVideo('https://example.com/intro.mp4');
const cleanupAudio = preloadAudio('https://example.com/music.mp3');
const cleanupFont = preloadFont('/static/fonts/Inter-Regular.woff2');

// Later, for cleanup:
cleanupImage();
cleanupVideo();
cleanupAudio();
cleanupFont();
```

`@remotion/preload` inserts `<link rel="preload">` hints. It is lighter than `prefetch()` because the browser can start loading without forcing the full asset to become a Blob URL.

## When to use `prefetch()`

`prefetch()` from `remotion` downloads the entire asset and maps future media usage to a Blob URL. It is more reliable but heavier.

```tsx
import {prefetch} from 'remotion';

const {free, waitUntilDone} = prefetch('https://example.com/short-intro.mp4');

waitUntilDone().then(() => {
  console.log('Short intro is fully downloaded');
});

// Free the Blob URL when the asset no longer needs to be cached.
free();
```

Use `prefetch()` for small, high-priority assets where you need a readiness promise. Use preload hints for large media where a best-effort browser signal is sufficient.

## Asset hygiene checklist

- [ ] Store stable assets under `public/`.
- [ ] Reference public files with `staticFile()` from composition code.
- [ ] Load fonts before rendering text-heavy frames.
- [ ] Keep font family names explicit and local to your project.
- [ ] Avoid gigantic `defaultProps` payloads; fetch large data or use `calculateMetadata()` instead.
- [ ] Prefer `.woff2` fonts for size and browser support.
- [ ] Keep source asset dimensions close to output requirements to avoid decoding huge files unnecessarily.

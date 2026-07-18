# `staticFile()` and the `public/` folder

`staticFile()` turns a file in `public/` into a URL that Remotion can use in Studio, renders, Player embeds, and framework integrations. It is the default way to reference images, video, audio, fonts, Lottie JSON, Rive files, and any fetchable project asset.

```tsx
import {Img, staticFile} from 'remotion';

export const Logo: React.FC = () => {
  return <Img src={staticFile('brand/logo.png')} />;
};
```

## The `public/` folder belongs next to `package.json`

```text
my-remotion-project/
|-- package.json
|-- public/
|   |-- brand/logo.png
|   |-- clips/hero.mp4
|   |-- fonts/Inter.woff2
|   `-- data/products.json
`-- src/
   |-- Root.tsx
   `-- index.ts
```

Even if your Remotion code lives in a subdirectory, the `public/` folder should be in the same folder as the `package.json` that contains the `remotion` dependency.

## Use `staticFile()` everywhere a URL is accepted

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {Audio, Video} from '@remotion/media';
import {Gif} from '@remotion/gif';
import {RemotionRiveCanvas} from '@remotion/rive';

export const StaticFileSampler: React.FC = () => {
  return (
    <AbsoluteFill>
      <Video src={staticFile('clips/background.mp4')} muted />
      <Audio src={staticFile('audio/music.mp3')} volume={0.4} />
      <Img src={staticFile('brand/logo.png')} style={{width: 220}} />
      <Gif
        src={staticFile('stickers/confetti.gif')}
        width={240}
        height={240}
        fit="contain"
      />
      <RemotionRiveCanvas
        src={staticFile('rive/button.riv')}
        style={{width: 400, height: 400}}
      />
    </AbsoluteFill>
  );
};
```

Other places where the URL is useful:

- `fetch(staticFile('data/products.json'))`
- `new FontFace('Brand', \`url(${staticFile('fonts/Brand.woff2')})\`)`
- Custom loaders for Lottie, Rive, captions, subtitles, or manifests.

## Do not hand-code `/public` paths

Avoid:

```tsx
<img src="/brand/logo.png" />
```

Prefer:

```tsx
<Img src={staticFile('brand/logo.png')} />
```

This prevents broken paths when:

- Studio has composition routes.
- The app is served from a subdirectory.
- The same composition is used in Vite, Next.js, React Router, Remotion Studio, and rendering.
- Filenames include URI-unsafe characters.

## URI-unsafe characters

Remotion 4 encodes filenames passed to `staticFile()`.

```tsx
import {staticFile} from 'remotion';

const image = staticFile('campaign/hero#summer?.png');
```

Do not pre-encode the path yourself; double encoding can break URLs.

## Fetching static data

```tsx
import React, {useEffect, useMemo, useState} from 'react';
import {
  AbsoluteFill,
  cancelRender,
  continueRender,
  delayRender,
  staticFile,
} from 'remotion';

type Product = {
  id: string;
  name: string;
  price: string;
};

export const ProductGridFromPublicJson: React.FC = () => {
  const [products, setProducts] = useState<Product[] | null>(null);
  const handle = useMemo(() => delayRender('Load product JSON'), []);

  useEffect(() => {
    fetch(staticFile('data/products.json'))
      .then((response) => {
        if (!response.ok) {
          throw new Error(`products.json failed: ${response.status}`);
        }
        return response.json() as Promise<Product[]>;
      })
      .then((data) => {
        setProducts(data);
        continueRender(handle);
      })
      .catch((error) => cancelRender(error));

    return () => continueRender(handle);
  }, [handle]);

  if (!products) {
    return null;
  }

  return (
    <AbsoluteFill style={{padding: 80, backgroundColor: '#fff'}}>
      {products.map((product) => (
        <div key={product.id} style={{fontSize: 42, marginBottom: 24}}>
          {product.name} - {product.price}
        </div>
      ))}
    </AbsoluteFill>
  );
};
```

If this data changes per render, prefer input props or `calculateMetadata()` rather than overwriting a file in `public/`.

## Listing files with `getStaticFiles()`

Remotion exposes `getStaticFiles()` to list files from `public/` in Studio and rendering contexts. It returns an empty array elsewhere.

```tsx
import React from 'react';
import {AbsoluteFill, getStaticFiles, Img, staticFile} from 'remotion';

export const FirstGalleryImage: React.FC = () => {
  const image = getStaticFiles().find((file) =>
    file.name.startsWith('gallery/'),
  );

  return (
    <AbsoluteFill style={{backgroundColor: '#0f172a'}}>
      {image ? (
        <Img
          src={staticFile(image.name)}
          style={{width: '100%', height: '100%', objectFit: 'cover'}}
        />
      ) : (
        <div style={{color: 'white', fontSize: 48}}>No gallery images</div>
      )}
    </AbsoluteFill>
  );
};
```

This is helpful for templates where users drop images into a folder and the composition discovers them.

## `staticFile()` with schemas and props

You can use string props to pick static assets.

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {z} from 'zod';

export const cardSchema = z.object({
  imagePath: z.string(),
  title: z.string(),
});

export const StaticAssetCard: React.FC<z.infer<typeof cardSchema>> = ({
  imagePath,
  title,
}) => {
  return (
    <AbsoluteFill style={{backgroundColor: '#f8fafc'}}>
      <Img
        src={staticFile(imagePath)}
        style={{width: '100%', height: '75%', objectFit: 'cover'}}
      />
      <h1 style={{fontSize: 72, padding: 64}}>{title}</h1>
    </AbsoluteFill>
  );
};
```

Register with defaults:

```tsx
import React from 'react';
import {Composition} from 'remotion';
import {StaticAssetCard, cardSchema} from './StaticAssetCard';

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="asset-card"
      component={StaticAssetCard}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={120}
      schema={cardSchema}
      defaultProps={{
        imagePath: 'gallery/default.jpg',
        title: 'New arrival',
      }}
    />
  );
};
```

## Rules of thumb

- `public/` is for files needed at runtime by URL.
- `src/` imports are for code and small bundled assets.
- Use `staticFile()` for local runtime URLs.
- Use remote URLs when assets are externally hosted and stable.
- Use input props to choose assets per render.
- Use `calculateMetadata()` when media metadata affects duration, dimensions, or normalized props.

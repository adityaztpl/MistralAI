# Embedding in Next.js and React apps

`@remotion/player` works in regular React apps. The main concerns are bundling, client-only rendering, lazy loading, props stability, and asset URLs.

## Vite or Create React App

```tsx
import React, {useMemo} from 'react';
import {Player} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

export const PromoEmbed: React.FC = () => {
  const inputProps = useMemo(
    () => ({
      title: 'Launch faster',
      subtitle: 'Automated video previews',
      backgroundColor: '#dbeafe',
    }),
    [],
  );

  return (
    <Player
      component={PromoVideo}
      inputProps={inputProps}
      durationInFrames={150}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      style={{width: '100%'}}
    />
  );
};
```

## Lazy component

Use `lazyComponent` for large video components. Wrap it in `useCallback()` so the Player does not treat it as a new component every render.

```tsx
import React, {useCallback} from 'react';
import {Player} from '@remotion/player';

export const LazyPromoEmbed: React.FC = () => {
  const lazyComponent = useCallback(() => import('./remotion/PromoVideo'), []);

  return (
    <Player
      lazyComponent={lazyComponent}
      durationInFrames={150}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      renderLoading={() => <div>Loading preview...</div>}
    />
  );
};
```

For `lazyComponent`, the imported file should have a default export for the video component.

## Next.js App Router

The Player is a client component.

```tsx
'use client';

import React, {useMemo} from 'react';
import {Player} from '@remotion/player';
import {PromoVideo} from '../remotion/PromoVideo';

export const PromoPlayerClient: React.FC<{
  title: string;
  subtitle: string;
}> = ({title, subtitle}) => {
  const inputProps = useMemo(
    () => ({
      title,
      subtitle,
      backgroundColor: '#f8fafc',
    }),
    [title, subtitle],
  );

  return (
    <Player
      component={PromoVideo}
      inputProps={inputProps}
      durationInFrames={150}
      compositionWidth={1080}
      compositionHeight={1080}
      fps={30}
      controls
      style={{width: '100%', maxWidth: 520}}
    />
  );
};
```

Use it from a server component:

```tsx
import {PromoPlayerClient} from './PromoPlayerClient';

export default async function Page() {
  const campaign = await getCampaign();

  return (
    <main>
      <h1>Preview</h1>
      <PromoPlayerClient
        title={campaign.title}
        subtitle={campaign.subtitle}
      />
    </main>
  );
}
```

## Dynamic import with SSR disabled

If your Next.js setup hits server/client bundling issues, dynamically import the client player.

```tsx
import dynamic from 'next/dynamic';

export const PromoPlayerNoSsr = dynamic(
  () => import('./PromoPlayerClient').then((mod) => mod.PromoPlayerClient),
  {ssr: false},
);
```

## Asset paths in embedded apps

Inside Remotion components, `staticFile()` is still useful for assets in the Remotion project's `public/` folder. If the host app and Remotion project have different public roots, prefer explicit URLs passed through props.

```tsx
<Player
  component={PromoVideo}
  inputProps={{
    title: 'CDN asset demo',
    heroImageUrl: 'https://cdn.example.com/campaigns/hero.jpg',
  }}
  durationInFrames={150}
  compositionWidth={1080}
  compositionHeight={1080}
  fps={30}
/>
```

Then render:

```tsx
import React from 'react';
import {AbsoluteFill, Img} from 'remotion';

export const PromoVideo: React.FC<{title: string; heroImageUrl: string}> = ({
  title,
  heroImageUrl,
}) => {
  return (
    <AbsoluteFill>
      <Img src={heroImageUrl} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
      <h1>{title}</h1>
    </AbsoluteFill>
  );
};
```

## Editor-style embed

```tsx
import React, {useMemo, useState} from 'react';
import {Player} from '@remotion/player';
import {PromoVideo} from './remotion/PromoVideo';

export const LiveEditor: React.FC = () => {
  const [title, setTitle] = useState('Launch faster');
  const [subtitle, setSubtitle] = useState('Edit props live');

  const inputProps = useMemo(
    () => ({title, subtitle, backgroundColor: '#dbeafe'}),
    [title, subtitle],
  );

  return (
    <div style={{display: 'grid', gridTemplateColumns: '320px 1fr', gap: 24}}>
      <form>
        <input value={title} onChange={(event) => setTitle(event.target.value)} />
        <textarea
          value={subtitle}
          onChange={(event) => setSubtitle(event.target.value)}
        />
      </form>
      <Player
        component={PromoVideo}
        inputProps={inputProps}
        durationInFrames={150}
        compositionWidth={1080}
        compositionHeight={1080}
        fps={30}
        controls
      />
    </div>
  );
};
```

## Checklist

- [ ] Mark Next.js Player wrappers with `'use client'`.
- [ ] Use `dynamic(..., {ssr: false})` if server rendering cannot bundle the Player path.
- [ ] Memoize `lazyComponent` with `useCallback()`.
- [ ] Memoize `inputProps` when derived from state.
- [ ] Keep asset URL strategy explicit between Remotion and host app public folders.
- [ ] Add error/loading UI for production embeds.

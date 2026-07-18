# API-fetched content

Remotion can fetch content from APIs, but you need deterministic loading. A render should not capture a half-loaded state, and a repeated render should not accidentally produce different output unless input data changed intentionally.

Use one of three patterns:

1. Fetch before rendering and pass the result as `inputProps`.
2. Fetch in `calculateMetadata()` and transform props.
3. Fetch inside a component with `delayRender()` when the data is visual-only and does not affect metadata.

## Preferred: fetch before render

```ts
import {renderMedia, selectComposition} from '@remotion/renderer';

type CampaignProps = {
  headline: string;
  products: Array<{name: string; imageUrl: string; price: string}>;
};

const fetchCampaign = async (campaignId: string): Promise<CampaignProps> => {
  const response = await fetch(`https://api.example.com/campaigns/${campaignId}`);
  if (!response.ok) {
    throw new Error(`Campaign fetch failed: ${response.status}`);
  }
  return response.json() as Promise<CampaignProps>;
};

const inputProps = await fetchCampaign('summer-2026');

const composition = await selectComposition({
  serveUrl,
  id: 'campaign',
  inputProps,
});

await renderMedia({
  composition,
  serveUrl,
  codec: 'h264',
  outputLocation: 'out/campaign.mp4',
  inputProps,
});
```

Benefits:

- Easier to test.
- Easier to cache.
- Renderer gets plain JSON.
- Failures happen before expensive rendering starts.

## Fetch in `calculateMetadata()`

Use this when fetched data changes duration, dimensions, or normalized props.

```tsx
import type {CalculateMetadataFunction} from 'remotion';

type CampaignInput = {
  campaignId: string;
  headline?: string;
  sceneCount?: number;
};

export const calculateMetadata: CalculateMetadataFunction<CampaignInput> = async ({
  props,
  abortSignal,
}) => {
  const response = await fetch(
    `https://api.example.com/campaigns/${props.campaignId}`,
    {signal: abortSignal},
  );

  if (!response.ok) {
    throw new Error(`Campaign not found: ${props.campaignId}`);
  }

  const campaign = (await response.json()) as {
    headline: string;
    scenes: Array<{title: string; durationInFrames: number}>;
  };

  return {
    durationInFrames: campaign.scenes.reduce(
      (sum, scene) => sum + scene.durationInFrames,
      0,
    ),
    props: {
      ...props,
      headline: campaign.headline,
      sceneCount: campaign.scenes.length,
    },
  };
};
```

## Component-level fetch with `delayRender()`

Use this for visual-only data that does not affect composition metadata.

```tsx
import React, {useEffect, useMemo, useState} from 'react';
import {
  AbsoluteFill,
  cancelRender,
  continueRender,
  delayRender,
} from 'remotion';

type Stat = {
  label: string;
  value: string;
};

export const LiveStatsCard: React.FC<{endpoint: string}> = ({endpoint}) => {
  const [stats, setStats] = useState<Stat[] | null>(null);
  const handle = useMemo(() => delayRender('Load stats'), []);

  useEffect(() => {
    fetch(endpoint)
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Stats failed: ${response.status}`);
        }
        return response.json() as Promise<Stat[]>;
      })
      .then((data) => {
        setStats(data);
        continueRender(handle);
      })
      .catch((error) => cancelRender(error));

    return () => continueRender(handle);
  }, [endpoint, handle]);

  if (!stats) {
    return null;
  }

  return (
    <AbsoluteFill style={{padding: 80, backgroundColor: '#f8fafc'}}>
      {stats.map((stat) => (
        <div key={stat.label} style={{fontSize: 48, marginBottom: 24}}>
          {stat.label}: {stat.value}
        </div>
      ))}
    </AbsoluteFill>
  );
};
```

## Avoid nondeterministic data

Bad:

```tsx
const response = await fetch('https://api.example.com/latest-post');
```

Better:

```tsx
const response = await fetch(`https://api.example.com/posts/${postId}?version=${version}`);
```

Use IDs, versions, timestamps, or snapshot URLs. Rendering "latest" is useful for dashboards but risky for reproducible video generation.

## Authenticated API calls

Do not hard-code secrets in composition code. For server-side rendering, fetch protected data in the render worker and pass sanitized props. If the browser rendering context must fetch, inject short-lived signed URLs or public data only.

```ts
const inputProps = {
  headline: campaign.headline,
  imageUrl: await createSignedImageUrl(campaign.heroImageKey),
};
```

## Caching pattern

```ts
const cache = new Map<string, Promise<unknown>>();

export const cachedJson = async <T,>(url: string): Promise<T> => {
  if (!cache.has(url)) {
    cache.set(
      url,
      fetch(url).then((response) => {
        if (!response.ok) {
          throw new Error(`${url} failed: ${response.status}`);
        }
        return response.json();
      }),
    );
  }

  return cache.get(url) as Promise<T>;
};
```

Cache at the worker/service layer when rendering many videos from the same campaign data.

## Checklist

- [ ] Fetch before render when possible.
- [ ] Use `calculateMetadata()` when fetched data affects duration or dimensions.
- [ ] Use `delayRender()` for component fetches and always resolve or cancel.
- [ ] Avoid "latest" URLs for reproducible outputs.
- [ ] Keep secrets out of browser-executed composition code.
- [ ] Pass compact, sanitized JSON as props.

# 06 - `delayRender()` and Prefetch

Most Remotion animations are synchronous frame math. Some videos also need data,
fonts, images, or media assets before a frame can be rendered correctly. Remotion
provides APIs for delaying a render until asynchronous work is ready.

Official docs:

- `delayRender()` and `continueRender()`:
  <https://www.remotion.dev/docs/delay-render>
- `useDelayRender()`: <https://www.remotion.dev/docs/use-delay-render>
- `prefetch()`: <https://www.remotion.dev/docs/prefetch>

## What `delayRender()` does

`delayRender()` pauses rendering while an async task completes. Once the task is
ready, call `continueRender(handle)`. If the task fails and cannot recover, call
`cancelRender(error)`.

The current official docs recommend using `useDelayRender()` inside React
components instead of the global functions because it scopes delays to a specific
render and is more future-proof for browser rendering.

## Preferred pattern: `useDelayRender()`

```tsx
import {useCallback, useEffect, useState} from 'react';
import {AbsoluteFill, useDelayRender} from 'remotion';

type ApiData = {
  title: string;
  metric: number;
};

export const DataDrivenVideo: React.FC = () => {
  const [data, setData] = useState<ApiData | null>(null);
  const {delayRender, continueRender, cancelRender} = useDelayRender();
  const [handle] = useState(() => delayRender('Loading report data'));

  const fetchData = useCallback(async () => {
    try {
      const response = await fetch('https://example.com/report.json');

      if (!response.ok) {
        throw new Error(`Report request failed: ${response.status}`);
      }

      const json = (await response.json()) as ApiData;
      setData(json);
      continueRender(handle);
    } catch (err) {
      cancelRender(err);
    }
  }, [cancelRender, continueRender, handle]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  if (!data) {
    return null;
  }

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1 style={{fontSize: 84}}>{data.title}</h1>
      <div style={{fontSize: 140, color: '#38bdf8'}}>{data.metric}%</div>
    </AbsoluteFill>
  );
};
```

Important details:

- Create the handle once, commonly with `useState(() => delayRender(...))`.
- Call `continueRender(handle)` after success.
- Call `cancelRender(err)` after unrecoverable failure.
- Add labels to make timeout errors easier to debug.

## Avoid top-level delayRender

Do not create a delay handle at module scope:

```tsx
// Avoid this. It can block unrelated compositions and composition discovery.
const handle = delayRender();
```

Keep delay handles inside components:

```tsx
const [handle] = useState(() => delayRender('Loading component data'));
```

## Avoid creating a handle on every render

Problem:

```tsx
const MyComponent: React.FC = () => {
  const {delayRender} = useDelayRender();
  const handle = delayRender('Created every render');

  return null;
};
```

Correct:

```tsx
const MyComponent: React.FC = () => {
  const {delayRender} = useDelayRender();
  const [handle] = useState(() => delayRender('Created once'));

  return null;
};
```

## Multiple async tasks

You can delay rendering for multiple tasks. Rendering continues only after all
handles are cleared.

```tsx
import {useEffect, useState} from 'react';
import {useDelayRender} from 'remotion';

export const MultipleLoads: React.FC = () => {
  const {delayRender, continueRender, cancelRender} = useDelayRender();
  const [dataHandle] = useState(() => delayRender('Loading data'));
  const [fontHandle] = useState(() => delayRender('Loading font'));

  useEffect(() => {
    const load = async () => {
      try {
        await Promise.all([
          fetch('/api/data').then((response) => response.json()),
          document.fonts.load('700 80px Inter'),
        ]);

        continueRender(dataHandle);
        continueRender(fontHandle);
      } catch (err) {
        cancelRender(err);
      }
    };

    load();
  }, [cancelRender, continueRender, dataHandle, fontHandle]);

  return null;
};
```

You can also use one handle for a `Promise.all()` if the tasks are conceptually
one loading phase.

## Data fetching in `calculateMetadata()`

The official docs note that data fetching can sometimes be better in
`calculateMetadata()` because it runs once rather than once per rendering
concurrency worker, and it avoids manual `continueRender()`.

```tsx
<Composition
  id="ReportVideo"
  component={ReportVideo}
  width={1920}
  height={1080}
  fps={30}
  durationInFrames={120}
  defaultProps={{
    reportId: 'q2',
  }}
  calculateMetadata={async ({props}) => {
    const response = await fetch(`https://example.com/reports/${props.reportId}`);

    if (!response.ok) {
      throw new Error(`Could not load report ${props.reportId}`);
    }

    const report = await response.json();

    return {
      durationInFrames: report.sections.length * 90,
      props: {
        ...props,
        report,
      },
    };
  }}
/>;
```

Use `calculateMetadata()` when the data determines duration, dimensions, or
normalized props. Use component-level `useDelayRender()` when the data is purely
needed for component rendering.

## Timeout and retries

The docs note that a delayed render must be continued within a timeout or the
render fails. Use labels so timeout messages identify the source:

```tsx
const [handle] = useState(() =>
  delayRender('Loading hero image', {
    timeoutInMilliseconds: 7000,
    retries: 1,
  }),
);
```

Use retries for flaky remote resources, not as a substitute for fixing slow
infrastructure.

## Image readiness

Remotion media components such as `<Img>`, `<Video>`, `<Audio>`, and related
components handle render delays internally. For normal usage, prefer Remotion's
media components over raw HTML tags.

```tsx
import {Img, staticFile} from 'remotion';

export const ProductImage: React.FC = () => {
  return (
    <Img
      src={staticFile('product.png')}
      style={{
        width: 720,
        height: 520,
        objectFit: 'contain',
      }}
    />
  );
};
```

If you need custom image loading or third-party rendering logic, wrap it in
`useDelayRender()`.

## Font readiness

Fonts can affect layout, so load them before rendering if needed:

```tsx
import {useEffect, useState} from 'react';
import {useDelayRender} from 'remotion';

export const FontGate: React.FC<{children: React.ReactNode}> = ({children}) => {
  const {delayRender, continueRender, cancelRender} = useDelayRender();
  const [handle] = useState(() => delayRender('Loading Inter font'));
  const [ready, setReady] = useState(false);

  useEffect(() => {
    document.fonts
      .load('800 96px Inter')
      .then(() => {
        setReady(true);
        continueRender(handle);
      })
      .catch((err) => cancelRender(err));
  }, [cancelRender, continueRender, handle]);

  if (!ready) {
    return null;
  }

  return <>{children}</>;
};
```

Wrap your video:

```tsx
export const Video: React.FC = () => {
  return (
    <FontGate>
      <TitleScene />
    </FontGate>
  );
};
```

## `prefetch()`

`prefetch()` fetches an asset and keeps it in memory so it is ready when you want
to play it in a `<Player>`. The official docs note that it is not recommended
for most cases and is mainly useful for Player workflows where media should be
fully loaded before it appears.

Basic official-style usage:

```tsx
import {prefetch} from 'remotion';

const {free, waitUntilDone} = prefetch('https://example.com/video.mp4', {
  method: 'blob-url',
});

waitUntilDone().then(() => {
  console.log('Video has finished loading');
});

// Later, when no longer needed:
free();
```

Remote assets need CORS support. For media fetched into Blob URLs, content type
can matter, especially in Safari.

## Prefetch in a Player-oriented component

```tsx
import {useEffect, useState} from 'react';
import {prefetch} from 'remotion';

export const usePrefetchedAsset = (src: string) => {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const {free, waitUntilDone} = prefetch(src, {
      method: 'blob-url',
      onProgress: (progress) => {
        if (progress.totalBytes === null) {
          console.log('Loaded bytes', progress.loadedBytes);
          return;
        }

        console.log(
          'Progress',
          Math.round((progress.loadedBytes / progress.totalBytes) * 100),
        );
      },
    });

    waitUntilDone()
      .then(() => setReady(true))
      .catch((err) => {
        console.error('Prefetch failed', err);
      });

    return () => free();
  }, [src]);

  return ready;
};
```

Use it with a Player preview UI:

```tsx
const PreviewGate: React.FC<{videoSrc: string}> = ({videoSrc}) => {
  const ready = usePrefetchedAsset(videoSrc);

  if (!ready) {
    return <div>Loading preview media...</div>;
  }

  return <PlayerShell videoSrc={videoSrc} />;
};
```

The docs state that media components can automatically use prefetched Blob URLs
when the original URL is passed and the asset has finished fetching.

## `delayRender()` vs `prefetch()`

| API | Primary purpose | Typical environment |
| --- | --- | --- |
| `useDelayRender()` | Prevent screenshot/render until async work is ready | Studio/rendering |
| `calculateMetadata()` | Determine metadata and normalized props before render | Studio/rendering |
| `prefetch()` | Warm media for smoother Player playback | Player/browser preview |

Use `useDelayRender()` for render correctness. Use `prefetch()` for Player media
readiness when it is actually needed.

## Practical data loading component

```tsx
import {useEffect, useState} from 'react';
import {
  AbsoluteFill,
  Sequence,
  interpolate,
  useCurrentFrame,
  useDelayRender,
} from 'remotion';

type Metric = {
  label: string;
  value: string;
};

type MetricsResponse = {
  title: string;
  metrics: Metric[];
};

export const MetricsVideo: React.FC = () => {
  const [data, setData] = useState<MetricsResponse | null>(null);
  const {delayRender, continueRender, cancelRender} = useDelayRender();
  const [handle] = useState(() => delayRender('Loading metrics data'));

  useEffect(() => {
    fetch('https://example.com/metrics.json')
      .then((response) => {
        if (!response.ok) {
          throw new Error(`Metrics request failed: ${response.status}`);
        }

        return response.json() as Promise<MetricsResponse>;
      })
      .then((json) => {
        setData(json);
        continueRender(handle);
      })
      .catch((err) => cancelRender(err));
  }, [cancelRender, continueRender, handle]);

  if (!data) {
    return null;
  }

  return (
    <AbsoluteFill
      style={{
        background: '#020617',
        color: 'white',
        padding: 100,
        boxSizing: 'border-box',
        fontFamily: 'Inter, Arial, sans-serif',
      }}
    >
      <h1 style={{fontSize: 84}}>{data.title}</h1>
      {data.metrics.map((metric, index) => (
        <Sequence key={metric.label} from={30 + index * 12}>
          <MetricRow metric={metric} />
        </Sequence>
      ))}
    </AbsoluteFill>
  );
};

const MetricRow: React.FC<{metric: Metric}> = ({metric}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 12], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });
  const x = interpolate(frame, [0, 18], [-50, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        opacity,
        transform: `translateX(${x}px)`,
        display: 'flex',
        justifyContent: 'space-between',
        width: 900,
        fontSize: 42,
        marginTop: 24,
      }}
    >
      <span>{metric.label}</span>
      <strong>{metric.value}</strong>
    </div>
  );
};
```

## Checklist

- Prefer `useDelayRender()` over global delay APIs in components.
- Create delay handles once with `useState`.
- Always call `continueRender(handle)` or `cancelRender(err)`.
- Add labels to delay handles.
- Fetch metadata-shaping data in `calculateMetadata()` when possible.
- Use Remotion media components for asset readiness.
- Use `prefetch()` mainly for Player media readiness.
- Clean up prefetched assets with `free()`.
- Keep async work deterministic and cacheable for reliable renders.

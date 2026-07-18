# Parameterized rendering

Parameterized rendering is the server-side pattern for turning JSON input into videos. The important rule from the Remotion docs: pass the same `inputProps` to both `selectComposition()` and `renderMedia()`.

## Minimal render script

```ts
import path from 'node:path';
import {bundle} from '@remotion/bundler';
import {renderMedia, selectComposition} from '@remotion/renderer';

type RenderInput = {
  title: string;
  subtitle: string;
  backgroundColor: string;
};

const inputProps: RenderInput = {
  title: 'Summer sale',
  subtitle: '40% off annual plans',
  backgroundColor: '#fef3c7',
};

const entryPoint = path.resolve('src/index.ts');
const serveUrl = await bundle({entryPoint});

const composition = await selectComposition({
  serveUrl,
  id: 'promo',
  inputProps,
});

await renderMedia({
  composition,
  serveUrl,
  codec: 'h264',
  outputLocation: 'out/summer-sale.mp4',
  inputProps,
});
```

`selectComposition()` evaluates the root, runs `calculateMetadata()` for the selected composition, and returns a concrete `VideoConfig` with duration, dimensions, fps, and props.

## Batch rendering

```ts
import path from 'node:path';
import {bundle} from '@remotion/bundler';
import {renderMedia, selectComposition} from '@remotion/renderer';

type Campaign = {
  slug: string;
  title: string;
  subtitle: string;
  backgroundColor: string;
};

const campaigns: Campaign[] = [
  {slug: 'summer', title: 'Summer sale', subtitle: 'Save 40%', backgroundColor: '#fef3c7'},
  {slug: 'webinar', title: 'Live webinar', subtitle: 'Register now', backgroundColor: '#dbeafe'},
];

const serveUrl = await bundle({entryPoint: path.resolve('src/index.ts')});

for (const campaign of campaigns) {
  const inputProps = campaign;
  const composition = await selectComposition({
    serveUrl,
    id: 'promo',
    inputProps,
  });

  await renderMedia({
    composition,
    serveUrl,
    codec: 'h264',
    outputLocation: `out/${campaign.slug}.mp4`,
    inputProps,
    onProgress: ({progress}) => {
      console.log(`${campaign.slug}: ${Math.round(progress * 100)}%`);
    },
  });
}
```

Bundle once, render many times.

## Render with progress and logging

```ts
import type {
  BrowserLog,
  RenderMediaOnProgress,
  RenderMediaOnDownload,
} from '@remotion/renderer';

const onProgress: RenderMediaOnProgress = ({
  progress,
  renderedFrames,
  encodedFrames,
  stitchStage,
}) => {
  console.log({
    progress: Math.round(progress * 100),
    renderedFrames,
    encodedFrames,
    stitchStage,
  });
};

const onBrowserLog = (log: BrowserLog) => {
  console.log(`[browser:${log.type}] ${log.text}`);
};

const onDownload: RenderMediaOnDownload = (src) => {
  console.log(`Downloading media: ${src}`);
  return ({percent, downloaded}) => {
    console.log(percent === null ? `${downloaded} bytes` : `${Math.round(percent * 100)}%`);
  };
};
```

Use these hooks in `renderMedia()`:

```ts
await renderMedia({
  composition,
  serveUrl,
  codec: 'h264',
  outputLocation,
  inputProps,
  onProgress,
  onBrowserLog,
  onDownload,
  logLevel: 'info',
});
```

## Renderer options that matter for templates

- `codec`: choose output format, commonly `h264` for MP4.
- `imageFormat`: use `png` for transparent frame rendering, `jpeg` for speed, `none` for audio-only.
- `pixelFormat`: needed for some transparent outputs.
- `frameRange`: render a still frame, preview segment, or partial output.
- `concurrency`: tune CPU/memory usage.
- `timeoutInMilliseconds`: time allowed for `delayRender()` calls.
- `mediaCacheSizeInBytes`: cache size for `@remotion/media` audio/video.
- `offthreadVideoCacheSizeInBytes`, `offthreadVideoThreads`: tune `<OffthreadVideo>` extraction.
- `sampleRate`: control output audio sample rate.

## API endpoint sketch

```ts
import express from 'express';
import {renderMarketingVideo} from './renderMarketingVideo';

const app = express();
app.use(express.json());

app.post('/renders', async (req, res, next) => {
  try {
    const job = await renderMarketingVideo({
      title: req.body.title,
      subtitle: req.body.subtitle,
      backgroundColor: req.body.backgroundColor,
    });

    res.status(202).json(job);
  } catch (error) {
    next(error);
  }
});
```

For production, render asynchronously in a worker queue. A render can take longer than an HTTP request should stay open.

## Idempotent output naming

```ts
import crypto from 'node:crypto';

export const renderKey = (compositionId: string, inputProps: unknown) => {
  const hash = crypto
    .createHash('sha256')
    .update(JSON.stringify(inputProps))
    .digest('hex')
    .slice(0, 16);

  return `${compositionId}-${hash}.mp4`;
};
```

This prevents duplicate renders for the same payload and makes cache lookup easier.

## Checklist

- [ ] Validate input before rendering.
- [ ] Pass identical `inputProps` to `selectComposition()` and `renderMedia()`.
- [ ] Bundle once per deploy/version, not once per job when possible.
- [ ] Log progress and browser errors.
- [ ] Use a queue for user-triggered renders.
- [ ] Store output artifacts with deterministic keys.

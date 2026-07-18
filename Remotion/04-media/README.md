# 04-media: Images, video, animated assets, fonts, and public files

This chapter focuses on the media primitives you use inside Remotion compositions. It is grounded in the first-party Remotion APIs:

- `<Img>` and `staticFile()` from `remotion`
- `<Video>` and `<Audio>` from `@remotion/media`
- `<OffthreadVideo>`, `<Html5Video>`, `<Html5Audio>`, `delayRender()`, `continueRender()`, and `cancelRender()` from `remotion`
- `<Gif>` from `@remotion/gif`
- `<Lottie>` from `@remotion/lottie`
- `<RemotionRiveCanvas>` from `@remotion/rive`

## Learning goals

By the end of this section, you should be able to:

- Choose between `staticFile()` and remote URLs for images, video, audio, fonts, and JSON assets.
- Use `<Img>` safely, including load failures, remote sources, retries, and manual `delayRender()` flows.
- Choose between `<Video>` from `@remotion/media`, `<OffthreadVideo>`, and HTML5 fallback tags.
- Explain the transparency and performance trade-offs of video frame extraction.
- Import GIF, Lottie, and Rive animations in a deterministic timeline.
- Load fonts and public files without relying on bundler-specific absolute paths.

## Files

| Guide | Focus |
|---|---|
| [01 Img](01-img.md) | `<Img>`, local files, remote URLs, load errors, `delayRender()` |
| [02 Video and OffthreadVideo](02-video-and-offthreadvideo.md) | `@remotion/media` `<Video>`, `<OffthreadVideo>`, transparency, HLS, fallbacks |
| [03 GIF, Lottie, Rive](03-gif-lottie-rive.md) | Animated image and vector animation packages |
| [04 Fonts and assets](04-fonts-and-assets.md) | `FontFace`, `staticFile()`, preload, asset manifests |
| [05 staticFile and public](05-staticfile-and-public.md) | Public folder conventions, URL encoding, `getStaticFiles()` |

## Media decision sheet

| Asset type | Preferred API | Notes |
|---|---|---|
| Still images | `<Img src={staticFile("...")}>` | Remotion waits for image load before rendering the frame. |
| Remote still images | `<Img src="https://...">` | Remote server must be reliable; CORS matters if drawing to canvas/effects. |
| Video clips | `<Video>` from `@remotion/media` | Recommended for new video usage; supports buffering and Mediabunny extraction. |
| Direct FFmpeg extraction | `<OffthreadVideo>` | Useful when configuring frame extraction directly or for fallback behavior. |
| Native preview behavior | `<Html5Video>` | Use only when you specifically need native HTML5 video behavior. |
| Audio clips | `<Audio>` from `@remotion/media` | Recommended for new audio usage; see the audio chapter. |
| GIF | `<Gif>` from `@remotion/gif` | Do not use `<Img>` for animated GIF timelines. |
| Lottie | `<Lottie>` from `@remotion/lottie` | Install `lottie-web`; check expression determinism. |
| Rive | `<RemotionRiveCanvas>` from `@remotion/rive` | Synchronizes Rive animation with Remotion time. |
| Fonts | `FontFace` + `staticFile()` | Use `delayRender()` while the font loads. |

## A reusable media composition shell

```tsx
import React from 'react';
import {AbsoluteFill, Img, staticFile} from 'remotion';
import {Video} from '@remotion/media';

export const MediaShowcase: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#101827'}}>
      <Video
        src={staticFile('clips/hero.mp4')}
        style={{width: '100%', height: '100%'}}
        objectFit="cover"
        muted
      />
      <AbsoluteFill
        style={{
          justifyContent: 'flex-end',
          padding: 80,
          background:
            'linear-gradient(180deg, transparent 45%, rgba(0,0,0,0.75) 100%)',
        }}
      >
        <Img
          src={staticFile('brand/logo-white.png')}
          style={{width: 240, height: 'auto'}}
        />
      </AbsoluteFill>
    </AbsoluteFill>
  );
};
```

## Production checklist

- [ ] Put stable project assets in `public/` and reference them with `staticFile()`.
- [ ] Use remote URLs only when the server supports range requests, CORS where needed, and predictable latency.
- [ ] Prefer `@remotion/media` `<Video>` and `<Audio>` for new work.
- [ ] Use `delayRender()` only for real async dependencies and always pair it with `continueRender()` or `cancelRender()`.
- [ ] Set `muted` on video clips whose audio should not be mixed; this can avoid unnecessary downloads during rendering.
- [ ] Use `transparent` video extraction only when you need an alpha channel.
- [ ] Keep asset filenames URI-safe, or rely on Remotion 4's `staticFile()` encoding instead of encoding paths yourself.

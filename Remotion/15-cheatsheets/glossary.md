# Glossary

## Composition

A registered video or still definition with an id, component, duration, FPS, width, height, and optional props/schema. Compositions are the main render targets.

## Frame

A discrete point in the video timeline. At 30 FPS, frame 30 is one second after frame 0. Remotion components should derive animation from the current frame.

## FPS

Frames per second. FPS controls timing conversion between frames and seconds. Changing FPS can affect animation and caption sync if timing is not handled carefully.

## `inputProps`

Data passed into a composition for a specific render. Use it for campaign copy, colors, metrics, captions, asset references, and product variants.

## `defaultProps`

Fallback props registered with a composition. Good defaults make Studio previews useful and CLI smoke renders easy.

## Studio

The local Remotion UI for previewing, scrubbing, editing props, and triggering draft renders. It is the fastest authoring loop.

## CLI render

A terminal render using commands such as `npx remotion render` or `npx remotion still`. It is repeatable and easy to put in scripts.

## Server-side rendering

Rendering from application code using `@remotion/renderer`. Common in product backends, queues, and worker processes.

## Lambda render

Distributed rendering on AWS Lambda through `@remotion/lambda`. Useful for bursty cloud rendering and SaaS export workflows.

## Cloud Run render

Rendering on Google Cloud Run through `@remotion/cloudrun`. Useful for GCP-native deployments and container-based cloud operations.

## `Sequence`

A timeline component that offsets children by a frame range. Use it for scenes, overlays, captions, and staged animations.

## `AbsoluteFill`

A convenience component for filling the entire composition with an absolutely positioned layer.

## `staticFile()`

A helper for referencing files in the `public/` directory in a render-safe way.

## `OffthreadVideo`

A video component optimized for rendering video sources frame-by-frame.

## Codec

The compression format used for output media, such as H.264, VP8, VP9, ProRes, or GIF-related formats. Codec choice affects compatibility, speed, transparency, and file size.

## CRF

Constant Rate Factor, a quality setting used by many encoders. Lower values generally mean higher quality and larger files.

## Artifact

The output of a render: MP4, WebM, still image, image sequence, subtitle file, or manifest. Production systems should store metadata for every artifact.

## Sidecar subtitles

Subtitle files such as SRT or VTT delivered alongside the video rather than burned into the pixels.

## Safe area

The region where important text and visuals should remain visible despite platform UI, cropping, or playback controls.

## Idempotency key

A stable key used to prevent duplicate render jobs when a user retries or a network request is repeated.

# Subtitles export

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Export subtitle sidecar files that match rendered video timing and publishing platform expectations.

## Mental model

Burned-in captions are pixels in the video. Subtitle exports are timed text files that players can enable, translate, style, or index. A production pipeline often needs both: burned-in social captions for engagement and SRT/VTT sidecars for accessibility and distribution.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Normalize caption timings before rendering.
2. Use the same caption source for on-screen captions and sidecar export.
3. Export SRT or VTT after editorial corrections, not directly from raw ASR when quality matters.
4. Include sidecar files in the render job artifact manifest.
5. Validate exported subtitles in the target player or platform.
6. Version subtitle files with the same job id or content hash as the video artifact.


## Example


```ts
import {serializeSrt} from '@remotion/captions';

const srt = serializeSrt({
  captions: [
    {text: 'Build videos with React.', startMs: 0, endMs: 1800},
    {text: 'Render them anywhere.', startMs: 1900, endMs: 3600},
  ],
});

await fs.promises.writeFile('out/product-promo.en.srt', srt, 'utf8');
```

```json
{
  "video": "s3://exports/jobs/123/final.mp4",
  "subtitles": [
    {"language": "en", "format": "srt", "url": "s3://exports/jobs/123/final.en.srt"}
  ],
  "burnedInCaptions": true
}
```


## Checklist


- Sidecar timing is generated from the final approved caption source.
- The export uses the same language code and job id as the video artifact.
- Caption files are uploaded with correct content type.
- Platform import is tested before scaling the workflow.
- Subtitle edits trigger a new artifact version or manifest update.


## Pitfalls


- SRT and VTT have different formatting rules; do not assume one parser accepts the other.
- Rounding milliseconds can accumulate visible drift in long videos.
- Editing burned-in captions without regenerating sidecars causes mismatched accessibility text.
- Some platforms reprocess subtitles and may change line breaks.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Exporting captions: <https://www.remotion.dev/docs/captions/exporting>
- `serializeSrt()`: <https://www.remotion.dev/docs/captions/serialize-srt>
- `parseSrt()`: <https://www.remotion.dev/docs/captions/parse-srt>

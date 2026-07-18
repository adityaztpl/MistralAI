# 14 - Examples

This section provides copy-friendly Remotion example patterns in markdown. They are intentionally written as teaching snippets rather than complete applications: each file explains the composition shape, props, timing, styling, render command, and extension ideas.

## Examples

| File | What you build |
|------|----------------|
| [01-hello-world-title-card.tsx.md](01-hello-world-title-card.tsx.md) | A minimal TSX title card with frame-based animation |
| [02-lower-third.md](02-lower-third.md) | A reusable lower-third overlay for names, roles, and captions |
| [03-product-promo-scenes.md](03-product-promo-scenes.md) | A multi-scene product promo with sequences and props |
| [04-data-driven-stats-video.md](04-data-driven-stats-video.md) | A data-driven stats video with animated counters and cards |
| [05-youtube-outro.md](05-youtube-outro.md) | A YouTube outro with subscribe CTA and video placeholders |
| [06-instagram-reel-vertical.md](06-instagram-reel-vertical.md) | A vertical social reel with safe areas, captions, and pacing |

## How to use these examples

1. Start with a fresh Remotion template or an existing Remotion entry point.
2. Copy the relevant component into `src/compositions/`.
3. Register it with `<Composition />` in your root file.
4. Render a still at a representative frame, then render the full video.
5. Replace static copy with `inputProps` once the design works.

## Official docs

- Templates: <https://www.remotion.dev/templates>
- Composition: <https://www.remotion.dev/docs/composition>
- Sequence: <https://www.remotion.dev/docs/sequence>
- Rendering: <https://www.remotion.dev/docs/render>

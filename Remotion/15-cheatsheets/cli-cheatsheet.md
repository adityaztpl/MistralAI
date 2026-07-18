# CLI cheatsheet

## Studio

```bash
npx remotion studio
npm start
npm run remotion
```

Use Studio for preview, frame scrubbing, prop exploration, and draft renders.

## Render video

```bash
npx remotion render src/index.ts ProductPromo out/product-promo.mp4
npx remotion render src/index.ts ProductPromo out/product-promo.mp4 --props=./props/acme.json
npx remotion render src/index.ts ProductPromo out/draft.mp4 --scale=0.5 --crf=28
```

## Render stills

```bash
npx remotion still src/index.ts ProductPromo out/poster.png --frame=90
npx remotion still src/index.ts ProductPromo out/first-frame.png --frame=0
```

## Render frame ranges

```bash
npx remotion render src/index.ts ProductPromo out/scene-1.mp4 --frames=0-59
npx remotion render src/index.ts ProductPromo out/debug.mp4 --frames=120-150 --concurrency=1
```

## Output tuning

```bash
# Common H.264 MP4
npx remotion render src/index.ts Promo out/final.mp4 --codec=h264 --crf=18

# Transparent WebM overlay
npx remotion render src/index.ts Overlay out/overlay.webm --codec=vp8 --pixel-format=yuva420p

# Lower-quality draft
npx remotion render src/index.ts Promo out/draft.mp4 --scale=0.5 --crf=30
```

## Good package scripts

```jsonc
{
  "scripts": {
    "start": "remotion studio",
    "render": "remotion render src/index.ts ProductPromo out/product-promo.mp4",
    "render:poster": "remotion still src/index.ts ProductPromo out/poster.png --frame=90",
    "render:smoke": "remotion render src/index.ts ProductPromo out/smoke.mp4 --frames=0-30"
  }
}
```

## Troubleshooting commands

```bash
# Test one still
npx remotion still src/index.ts Broken out/debug.png --frame=123

# Reduce resource pressure
npx remotion render src/index.ts Broken out/debug.mp4 --concurrency=1 --scale=0.5

# Re-render with props file to remove shell escaping issues
npx remotion render src/index.ts Broken out/debug.mp4 --props=./props/debug.json
```

## Official docs

- CLI overview: <https://www.remotion.dev/docs/cli>
- Render command: <https://www.remotion.dev/docs/cli/render>
- Studio command: <https://www.remotion.dev/docs/cli/studio>

# Hooks cheatsheet

## `useCurrentFrame()`

Use it to derive animation state from the current frame.

```tsx
const frame = useCurrentFrame();
const opacity = interpolate(frame, [0, 20], [0, 1], {extrapolateRight: 'clamp'});
```

Rules of thumb:

- Prefer frame-based values over `Date.now()`, `setInterval()`, or `requestAnimationFrame()`.
- Convert frames to seconds with `frame / fps` when comparing to transcript or media timing.
- Clamp values when an animation should hold after its range.

## `useVideoConfig()`

Use it for FPS, dimensions, and duration.

```tsx
const {fps, width, height, durationInFrames} = useVideoConfig();
```

Use cases:

- Responsive layout based on composition size.
- Spring configs that need FPS.
- Safe-area calculations.
- Duration-aware outro or progress bars.

## `spring()`

Natural motion based on frame and FPS.

```tsx
const entrance = spring({frame, fps, config: {damping: 18, stiffness: 120}});
```

Use for:

- Title entrances.
- Card reveals.
- Camera pushes.
- Button or CTA emphasis.

## `interpolate()`

Map frame ranges to values.

```tsx
const x = interpolate(frame, [0, 30, 90], [-120, 0, 0]);
const scale = interpolate(frame, [0, 15], [0.94, 1], {extrapolateRight: 'clamp'});
```

Use for:

- Opacity fades.
- Position changes.
- Counter values.
- Color interpolation when paired with `interpolateColors()`.

## Async render hooks and helpers

```tsx
const handle = delayRender('load data');
fetchData()
  .then(() => continueRender(handle))
  .catch((err) => cancelRender(err));
```

Use sparingly and always resolve or cancel. Hanging handles are a common cause of failed renders.

## Official docs

- `useCurrentFrame()`: <https://www.remotion.dev/docs/use-current-frame>
- `useVideoConfig()`: <https://www.remotion.dev/docs/use-video-config>
- `spring()`: <https://www.remotion.dev/docs/spring>
- `interpolate()`: <https://www.remotion.dev/docs/interpolate>

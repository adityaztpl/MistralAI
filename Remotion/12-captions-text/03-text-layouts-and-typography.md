# Text layouts and typography

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Design text systems that survive different aspect ratios, brand rules, languages, and render environments.

## Mental model

Video typography is layout plus timing. A text component must know safe areas, maximum width, line count, font loading, background contrast, and how long the viewer has to read. Remotion makes text programmable, but the constraints are editorial and visual.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Define type roles: hero, subtitle, caption, callout, lower third, disclaimer.
2. Set safe-area margins per format: 16:9, 1:1, 9:16, and 4:5.
3. Load fonts deterministically and test fallback behavior.
4. Use line clamps or fitting utilities for user-generated text.
5. Add stroke, shadow, scrim, or background pills for contrast over footage.
6. Keep important text away from platform UI overlays such as captions, buttons, and profile chrome.


## Example


```tsx
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

export const TitleCardText = ({title, subtitle}: {title: string; subtitle: string}) => {
  const frame = useCurrentFrame();
  const y = interpolate(frame, [0, 24], [50, 0], {extrapolateRight: 'clamp'});
  const opacity = interpolate(frame, [0, 18], [0, 1], {extrapolateRight: 'clamp'});

  return (
    <AbsoluteFill style={{justifyContent: 'center', padding: 96}}>
      <h1 style={{fontSize: 112, lineHeight: 0.92, margin: 0, transform: `translateY(${y}px)`, opacity}}>
        {title}
      </h1>
      <p style={{fontSize: 38, maxWidth: 900, opacity: 0.82}}>{subtitle}</p>
    </AbsoluteFill>
  );
};
```

```text
Safe area starter values
- Landscape 1920x1080: 96 px outer margin
- Square 1080x1080: 72 px outer margin
- Vertical 1080x1920: 96 px horizontal, 180 px top/bottom when posting to social apps
```


## Checklist


- The font is licensed for video use and loaded consistently.
- Long user-generated strings have a fitting or truncation strategy.
- Contrast is tested on light and dark footage.
- Text animations leave enough hold time for reading.
- Localization is considered before hard-coding uppercase, line breaks, or fixed widths.


## Pitfalls


- Browser font fallback can shift layout between preview and render if fonts are not loaded.
- All-caps text can be less readable for longer captions.
- Text near the bottom of vertical videos may be covered by platform UI.
- CSS filters and shadows can become expensive when applied to huge text layers every frame.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Fonts API: <https://www.remotion.dev/docs/fonts-api/>
- Layout utilities: <https://www.remotion.dev/docs/layout-utils/>
- Tailwind: <https://www.remotion.dev/docs/tailwind>

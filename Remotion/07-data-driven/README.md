# 07-data-driven: Props, schemas, metadata, APIs, and templates

Data-driven Remotion means the same React composition can render many videos by changing JSON input, schema-controlled defaults, fetched content, and dynamic metadata.

Core APIs:

- `<Composition defaultProps schema calculateMetadata>`
- `inputProps` for CLI, Studio render dialog, Player, and renderer APIs
- `getInputProps()` for root-level access when needed
- `selectComposition()` and `renderMedia()` from `@remotion/renderer`
- Zod schemas and `@remotion/zod-types`

## Files

| Guide | Focus |
|---|---|
| [01 Input props](01-input-props.md) | Typed React props, `defaultProps`, CLI `--props`, prop resolution |
| [02 Zod schema Studio](02-zod-schema-studio.md) | Visual editing with Zod and Remotion-specific schema helpers |
| [03 Parameterized rendering](03-parameterized-rendering.md) | `bundle()`, `selectComposition()`, `renderMedia()`, render queues |
| [04 Dynamic duration metadata](04-dynamic-duration-metadata.md) | `calculateMetadata()` for duration, dimensions, defaults, and transformed props |
| [05 API fetched content](05-api-fetched-content.md) | Fetching JSON safely for deterministic renders |
| [06 Templates for marketing videos](06-templates-for-marketing-videos.md) | Practical template architecture for campaigns |

## Props resolution mental model

During rendering, Remotion resolves props roughly as:

1. `defaultProps` from `<Composition>`
2. `inputProps` from CLI, Studio render dialog, or renderer API
3. transformed output from `calculateMetadata()`

The component receives the final props as normal React props.

## Minimal typed composition

```tsx
import React from 'react';
import {AbsoluteFill, Composition} from 'remotion';

type PromoProps = {
  headline: string;
  color: string;
};

export const Promo: React.FC<PromoProps> = ({headline, color}) => {
  return (
    <AbsoluteFill style={{backgroundColor: color, justifyContent: 'center'}}>
      <h1 style={{fontSize: 96, textAlign: 'center'}}>{headline}</h1>
    </AbsoluteFill>
  );
};

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="promo"
      component={Promo}
      width={1080}
      height={1080}
      fps={30}
      durationInFrames={120}
      defaultProps={{
        headline: 'Launch faster',
        color: '#dbeafe',
      }}
    />
  );
};
```

Render override:

```bash
npx remotion render promo out/promo.mp4 --props='{"headline":"Summer sale","color":"#fee2e2"}'
```

## Design rule

Keep props JSON-serializable. `defaultProps` and `inputProps` should not contain functions, class instances, or huge payloads. Use IDs, URLs, and compact content; fetch or compute heavy data in a controlled place.

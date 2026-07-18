# Tailwind and styling

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Use Tailwind, CSS, and design tokens in Remotion while keeping render output deterministic.

## Mental model

Remotion renders in a browser, so normal web styling tools are available. The difference is that styles become pixels in a video. Utility classes, CSS modules, variables, and design tokens should resolve consistently in Studio, CLI, server render, and cloud render environments.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Pick one styling strategy per project: Tailwind, CSS modules, vanilla CSS, styled components, or a design-system package.
2. Configure Remotion webpack overrides for Tailwind when needed.
3. Keep video-safe design tokens close to the composition package.
4. Avoid styles that depend on interactive browser states unless intentionally controlled by frame/props.
5. Test final output after platform compression; thin lines, gradients, and small text can degrade.
6. Document aspect-ratio-specific spacing and safe area rules.


## Example


```tsx
export const StatCard = ({label, value}: {label: string; value: string}) => (
  <div className="rounded-3xl bg-white/10 p-10 shadow-2xl backdrop-blur-md">
    <div className="text-3xl font-semibold uppercase tracking-[0.25em] text-cyan-200">{label}</div>
    <div className="mt-4 text-8xl font-black text-white">{value}</div>
  </div>
);
```

```ts
// remotion.config.ts sketch for Tailwind setup; verify package/version in official docs.
import {Config} from '@remotion/cli/config';
import {enableTailwind} from '@remotion/tailwind';

Config.overrideWebpackConfig((currentConfiguration) => {
  return enableTailwind(currentConfiguration);
});
```


## Checklist


- Tailwind content paths include Remotion composition files.
- Fonts and CSS are loaded before frames are captured.
- Dynamic class names are safelisted or avoided.
- Design tokens map to video-safe contrast and spacing.
- Styling is tested in local CLI render, not just Studio.


## Pitfalls


- Tailwind purge/content misconfiguration can remove classes used only in compositions.
- Dynamic class string construction may not be detected by the build system.
- Browser-only responsive assumptions can break deterministic output.
- Heavy CSS filters on large layers can slow renders dramatically.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Tailwind: <https://www.remotion.dev/docs/tailwind>
- Tailwind v3 package: <https://www.remotion.dev/docs/tailwind/tailwind>
- Tailwind v4 package: <https://www.remotion.dev/docs/tailwind-v4/overview>

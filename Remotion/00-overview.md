# 00 - Remotion Product Overview

Remotion is both a programming model and an ecosystem for creating videos with
React. The core idea is simple: a video is a React component evaluated at many
frame numbers. If the component is deterministic for each frame, Remotion can
preview it interactively and render it reproducibly.

## Core product surfaces

### Remotion Studio

Remotion Studio is the local development interface. It is the place where you
preview compositions, scrub the timeline, inspect frames, adjust props, and
render outputs while developing.

Typical usage:

```bash
npm run dev
```

In a project, Studio discovers compositions registered in the root component:

```tsx
import {Composition, Folder} from 'remotion';
import {HeroAd} from './HeroAd';
import {SquareSocialCut} from './SquareSocialCut';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Folder name="Ads">
        <Composition
          id="HeroAd"
          component={HeroAd}
          width={1920}
          height={1080}
          fps={30}
          durationInFrames={240}
        />
        <Composition
          id="SquareSocialCut"
          component={SquareSocialCut}
          width={1080}
          height={1080}
          fps={30}
          durationInFrames={180}
        />
      </Folder>
    </>
  );
};
```

Studio is especially useful because a Remotion project is code, but timing and
motion still need visual feedback. You can scrub frame by frame, pause on
precise moments, and catch layout issues that a pure test suite would miss.

### Remotion Player

`@remotion/player` is a React component for embedding Remotion playback inside a
web app. It is not just a video tag. It can play a Remotion component directly,
which means props, controls, and surrounding app state can drive the preview.

Example use case: a SaaS dashboard where users customize a generated video
before rendering it.

```tsx
import {Player} from '@remotion/player';
import {MarketingVideo} from './MarketingVideo';

export const VideoPreviewPanel: React.FC = () => {
  return (
    <Player
      component={MarketingVideo}
      compositionWidth={1280}
      compositionHeight={720}
      durationInFrames={150}
      fps={30}
      controls
      inputProps={{
        companyName: 'Northstar Analytics',
        accentColor: '#38bdf8',
      }}
      style={{
        width: '100%',
        borderRadius: 16,
        overflow: 'hidden',
      }}
    />
  );
};
```

The Player is useful for editors, preview screens, approval workflows, and
internal tools where renderable React video needs to live inside an existing app.

### Remotion Lambda

Remotion Lambda is the AWS Lambda rendering solution. It is designed for
serverless rendering at scale: deploy a Remotion project, invoke renders, and
receive output files. This is a natural fit for bursty workloads such as
personalized video campaigns or on-demand exports.

High-level shape:

```ts
// Conceptual server-side orchestration.
import {renderMediaOnLambda} from '@remotion/lambda/client';

export const startRender = async () => {
  const result = await renderMediaOnLambda({
    region: 'us-east-1',
    functionName: 'remotion-render-4-0-000-mem2048mb-disk2048mb-120sec',
    serveUrl: 'https://example-bucket.s3.amazonaws.com/sites/my-site',
    composition: 'HeroAd',
    inputProps: {
      customerName: 'Ada',
      planName: 'Enterprise',
    },
    codec: 'h264',
  });

  return result.renderId;
};
```

The exact configuration changes by version and deployment setup, so use the
official Lambda docs when implementing production infrastructure:
<https://www.remotion.dev/docs/lambda>

### Remotion Cloud Run

Remotion Cloud Run is the Google Cloud Run rendering option. It serves a similar
role to Lambda but targets Google Cloud infrastructure. It is relevant when a
team already runs workloads on GCP, wants containerized rendering services, or
prefers Cloud Run's scaling and operations model.

Conceptually, the workflow is:

1. Bundle or deploy the Remotion project.
2. Start a render job with a composition ID and input props.
3. Store or return the resulting artifact.

Official docs: <https://www.remotion.dev/docs/cloudrun>

### Editor Starter

Remotion's Editor Starter is a foundation for building browser-based video
editing experiences. It uses Remotion's renderable React components and packages
them into an application-like editor workflow.

Think of it as a starting point when you want:

- A video editor UI.
- Tracks, layers, or editable scenes.
- Preview through Remotion Player.
- Export through Remotion rendering.
- A codebase you can own and adapt, rather than a hosted editor only.

### Timeline

The Remotion timeline appears in Studio and editor tooling. At the code level,
timeline structure is mostly created through `<Sequence>` and `<Series>`.

```tsx
import {AbsoluteFill, Sequence, Series} from 'remotion';

export const TimelineComposition: React.FC = () => {
  return (
    <AbsoluteFill>
      <Series>
        <Series.Sequence durationInFrames={45} name="Logo reveal">
          <LogoReveal />
        </Series.Sequence>
        <Series.Sequence durationInFrames={90} name="Product shots">
          <ProductShots />
        </Series.Sequence>
        <Series.Sequence durationInFrames={60} name="CTA">
          <CallToAction />
        </Series.Sequence>
      </Series>

      <Sequence from={20} durationInFrames={140} name="Music visualizer">
        <Visualizer />
      </Sequence>
    </AbsoluteFill>
  );
};
```

Important timeline idea: Remotion does not require you to manually calculate
every absolute start frame. `<Series>` can place scenes sequentially, while
`<Sequence>` can offset, trim, freeze, premount, and label sections.

### Recorder

Remotion Recorder is part of the ecosystem for recording and using captured
media in video workflows. In broader Remotion architecture, recorder-style
features usually pair with:

- Browser previews via Player.
- Captured webcam, screen, or audio assets.
- Deterministic render compositions.
- Server-side render/export infrastructure.

When building recorder workflows, keep the Remotion split clear:

```text
Capture UI -> media assets + metadata -> Remotion composition -> final render
```

That separation makes it easier to preview a recording immediately while still
rendering a polished result later.

### AI skills

Remotion publishes AI-oriented guidance and skills for coding agents. The
official getting-started docs include a workflow that installs Remotion skills:

```bash
npx -y skills@latest add remotion-dev/skills -g -y
```

And a starter flow:

```bash
npx create-video@latest --yes --blank my-video
cd my-video
npm i
npx remotion skills add
npm run dev
```

The practical value of AI skills is that Remotion videos are code. A coding
agent can generate scenes, refactor component structure, wire props, and apply
animation primitives when the project has a clear prompt and examples.

### Templates

The `create-video` wizard can scaffold projects from templates. Templates are
useful because a Remotion project involves more than React components:

- Root registration.
- TypeScript configuration.
- bundler configuration.
- render scripts.
- example compositions.
- optional styling setup such as Tailwind.
- optional AI skills.

Starter command:

```bash
npx create-video@latest
```

For first-time learning, the official docs recommend the Hello World template.
For a minimal base, use a blank template.

## License note: company vs free use

Remotion has a source-available licensing model with free use for many cases and
a company license requirement for some commercial/company usage. The exact
thresholds and terms can change, so treat this dump as a technical guide, not
legal guidance.

Always verify the current license page before adopting Remotion for a company or
client project:

- Docs: <https://www.remotion.dev/docs/>
- License information: <https://www.remotion.dev/license>

Engineering recommendation:

```text
Personal experiment or open learning project -> read docs, proceed.
Internal/company production workflow -> check license terms before rollout.
Client deliverable or SaaS feature -> check license terms and budget early.
```

## Where Remotion fits

Remotion is excellent when:

- The video has many variants.
- Video content is data-driven.
- Design systems, web components, or React state should be reused.
- You want version control and code review for motion graphics.
- You need a programmatic render pipeline.
- You need interactive preview before export.

It is less ideal when:

- A timeline-only visual editor is the whole workflow.
- Non-developers need to do all animation without code.
- The output is a single handcrafted video where After Effects or a NLE is faster.
- You need unsupported native codecs or platform-specific video features without
  a browser/rendering bridge.

## Concept map

```text
React component
  -> uses frame hooks
  -> maps frames to visual state
  -> registered as a Composition
  -> previewed in Studio or Player
  -> rendered locally, on Lambda, or on Cloud Run
```

## Minimal production-ish example

```tsx
import {
  AbsoluteFill,
  Composition,
  Sequence,
  interpolate,
  spring,
  useCurrentFrame,
  useVideoConfig,
} from 'remotion';

type ReportVideoProps = {
  title: string;
  metric: number;
  accent: string;
};

const ReportVideo: React.FC<ReportVideoProps> = ({title, metric, accent}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();

  const cardProgress = spring({
    frame,
    fps,
    config: {damping: 16, stiffness: 130},
  });

  const metricOpacity = interpolate(frame, [30, 50], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill style={{backgroundColor: '#020617', color: 'white'}}>
      <AbsoluteFill
        style={{
          alignItems: 'center',
          justifyContent: 'center',
          fontFamily: 'Inter, Arial, sans-serif',
        }}
      >
        <div
          style={{
            width: 900,
            padding: 64,
            borderRadius: 36,
            background: '#0f172a',
            border: `4px solid ${accent}`,
            transform: `translateY(${interpolate(
              cardProgress,
              [0, 1],
              [80, 0],
            )}px)`,
          }}
        >
          <h1 style={{fontSize: 72, margin: 0}}>{title}</h1>

          <Sequence from={30}>
            <div style={{opacity: metricOpacity, marginTop: 48}}>
              <div style={{fontSize: 144, color: accent}}>{metric}%</div>
              <div style={{fontSize: 32, color: '#cbd5e1'}}>
                quarter-over-quarter growth
              </div>
            </div>
          </Sequence>
        </div>
      </AbsoluteFill>
    </AbsoluteFill>
  );
};

export const RemotionRoot: React.FC = () => {
  return (
    <Composition
      id="ReportVideo"
      component={ReportVideo}
      width={1920}
      height={1080}
      fps={30}
      durationInFrames={150}
      defaultProps={{
        title: 'Revenue Pulse',
        metric: 38,
        accent: '#38bdf8',
      }}
    />
  );
};
```

This example shows the ecosystem's core promise: props define a video variant,
React defines visual structure, frame hooks define timing, and Remotion can
preview or render the result.

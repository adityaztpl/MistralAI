# Animated captions

> This guide is part of the local Remotion curriculum dump. It favors practical production patterns over a copied reference manual.

## Outcome

Build readable animated captions with word highlighting, pop-in motion, and platform-friendly styling.

## Mental model

Animated captions use timing metadata to emphasize the current word or phrase. The animation should help comprehension, not distract from it. Good components use phrase grouping, stable line boxes, predictable emphasis, and restrained motion curves.

The reliable Remotion pattern is to make every visual decision a pure function of frame, config, props, and loaded assets. This is especially important for complex scenes: 3D canvases, word-level captions, editor timelines, and AI-generated media can all become nondeterministic if they depend on wall-clock time or uncontrolled browser state.

## Workflow


1. Start from a normalized caption array with optional word-level timing.
2. Group words into lines that fit the target format.
3. Highlight the active word based on current time.
4. Add small scale, color, or background changes rather than moving the whole line constantly.
5. Keep line height and box dimensions stable to avoid jitter.
6. Render a silent preview and ask whether the text is understandable without audio.


## Example


```tsx
type Word = {text: string; startMs: number; endMs: number};

export const KaraokeCaption = ({words}: {words: Word[]}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const now = (frame / fps) * 1000;

  return (
    <div className="karaoke-caption">
      {words.map((word) => {
        const active = now >= word.startMs && now < word.endMs;
        return (
          <span
            key={`${word.text}-${word.startMs}`}
            style={{
              color: active ? '#fde047' : 'white',
              transform: active ? 'scale(1.08)' : 'scale(1)',
              display: 'inline-block',
              marginRight: 10,
            }}
          >
            {word.text}
          </span>
        );
      })}
    </div>
  );
};
```

```css
.karaoke-caption {
  position: absolute;
  left: 96px;
  right: 96px;
  bottom: 120px;
  text-align: center;
  font-size: 72px;
  line-height: 1.05;
  font-weight: 900;
  text-shadow: 0 5px 22px rgba(0, 0, 0, 0.7);
}
```


## Checklist


- Animated emphasis never reduces contrast.
- Captions stay inside platform safe areas.
- The current word is highlighted accurately enough for human perception.
- Layout does not jump when a word becomes active.
- Word-level captions fall back to phrase-level captions when timing data is missing.


## Pitfalls


- Overusing bounce, rotation, and color changes makes captions tiring.
- Per-word React keys must be stable or animation state can flicker.
- Highlighting every filler word can make the video feel noisy.
- Captions that look good in 1080x1920 may be unreadable after platform compression.


## Practice exercise

Build a 6-second composition related to this topic. Add one prop that changes the look, one animation based on `useCurrentFrame()`, and one render command for a still plus a full video. Then write a short note explaining how the design would behave on Lambda or Cloud Run.


## Official docs

- Animated captions: <https://www.remotion.dev/docs/animated-captions>
- `createTikTokStyleCaptions()`: <https://www.remotion.dev/docs/captions/create-tiktok-style-captions>

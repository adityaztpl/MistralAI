# 05-audio: Sound, voiceover, music beds, and visualization

Audio in Remotion is timeline-driven. You can place tracks at frame offsets, trim them, loop them, fade them, visualize their waveform, and mix them with video audio. For new work, use `<Audio>` from `@remotion/media`; use `<Html5Audio>` from `remotion` only when you specifically need native HTML5 audio behavior or fallback props.

## Files

| Guide | Focus |
|---|---|
| [01 Using audio](01-using-audio.md) | Add music, voiceover, SFX, remote audio, and static files |
| [02 HTML5 audio vs Audio](02-html5-audio-vs-audio.md) | `@remotion/media` `<Audio>` compared with `<Html5Audio>` |
| [03 Volume, trimming, speed](03-volume-trimming-speed.md) | `volume`, `trimBefore`, `trimAfter`, `playbackRate`, `loop` |
| [04 Audio visualization](04-audio-visualization.md) | `getAudioData()`, `useAudioData()`, `visualizeAudio()` patterns |
| [05 Soundtrack patterns](05-soundtrack-patterns.md) | Music beds, ducking, SFX buses, scene-level sound design |

## Recommended import

```tsx
import {Audio} from '@remotion/media';
import {staticFile} from 'remotion';
```

```tsx
export const SimpleMusicBed: React.FC = () => {
  return <Audio src={staticFile('audio/music.mp3')} volume={0.35} />;
};
```

## Mental model

- Audio tags are layers on the Remotion timeline.
- The frame passed to `volume={(frame) => ...}` is local to that audio clip unless loop behavior changes it.
- Use `from` and `durationInFrames` to place audio on the composition timeline.
- Use `trimBefore` and `trimAfter` to choose the range from the source file.
- Use `muted` to keep a tag mounted while silencing it.
- Use `loop` for beds or ambiences that must cover arbitrary durations.

## Common track types

| Track | Typical API | Notes |
|---|---|---|
| Voiceover | `<Audio src={staticFile("vo.mp3")}>` | Usually controls narrative timing. |
| Music bed | `<Audio loop volume={...}>` | Fade in/out; duck under narration. |
| Sound effects | `<Sequence from={...}><Audio ... /></Sequence>` or `from` prop | Short, named timeline items. |
| Video audio | `<Video volume={...}>` | Mute video if audio is not needed. |
| Visualization source | `useAudioData()` | Load waveform data before drawing bars/waves. |

## Minimal multi-track composition

```tsx
import React from 'react';
import {AbsoluteFill, Sequence, staticFile} from 'remotion';
import {Audio} from '@remotion/media';

export const AudioStack: React.FC = () => {
  return (
    <AbsoluteFill style={{backgroundColor: '#020617', color: 'white'}}>
      <Audio
        name="Music bed"
        src={staticFile('audio/music.mp3')}
        loop
        volume={0.25}
      />
      <Audio
        name="Voiceover"
        from={30}
        src={staticFile('audio/voiceover.mp3')}
        volume={1}
      />
      <Sequence from={96} durationInFrames={18}>
        <Audio
          name="Button click SFX"
          src={staticFile('audio/click.wav')}
          volume={0.6}
        />
      </Sequence>
    </AbsoluteFill>
  );
};
```

## Production checklist

- [ ] Use `staticFile()` for local audio assets.
- [ ] Prefer WAV/MP3/AAC sources that Chrome can decode reliably.
- [ ] Normalize loudness before import when possible.
- [ ] Use frame-based fades instead of editing audio destructively for every version.
- [ ] Pass `requestInit` for protected remote audio.
- [ ] Match `sampleRate` render settings when source quality matters.
- [ ] Fail fast if voiceover is required and cannot load.

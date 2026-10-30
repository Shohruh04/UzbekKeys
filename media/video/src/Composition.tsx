import {Audio, interpolate, Sequence, staticFile, useVideoConfig} from 'remotion';
import {Frame} from './design';
import {Intro} from './scenes/Intro';
import {Letters} from './scenes/Letters';
import {Modifiers} from './scenes/Modifiers';
import {Download} from './scenes/Download';

export type PromoProps = {narration: boolean; music: boolean};

export const UzbekKeysPromo = ({narration,music}: PromoProps) => {
  const {fps} = useVideoConfig();
  return <Frame progress>
    <Sequence name="Yangi alifbo" durationInFrames={5 * fps} premountFor={fps}><Intro /></Sequence>
    <Sequence name="Tört harf" from={5 * fps} durationInFrames={7 * fps} premountFor={fps}><Letters /></Sequence>
    <Sequence name="Taniş belgilar" from={12 * fps} durationInFrames={5.4 * fps} premountFor={fps}><Modifiers /></Sequence>
    <Sequence name="Yuklab oliş" from={17.4 * fps} durationInFrames={6.6 * fps} premountFor={fps}><Download /></Sequence>
    {narration ? <>
      <Sequence name="ElevenLabs — Intro" from={Math.round(.14 * fps)} premountFor={fps}><Audio src={staticFile('voiceover/intro.mp3')} volume={.92} /></Sequence>
      <Sequence name="ElevenLabs — Letters" from={Math.round(5.3 * fps)} premountFor={fps}><Audio src={staticFile('voiceover/letters.mp3')} volume={.92} /></Sequence>
      <Sequence name="ElevenLabs — AltGr" from={Math.round(12.3 * fps)} premountFor={fps}><Audio src={staticFile('voiceover/modifiers.mp3')} volume={.92} /></Sequence>
      <Sequence name="ElevenLabs — Download" from={Math.round(17.5 * fps)} premountFor={fps}><Audio src={staticFile('voiceover/download.mp3')} volume={.92} /></Sequence>
    </> : null}
    {music ? <Sequence name="ElevenLabs instrumental music" premountFor={fps}><Audio src={staticFile('music.mp3')} volume={frame=>interpolate(frame,[0,.8*fps,10.2*fps,11.2*fps,12.2*fps,14.8*fps,15.4*fps,17.3*fps,23.3*fps,24*fps],[0,.16,.16,.25,.16,.16,.25,.16,.16,0],{extrapolateLeft:'clamp',extrapolateRight:'clamp'})} /></Sequence> : null}
  </Frame>;
};

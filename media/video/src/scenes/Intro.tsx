import {useCurrentFrame, useVideoConfig, interpolate} from 'remotion';
import {clamp, Frame, Keycap, palette, Rise, Scene} from '../design';

export const Intro=()=>{
 const frame=useCurrentFrame();const {fps}=useVideoConfig();
 return <Frame><Scene seconds={5}>
  <Rise delay={.1*fps} style={{position:'absolute',left:130,top:242,color:palette.cyan,fontSize:26,letterSpacing:4,fontWeight:600}}>WINDOWS UÇUN NATIVE KLAVIATURA</Rise>
  <Rise delay={.25*fps} style={{position:'absolute',left:125,top:320,fontSize:112,lineHeight:1.08,fontWeight:650,letterSpacing:-5}}>Yangi alifbo.<br/><span style={{color:palette.blue}}>Taniş tugmalar.</span></Rise>
  <Rise delay={.6*fps} style={{position:'absolute',left:132,top:635,fontSize:32,lineHeight:1.5,color:palette.muted}}>Özbekça yozing.<br/>Öz klaviaturangizda.</Rise>
  <div style={{position:'absolute',right:155,top:278,display:'grid',gridTemplateColumns:'repeat(2, 210px)',gap:36,rotate:'-5deg',scale:interpolate(frame,[0,3*fps],[.93,1],clamp)}}>
   <Rise delay={.3*fps}><Keycap active>Ö</Keycap></Rise>
   <Rise delay={.45*fps}><Keycap>Ğ</Keycap></Rise>
   <Rise delay={.6*fps}><Keycap>Ş</Keycap></Rise>
   <Rise delay={.75*fps}><Keycap active>Ç</Keycap></Rise>
  </div>
  <Rise delay={.9*fps} style={{position:'absolute',bottom:115,left:132,fontSize:24,color:palette.muted,letterSpacing:2}}>OÇIQ KOD · MIT LITSENZIYA</Rise>
 </Scene></Frame>
};

import type {CSSProperties, ReactNode} from 'react';
import {AbsoluteFill, Easing, interpolate, useCurrentFrame, useVideoConfig} from 'remotion';

export const palette = {bg:'#07101f', text:'#eef5ff', muted:'#9cabbe', blue:'#70bfff', cyan:'#65ead9', line:'#203752'};
export const family = 'Segoe UI, Arial, sans-serif';
export const ease = Easing.bezier(0.16, 1, 0.3, 1);
export const clamp = {extrapolateLeft:'clamp' as const, extrapolateRight:'clamp' as const};

export const Frame = ({children,progress=false}: {children: ReactNode;progress?:boolean}) => {
  const frame=useCurrentFrame(); const {durationInFrames}=useVideoConfig();
  return <AbsoluteFill style={{background:palette.bg, color:palette.text, fontFamily:family, overflow:'hidden'}}>
    <AbsoluteFill style={{background:'radial-gradient(ellipse at 76% 25%, #12315a 0%, transparent 58%), radial-gradient(ellipse at 20% 100%, #0a3c43 0%, transparent 55%)'}} />
    <AbsoluteFill style={{opacity:0.13, backgroundImage:'linear-gradient(#7fa2c522 1px, transparent 1px), linear-gradient(90deg, #7fa2c522 1px, transparent 1px)', backgroundSize:'80px 80px', maskImage:'linear-gradient(black, transparent 90%)'}} />
    <div style={{position:'absolute',top:58,left:100,display:'flex',alignItems:'center',gap:18,fontSize:30,fontWeight:650,letterSpacing:-0.7}}><Logo size={44} />UzbekKeys</div>
    <div style={{position:'absolute',top:66,right:100,color:palette.muted,fontSize:22,letterSpacing:4}}>ÖZBEK LOTIN · 2026</div>
    {children}
    {progress ? <div style={{position:'absolute',bottom:0,left:0,height:4,width:`${interpolate(frame,[0,durationInFrames-1],[0,100],clamp)}%`,background:`linear-gradient(90deg, ${palette.blue}, ${palette.cyan})`}} /> : null}
  </AbsoluteFill>;
};

export const Logo = ({size=64}: {size?:number}) => <div style={{width:size,height:size,borderRadius:size*.25,background:'linear-gradient(140deg,#57a9ff,#32b4ab)',display:'flex',alignItems:'center',justifyContent:'center',boxShadow:'0 0 45px #4ca8ff22',color:'#061627',fontSize:size*.65,fontWeight:800}}>Ö</div>;

export const Scene = ({children, seconds}: {children:ReactNode; seconds:number}) => {
  const frame=useCurrentFrame(); const {fps}=useVideoConfig();
  return <AbsoluteFill style={{opacity:interpolate(frame,[0,.42*fps,(seconds-.35)*fps,seconds*fps],[0,1,1,0],clamp)}}>{children}</AbsoluteFill>;
};

export const Rise = ({children,delay=0,style={}}:{children:ReactNode;delay?:number;style?:CSSProperties}) => {
  const frame=useCurrentFrame(); const {fps}=useVideoConfig();
  return <div style={{opacity:interpolate(frame,[delay,delay+.6*fps],[0,1],clamp),translate:interpolate(frame,[delay,delay+.8*fps],['0px 34px','0px 0px'],{...clamp,easing:ease}),...style}}>{children}</div>;
};

export const Keycap = ({children,sub,active=false,style={}}:{children:ReactNode;sub?:ReactNode;active?:boolean;style?:CSSProperties}) => <div style={{width:210,height:200,borderRadius:28,position:'relative',background:active?'linear-gradient(145deg,#1d6093,#123c64)':'linear-gradient(145deg,#192b43,#0d1b2d)',border:`2px solid ${active?'#70bfff99':'#34516b'}`,boxShadow:active?'0 16px 0 #071522, 0 25px 60px #4dbeff22':'0 14px 0 #050b14, 0 22px 45px #0005',display:'flex',alignItems:'center',justifyContent:'center',fontSize:116,fontWeight:550,color:active?'#dcf4ff':palette.text,...style}}>{children}{sub ? <div style={{position:'absolute',right:20,bottom:14,fontSize:25,color:palette.muted,fontWeight:400}}>{sub}</div> : null}</div>;

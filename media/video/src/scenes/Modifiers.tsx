import {useCurrentFrame,useVideoConfig} from 'remotion';
import {Frame,Keycap,palette,Rise,Scene} from '../design';

export const Modifiers=()=>{
 const frame=useCurrentFrame();const {fps}=useVideoConfig();const shifted=frame>=3.1*fps;
 return <Frame><Scene seconds={5.4}>
  <Rise style={{position:'absolute',left:130,top:195,fontSize:76,fontWeight:650,letterSpacing:-2}}>Belgilar ham <span style={{color:palette.cyan}}>qölingiz ostida.</span></Rise>
  <Rise delay={.2*fps} style={{position:'absolute',left:133,top:310,fontSize:32,color:palette.muted}}>Öng Alt bilan asl belgilarni yozing.</Rise>
  <div style={{position:'absolute',left:140,top:470,display:'flex',alignItems:'center',gap:32}}>
   <Rise delay={.3*fps}><Keycap active style={{width:300,fontSize:48,height:175}}>{shifted?'AltGr + Shift':'Öng Alt'}</Keycap></Rise>
   <span style={{fontSize:50,color:palette.muted}}>+</span>
   <Rise delay={.45*fps}><Keycap style={{width:240,fontSize:40,height:175}}>[ ] - =</Keycap></Rise>
   <span style={{fontSize:50,color:palette.muted}}>→</span>
   <Rise delay={.6*fps}><div style={{fontFamily:'Consolas, monospace',fontSize:96,letterSpacing:19,color:palette.cyan}}>{shifted?'{ } _ +':'[ ] - ='}</div></Rise>
  </div>
  <Rise delay={.9*fps} style={{position:'absolute',left:145,right:145,bottom:170,paddingTop:35,borderTop:`1px solid ${palette.line}`,display:'flex',alignItems:'center',justifyContent:'space-between'}}><span style={{fontSize:31,color:palette.muted}}>Öng Alt + apostrof</span><span style={{fontSize:52,color:palette.text}}>maʼno <span style={{color:palette.cyan,marginLeft:34}}>ʼ</span> <span style={{fontSize:24,color:palette.muted,marginLeft:14}}>U+02BC</span></span></Rise>
 </Scene></Frame>
};

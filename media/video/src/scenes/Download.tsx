import {useVideoConfig} from 'remotion';
import {Frame,Keycap,Logo,palette,Rise,Scene} from '../design';

export const Download=()=>{
 const {fps}=useVideoConfig();
 return <Frame><Scene seconds={6.6}>
  <Rise style={{position:'absolute',left:0,right:0,top:185,display:'flex',alignItems:'center',justifyContent:'center',gap:28}}><Logo size={92}/><span style={{fontSize:92,letterSpacing:-4,fontWeight:650}}>UzbekKeys</span></Rise>
  <Rise delay={.15*fps} style={{position:'absolute',left:0,right:0,top:340,textAlign:'center',fontSize:39,color:palette.muted}}>Windows klaviaturalari orasidan tanlang.</Rise>
  <Rise delay={.35*fps} style={{position:'absolute',left:0,right:0,top:445,display:'flex',alignItems:'center',justifyContent:'center',gap:30}}><Keycap active style={{width:170,height:115,fontSize:46,borderRadius:20}}>Win</Keycap><span style={{fontSize:44,color:palette.muted}}>+</span><Keycap active style={{width:290,height:115,fontSize:43,borderRadius:20}}>Space</Keycap><span style={{fontSize:44,color:palette.muted}}>→</span><span style={{fontSize:44,color:palette.cyan}}>UzbekKeys</span></Rise>
  <Rise delay={.75*fps} style={{position:'absolute',left:0,right:0,top:660,textAlign:'center',fontSize:52,fontWeight:600}}>Bepul. Oçiq kodli. <span style={{color:palette.blue}}>Siz uçun.</span></Rise>
  <Rise delay={1*fps} style={{position:'absolute',left:0,right:0,top:780,textAlign:'center',fontSize:34,color:palette.cyan,fontFamily:'Consolas, monospace'}}>github.com/Shohruh04/UzbekKeys</Rise>
  <Rise delay={1.15*fps} style={{position:'absolute',left:0,right:0,bottom:98,textAlign:'center',fontSize:23,color:palette.muted,letterSpacing:2}}>YUKLAB OLING VA SINAB KÖRING</Rise>
 </Scene></Frame>
};

import {useCurrentFrame,useVideoConfig} from 'remotion';
import {Frame,Keycap,palette,Rise,Scene} from '../design';

export const Letters=()=>{
 const frame=useCurrentFrame();const {fps}=useVideoConfig();const upper=frame>=3.65*fps;
 return <Frame><Scene seconds={7}>
  <Rise style={{position:'absolute',left:130,top:195,fontSize:72,letterSpacing:-2,fontWeight:650}}>Tört harf. <span style={{color:palette.blue}}>Bitta bosiş.</span></Rise>
  <Rise delay={.2*fps} style={{position:'absolute',left:133,top:300,fontSize:30,color:palette.muted}}>Inglizça QWERTY harflari öz joyida qoladi.</Rise>
  <div style={{position:'absolute',left:210,right:210,top:420,display:'flex',justifyContent:'space-between'}}>
   {[['[','ç','Ç'],[']','ö','Ö'],['-','ğ','Ğ'],['=','ş','Ş']].map(([original,lower,capital],i)=><Rise key={original} delay={(.25+i*.12)*fps} style={{textAlign:'center'}}><div style={{fontSize:25,color:palette.muted,marginBottom:28,fontFamily:'Consolas, monospace'}}>{original} tugmasi</div><Keycap active>{upper?capital:lower}</Keycap></Rise>)}
  </div>
  <Rise delay={1.1*fps} style={{position:'absolute',left:0,right:0,top:805,display:'flex',justifyContent:'center',alignItems:'center',gap:25}}><div style={{padding:'14px 28px',background:upper?'#65ead91b':'#15253a',border:`1px solid ${upper?palette.cyan:palette.line}`,borderRadius:14,color:upper?palette.cyan:palette.muted,fontSize:27}}>Shift</div><span style={{fontSize:31,color:palette.muted}}>{upper?'Katta harflar — birga bosing.':'Katta harflar uçun Shift.'}</span></Rise>
 </Scene></Frame>
};

import {existsSync,mkdirSync} from 'node:fs';
import {createRequire} from 'node:module';
import {dirname,join,resolve} from 'node:path';
import {spawnSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const project=resolve(dirname(fileURLToPath(import.meta.url)),'..');
const require=createRequire(import.meta.url);
const cli=join(dirname(require.resolve('@remotion/cli/package.json')),'remotion-cli.js');
const temporary=join(project,'out','tmp');
mkdirSync(temporary,{recursive:true});
const poster=process.argv.includes('--poster');
const voiced=process.argv.includes('--voice');
if(voiced&&!existsSync(join(project,'public','narration.mp3'))){
  console.error('Missing public/narration.mp3; restore the included ElevenLabs asset first.');
  process.exit(1);
}
const args=poster?['still','UzbekKeys','out/UzbekKeys-poster.png','--frame=70']:
  ['render','UzbekKeys',voiced?'out/UzbekKeys-promo-voiced.mp4':'out/UzbekKeys-promo.mp4','--codec=h264','--crf=18','--pixel-format=yuv420p','--concurrency=4',...(voiced?['--props={"narration":true}']:[])];
args.push(...process.argv.slice(2).filter(argument=>argument!=='--poster'&&argument!=='--voice'));
const child=spawnSync(process.execPath,[cli,...args],{cwd:project,stdio:'inherit',env:{...process.env,TEMP:temporary,TMP:temporary,TMPDIR:temporary}});
if(child.error)throw child.error;
process.exit(child.status??1);

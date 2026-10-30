import {readFile, writeFile} from 'node:fs/promises';

// The published diagram reads its glyphs from the canonical KLC source.
const klc = await readFile(new URL('../../src/uzbkeys.klc', import.meta.url), 'utf8');
const rows = new Map();
let inLayout = false;
for (const raw of klc.split(/\r?\n/)) {
  const line = raw.split('//')[0].trim();
  if (line === 'LAYOUT') { inLayout = true; continue; }
  if (inLayout && line === 'KEYNAME') break;
  if (!inLayout || !/^[\da-f]{2}\s/i.test(line)) continue;
  const [scan, vk, caps, ...states] = line.split(/\s+/);
  rows.set(scan.toLowerCase(), {vk, caps, chars: states.map(x => x === '-1' ? '' : String.fromCodePoint(parseInt(x, 16)))});
}

const esc = text => String(text).replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const changed = new Set(['0c','0d','1a','1b']);
const keyboard = [
  [['29',1],['02',1],['03',1],['04',1],['05',1],['06',1],['07',1],['08',1],['09',1],['0a',1],['0b',1],['0c',1],['0d',1],['Backspace',2]],
  [['Tab',1.5],['10',1],['11',1],['12',1],['13',1],['14',1],['15',1],['16',1],['17',1],['18',1],['19',1],['1a',1],['1b',1],['2b',1.5]],
  [['Caps Lock',1.75],['1e',1],['1f',1],['20',1],['21',1],['22',1],['23',1],['24',1],['25',1],['26',1],['27',1],['28',1],['Enter',2.25]],
  [['Shift',2.25],['2c',1],['2d',1],['2e',1],['2f',1],['30',1],['31',1],['32',1],['33',1],['34',1],['35',1],['Shift',2.75]],
  [['Ctrl',1.25],['Win',1.25],['Alt',1.25],['Space',6.25],['AltGr',1.25],['Win',1.25],['Menu',1.25],['Ctrl',1.25]],
];
const svg = [`<svg xmlns="http://www.w3.org/2000/svg" width="1480" height="740" viewBox="0 0 1480 740" role="img" aria-labelledby="title description">`,
`<title id="title">UzbekKeys — klaviatura joylaşuvi</title><desc id="description">Yangi harflar: ç — [, ö — ], ğ — minus, ş — tenglik tugmasida. Oddiy belgilar AltGr orqali. Tutuq: AltGr va apostrof.</desc>`,
`<defs><linearGradient id="bg" x2="1" y2="1"><stop stop-color="#081221"/><stop offset="1" stop-color="#101f33"/></linearGradient><linearGradient id="new" x2="0" y2="1"><stop stop-color="#16465a"/><stop offset="1" stop-color="#113747"/></linearGradient></defs>`,
`<rect width="1480" height="740" rx="28" fill="url(#bg)"/>`,
`<g font-family="Segoe UI,Arial,sans-serif"><text x="54" y="66" fill="#f3f8ff" font-size="32" font-weight="700">UzbekKeys</text><text x="54" y="104" fill="#a4b9cf" font-size="20">Yangi özbek lotin alifbosi · US QWERTY asosida</text>`,
`<rect x="1198" y="38" width="228" height="47" rx="23" fill="#13394a" stroke="#277a90"/><text x="1312" y="69" text-anchor="middle" fill="#82e7ee" font-size="21" font-weight="600">Ö Ğ Ş Ç</text>`];
const unit=91.2, gap=8, keyHeight=76;
for (let r=0;r<keyboard.length;r++) {
  let x=54; const y=144+r*86;
  for (const [id,units] of keyboard[r]) {
    const width=unit*units-gap;
    const data=rows.get(id), isNew=changed.has(id), isQuote=id==='28', isAlt=id==='AltGr';
    const fill=isNew?'url(#new)':isAlt?'#12333e':'#17263a';
    const stroke=isNew?'#39c4d3':isQuote?'#7889af':'#33465c';
    svg.push(`<rect x="${x}" y="${y+3}" width="${width}" height="${keyHeight}" rx="10" fill="#050d19"/><rect x="${x}" y="${y}" width="${width}" height="${keyHeight-2}" rx="10" fill="${fill}" stroke="${stroke}" stroke-width="${isNew?1.5:1}"/>`);
    if (data) {
      const [base,shift,,,alt,shiftAlt]=data.chars;
      svg.push(`<text x="${x+15}" y="${y+27}" font-size="18" fill="${isNew?'#c5faff':'#9db2c9'}">${esc(shift)}</text><text x="${x+15}" y="${y+60}" font-size="28" font-weight="${isNew?650:500}" fill="${isNew?'#e6fdff':'#ecf3fc'}">${esc(base)}</text>`);
      if(alt || shiftAlt) svg.push(`<text x="${x+width-12}" y="${y+27}" font-size="18" text-anchor="end" fill="#67d6e8">${esc(shiftAlt)}</text><text x="${x+width-12}" y="${y+60}" font-size="21" text-anchor="end" fill="#67d6e8">${esc(alt)}</text>`);
    } else {
      const label=id==='Space'?'':id;
      svg.push(`<text x="${x+width/2}" y="${y+46}" text-anchor="middle" font-size="${label.length>7?16:18}" font-weight="${isAlt?650:500}" fill="${isAlt?'#8ceaf1':'#acbed1'}">${esc(label)}</text>`);
    }
    x+=unit*units;
  }
}
svg.push(`<line x1="54" x2="1426" y1="607" y2="607" stroke="#2a3e54"/>`,
`<rect x="54" y="629" width="140" height="76" rx="10" fill="#164154" stroke="#39c4d3"/><text x="68" y="654" fill="#a9c1d7" font-size="16">Shift</text><text x="68" y="689" fill="#f0faff" font-size="18">oddiy</text><text x="181" y="654" text-anchor="end" fill="#67d6e8" font-size="13">⇧AltGr</text><text x="181" y="689" text-anchor="end" fill="#67d6e8" font-size="14">AltGr</text>`,
`<text x="219" y="656" fill="#d7e4f3" font-size="19">AltGr = öng Alt</text><text x="219" y="690" fill="#9ab2c9" font-size="18">Shift va Caps Lock — katta harflar</text>`,
`<text x="680" y="656" fill="#d7e4f3" font-size="19">AltGr + ' → ʼ</text><text x="680" y="690" fill="#9ab2c9" font-size="18">Tutuq belgisi · U+02BC</text>`,
`<text x="1426" y="656" text-anchor="end" fill="#70dce9" font-size="19">[ ç   ] ö   - ğ   = ş</text><text x="1426" y="690" text-anchor="end" fill="#9ab2c9" font-size="18">Yangi harflar — oddiy bosişda</text></g></svg>`);
await writeFile(new URL('keyboard-layout.svg',import.meta.url),svg.join('\n'));
console.log('Generated docs/assets/keyboard-layout.svg from src/uzbkeys.klc');

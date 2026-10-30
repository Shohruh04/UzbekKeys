# UzbekKeys — video manbasi

24 soniyalik taniştiruv: 1920 × 1080, 30 fps, H.264 MP4. ElevenLabs özbekça ovozi va sokin instrumental musiqa bilan. Ekrandagi matn yangi özbek alifbosida yozilgan. Tört sahna Remotion Studioʼda alohida tahrirlanadi.

## Qayta yiğiş

Node.js 20.19+, 22.13+ yoki 24+ kerak.

```console
npm ci
npm run dev
npm run lint
npm run render
npm run poster
```

Natijalar `out/UzbekKeys-promo.mp4` va `out/UzbekKeys-poster.png` fayllariga yoziladi. Render skripti vaqtinçalik fayllarni loyiha içida saqlaydi. Bu Windowsʼdagi ayrim çeklangan TEMP ruxsatlari bilan ham yiğişni osonlaştiradi.

## ElevenLabs ovozi

Asilbek özbekça ovozi `eleven_v4` bilan yaratilgan. Tayyor ovoz ElevenLabs Scribe orqali qayta matnga aylantirilib, töliq ssenariy bilan mosligi tekşirilgan. Tabiiy tezlik va ohang saqlangan; gaplar tört sahnaga moslab joylaştirilgan.

Matn `narration.txt`, model sozlamalari `voiceover.json` faylida. Töliq ovoz `public/narration.mp3`, sahna qismlari `public/voiceover/` içida. Oddiy render ham ovozli; alohida nom bilan eksport uçun:

```console
npm run render:voiced
```

API kalitlari va vaqtinçalik yuklab oliş URLʼlari manbaga kiritilmaydi.

## Musiqa

`eleven_music_v2_5` bilan yaratilgan 24 soniyalik instrumental fon `public/music.mp3` faylida. Sokin sintezatorlar va yengil arpejjio nutq ostida past ovozda çalinadi. Fon musiqa gaplar orasida biroz kötari­ladi, boşida va oxirida yumşoq sönadi. Prompt va qayta işlaş tafsilotlari `music.json` içida.

## Boğliqliklar

Remotion paketlari bir xil `4.0.539` versiyasiga mahkamlangan. `zod` uçun `4.6.7` override xavfsizlik tuzatişini oladi; Remotionʼning qatʼiy versiya tekşiruvi bu farq haqida xabar beradi. TypeScript, ESLint va render bilan moslik tekşirilgan. `npm audit` natijasi: 0 zaiflik.

Loyihadagi özimiz yozgan kod MIT litsenziyasida. Remotion va boşqa boğliqliklar öz litsenziyalarini saqlaydi.

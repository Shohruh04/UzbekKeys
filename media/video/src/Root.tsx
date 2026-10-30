import {Composition, Folder} from 'remotion';
import {UzbekKeysPromo} from './Composition';
import {Intro} from './scenes/Intro';
import {Letters} from './scenes/Letters';
import {Modifiers} from './scenes/Modifiers';
import {Download} from './scenes/Download';

export const RemotionRoot: React.FC = () => {
  return (
    <>
      <Composition id="UzbekKeys" component={UzbekKeysPromo} durationInFrames={720} fps={30} width={1920} height={1080} defaultProps={{narration: true,music:true}} />
      <Folder name="Scenes">
        <Composition id="Intro" component={Intro} durationInFrames={150} fps={30} width={1920} height={1080} />
        <Composition id="Letters" component={Letters} durationInFrames={210} fps={30} width={1920} height={1080} />
        <Composition id="Modifiers" component={Modifiers} durationInFrames={162} fps={30} width={1920} height={1080} />
        <Composition id="Download" component={Download} durationInFrames={198} fps={30} width={1920} height={1080} />
      </Folder>
    </>
  );
};

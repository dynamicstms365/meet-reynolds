const { ReynoldsSoraGenerator } = require('./SORA_VIDEO_GENERATOR');
const { REYNOLDS_HYPER_DETAILED_SCENES } = require('./REYNOLDS_HYPER_DETAILED_PROMPTS');

class ReynoldsLiteraryGenerator extends ReynoldsSoraGenerator {
  constructor() {
    super();
    this.outputDir = "./literary_videos";
    
    // Ensure output directory exists
    const fs = require("fs");
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
  }

  // Test single hyper-detailed scene
  async testLiteraryScene(sceneId = "V1_S1_Fourth_Wall_Opening_LITERARY", aspectRatio = "16:9") {
    console.log(`🎭📚 Testing Literary Masterpiece Scene: ${sceneId}`);
    
    const scene = REYNOLDS_HYPER_DETAILED_SCENES.find(s => s.id === sceneId);
    if (!scene) {
      throw new Error(`Literary scene ${sceneId} not found!`);
    }

    console.log(`📖 Prompt Length: ${scene.prompt.length} characters`);
    console.log(`🎬 Expected Duration: ${scene.duration} seconds`);
    console.log(`📐 Aspect Ratio: ${aspectRatio}`);
    
    return await this.generateVideo(
      scene.prompt,
      scene.duration,
      aspectRatio,
      scene.id
    );
  }

  // Generate all literary scenes
  async generateLiteraryMasterpiece(aspectRatio = "16:9") {
    console.log(`
🎭📚 REYNOLDS LITERARY MASTERPIECE GENERATION
═══════════════════════════════════════════════════════
    HYPER-DETAILED VISUAL STORYTELLING
    MAXIMUM EFFORT™ APPLIED TO EVERY PIXEL
═══════════════════════════════════════════════════════
    `);

    console.log(`📖 Literary Scenes: ${REYNOLDS_HYPER_DETAILED_SCENES.length}`);
    console.log(`📐 Aspect Ratio: ${aspectRatio}`);
    console.log(`🎯 Target: Perfect Reynolds Character Consistency`);

    const generationPromises = REYNOLDS_HYPER_DETAILED_SCENES.map(scene => 
      this.generateVideo(scene.prompt, scene.duration, aspectRatio, scene.id)
    );

    try {
      const results = await Promise.all(generationPromises);
      console.log(`🏆 LITERARY MASTERPIECE COMPLETE!`);
      return results;
    } catch (error) {
      console.error(`💥 Literary generation error:`, error.message);
      throw error;
    }
  }
}

// CLI execution
if (require.main === module) {
  const generator = new ReynoldsLiteraryGenerator();
  
  const command = process.argv[2];
  const sceneId = process.argv[3];
  const aspectRatio = process.argv[4] || "16:9";
  
  switch(command) {
    case 'test':
      generator.testLiteraryScene(sceneId, aspectRatio).catch(console.error);
      break;
      
    case 'generate':
      generator.generateLiteraryMasterpiece(aspectRatio).catch(console.error);
      break;
      
    default:
      console.log(`
🎭📚 Reynolds Literary Generator Commands:

Test single scene:
node REYNOLDS_LITERARY_GENERATOR.js test V1_S1_Fourth_Wall_Opening_LITERARY 16:9

Generate all scenes:
node REYNOLDS_LITERARY_GENERATOR.js generate 16:9

Available Scenes:
${REYNOLDS_HYPER_DETAILED_SCENES.map(s => `- ${s.id}`).join('\n')}
      `);
      break;
  }
}

module.exports = { ReynoldsLiteraryGenerator };
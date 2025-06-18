const { ReynoldsSoraGenerator, VIDEO_1_SCENES } = require('./SORA_VIDEO_GENERATOR');
const { ReynoldsAudioGenerator, REYNOLDS_AUDIO_SCRIPTS } = require('./REYNOLDS_AUDIO_GENERATOR');
const fs = require('fs');
const path = require('path');

class ReynoldsMasterOrchestrator {
  constructor() {
    this.videoGenerator = new ReynoldsSoraGenerator();
    this.audioGenerator = new ReynoldsAudioGenerator();
    this.outputDir = "./reynolds_production";
    
    // Ensure output directory exists
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
  }

  // Generate complete video series with audio in parallel
  async orchestrateMaximumEffort() {
    console.log(`
🎭✨ REYNOLDS MASTER ORCHESTRATOR ACTIVATED! ✨🎭
═══════════════════════════════════════════════════════
    MAXIMUM EFFORT™ APPLIED TO EVERY PROCESS
═══════════════════════════════════════════════════════
    `);

    const startTime = Date.now();
    
    try {
      // Phase 1: Generate all audio tracks in parallel (fastest)
      console.log(`\n🎙️ PHASE 1: Audio Generation (Parallel Execution)`);
      const audioPromise = this.audioGenerator.generateAllAudio(REYNOLDS_AUDIO_SCRIPTS);

      // Phase 2: Generate videos for all aspect ratios in parallel
      console.log(`\n🎬 PHASE 2: Video Generation (Parallel Execution)`);
      const aspectRatios = ["16:9", "1:1", "9:16"];
      const videoPromises = aspectRatios.map(ratio => 
        this.videoGenerator.generateVideoScenes(VIDEO_1_SCENES, ratio)
      );

      // Execute both phases simultaneously for MAXIMUM EFFICIENCY
      console.log(`\n⚡ EXECUTING ALL GENERATION TASKS IN PARALLEL...`);
      const [audioResults, ...videoResults] = await Promise.all([
        audioPromise,
        ...videoPromises
      ]);

      const endTime = Date.now();
      const totalTime = (endTime - startTime) / 1000;

      // Success report
      console.log(`
🏆 REYNOLDS ORCHESTRATION COMPLETE! 🏆
═══════════════════════════════════════════════════════
✅ Audio Tracks Generated: ${audioResults.length}
✅ Video Formats Generated: ${aspectRatios.length}
✅ Total Scenes per Format: ${VIDEO_1_SCENES.length}
✅ Total Video Files: ${aspectRatios.length * VIDEO_1_SCENES.length}
⏱️ Total Generation Time: ${totalTime.toFixed(2)} seconds
🎭 Maximum Effort™ Applied: SUCCESSFULLY
═══════════════════════════════════════════════════════
      `);

      // Generate production summary
      await this.generateProductionSummary(audioResults, videoResults, aspectRatios);

      return {
        success: true,
        audioFiles: audioResults,
        videoFiles: videoResults,
        aspectRatios: aspectRatios,
        totalTime: totalTime
      };

    } catch (error) {
      console.error(`
💥 ORCHESTRATION ERROR DETECTED! 💥
═══════════════════════════════════════════════════════
❌ Error: ${error.message}
🔄 Recommendation: Check API keys and endpoint configuration
🎭 Reynolds Status: Supernatural debugging engaged
═══════════════════════════════════════════════════════
      `);
      throw error;
    }
  }

  // Generate individual scene for testing
  async generateSingleScene(sceneId, aspectRatio = "16:9") {
    const scene = VIDEO_1_SCENES.find(s => s.id === sceneId);
    if (!scene) {
      throw new Error(`Scene ${sceneId} not found!`);
    }

    console.log(`🎬 Generating single scene: ${sceneId} (${aspectRatio})`);
    const videoResult = await this.videoGenerator.generateVideo(
      scene.prompt, 
      scene.duration, 
      aspectRatio, 
      scene.id
    );

    const audioScript = REYNOLDS_AUDIO_SCRIPTS.find(a => a.scene === sceneId.replace(/_.*/, ''));
    let audioResult = null;
    if (audioScript) {
      console.log(`🎙️ Generating audio for: ${sceneId}`);
      audioResult = await this.audioGenerator.generateAudio(
        audioScript.text,
        audioScript.voice,
        audioScript.id
      );
    }

    return { video: videoResult, audio: audioResult };
  }

  // Generate production summary report
  async generateProductionSummary(audioResults, videoResults, aspectRatios) {
    const summary = `
# REYNOLDS PRODUCTION SUMMARY
## Maximum Effort™ Generation Report

### Generated Assets:
- **Audio Tracks:** ${audioResults.length} files
- **Video Formats:** ${aspectRatios.join(', ')}
- **Total Video Files:** ${videoResults.flat().length}
- **Generation Date:** ${new Date().toISOString()}

### File Structure:
\`\`\`
reynolds_production/
├── generated_audio/
${audioResults.map(file => `│   ├── ${path.basename(file)}`).join('\n')}
├── generated_videos/
${videoResults.flat().map(file => `│   ├── ${path.basename(file)}`).join('\n')}
\`\`\`

### Next Steps:
1. **Video Editing:** Combine scenes into complete videos
2. **Audio Sync:** Sync audio tracks with corresponding videos
3. **Post-Production:** Add background music and sound effects
4. **Distribution:** Export for different platforms

### Reynolds Notes:
🎭 All generation completed with Maximum Effort™
⚡ Parallel processing achieved supernatural efficiency
🎬 Ready for post-production orchestration
`;

    const summaryPath = path.join(this.outputDir, 'PRODUCTION_SUMMARY.md');
    fs.writeFileSync(summaryPath, summary);
    console.log(`📋 Production summary saved: ${summaryPath}`);
  }

  // Test API connectivity
  async testConnectivity() {
    console.log(`🔧 Testing Reynolds API Connectivity...`);
    
    try {
      // Test with minimal prompt
      const testScene = {
        id: "connectivity_test",
        duration: 5,
        prompt: "Reynolds in red and black suit gives thumbs up to camera. Test successful."
      };

      await this.videoGenerator.generateVideo(
        testScene.prompt,
        testScene.duration,
        "16:9",
        testScene.id
      );

      console.log(`✅ SORA API connectivity: SUCCESSFUL`);

      // Test audio
      await this.audioGenerator.generateAudio(
        "Reynolds connectivity test successful with Maximum Effort!",
        "onyx",
        "audio_connectivity_test"
      );

      console.log(`✅ TTS API connectivity: SUCCESSFUL`);
      console.log(`🎭 All systems ready for Maximum Effort™ generation!`);

    } catch (error) {
      console.error(`❌ Connectivity test failed:`, error.message);
      throw error;
    }
  }
}

// CLI Commands
if (require.main === module) {
  const orchestrator = new ReynoldsMasterOrchestrator();
  
  const command = process.argv[2];
  
  switch(command) {
    case 'test':
      orchestrator.testConnectivity().catch(console.error);
      break;
      
    case 'scene':
      const sceneId = process.argv[3];
      const aspectRatio = process.argv[4] || "16:9";
      if (!sceneId) {
        console.log("Usage: node REYNOLDS_MASTER_ORCHESTRATOR.js scene <sceneId> [aspectRatio]");
        process.exit(1);
      }
      orchestrator.generateSingleScene(sceneId, aspectRatio).catch(console.error);
      break;
      
    case 'generate':
    default:
      orchestrator.orchestrateMaximumEffort().catch(console.error);
      break;
  }
}

module.exports = { ReynoldsMasterOrchestrator };
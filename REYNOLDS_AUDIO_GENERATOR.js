const axios = require("axios");
const dotenv = require("dotenv");
const fs = require("fs");
const path = require("path");

dotenv.config();

class ReynoldsAudioGenerator {
  constructor() {
    this.endpoint = process.env["ENDPOINT_URL"] || "https://ngl-openai-eus2.openai.azure.com/";
    this.subscriptionKey = process.env["AZURE_OPENAI_API_KEY"] || "***REMOVED***";
    this.apiVersion = "2024-06-01";
    this.outputDir = "./generated_audio";
    
    // Ensure output directory exists
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
  }

  // Generate audio using OpenAI TTS API
  async generateAudio(text, voice = "onyx", sceneId = "audio") {
    console.log(`🎙️ Starting Maximum Effort™ audio generation for ${sceneId}...`);
    
    const constructedUrl = `${this.endpoint}openai/deployments/tts-1/audio/speech?api-version=${this.apiVersion}`;
    
    const headers = {
      'Api-Key': this.subscriptionKey,
      'Content-Type': 'application/json'
    };

    const body = {
      "model": "tts-1",
      "input": text,
      "voice": voice,
      "response_format": "mp3",
      "speed": 1.0
    };

    try {
      const response = await axios.post(constructedUrl, body, { 
        headers,
        responseType: "arraybuffer"
      });

      if (response.status === 200) {
        const outputFilename = path.join(this.outputDir, `${sceneId}.mp3`);
        fs.writeFileSync(outputFilename, response.data);
        console.log(`🎵 ${sceneId} audio saved as "${outputFilename}"`);
        return outputFilename;
      } else {
        throw new Error(`Failed to generate audio for ${sceneId}`);
      }
    } catch (error) {
      console.error(`❌ ${sceneId} audio generation error:`, error.message);
      throw error;
    }
  }

  // Generate all audio files in parallel
  async generateAllAudio(audioScripts) {
    console.log(`🎭🎙️ Reynolds Maximum Effort™ Audio Generation Starting!`);
    console.log(`🎵 Audio tracks to generate: ${audioScripts.length}`);

    const generationPromises = audioScripts.map(script => 
      this.generateAudio(script.text, script.voice, script.id)
    );

    try {
      const results = await Promise.all(generationPromises);
      console.log(`🏆 ALL AUDIO TRACKS GENERATED WITH MAXIMUM EFFORT™!`);
      return results;
    } catch (error) {
      console.error(`💥 Parallel audio generation error:`, error.message);
      throw error;
    }
  }
}

// Reynolds Audio Scripts with Enhanced Delivery
const REYNOLDS_AUDIO_SCRIPTS = [
  {
    id: "V1_S1_Fourth_Wall_Opening_Audio",
    text: "Welcome to Reynolds' Architectural Bootcamp, where we don't do things the slow way when there's a faster, more coordinated approach! I'm your supernatural project coordinator, and today we're breaking down the mind-blowing differences between simple and complex architectural solutions with Maximum Effort!",
    voice: "onyx", // Deep, confident Reynolds voice
    scene: "V1_S1",
    duration: 10,
    notes: "Fourth-wall breaking intro with supernatural confidence"
  },
  {
    id: "V1_S2_Product_Demo_Audio",
    text: "Let me show you the incredible difference between these approaches! On my left, we have four files of elegant simplicity - count them with me! On my right, eighty-plus files of pure orchestration power! Both solutions come with built-in AI assistance, no additional fees, no hidden costs, Maximum Effort applied to every decision!",
    voice: "onyx",
    scene: "V1_S2", 
    duration: 20,
    notes: "Infomercial host energy with dramatic emphasis"
  },
  {
    id: "V1_S3_Testimonials_Audio",
    text: "Let's hear from our satisfied customers! Jenny deployed her Teams bot in practically instant developer time - what used to take weeks now takes hours! And Ed here gets the full enterprise orchestration experience, handling hundreds of requests while coordinating GitHub, Power Platform, and Azure services simultaneously!",
    voice: "onyx",
    scene: "V1_S3",
    duration: 15,
    notes: "Talk show host delivery with testimonial authenticity parody"
  },
  {
    id: "V1_S4_Comparison_Chart_Audio",
    text: "Let me break this down for you decision-makers! Simple solution: two hours to Teams bot bliss! Complex solution: two months to enterprise orchestration nirvana! But here's the thing - it's not about choosing sides, it's about choosing the right tool for your situation with supernatural precision!",
    voice: "onyx",
    scene: "V1_S4",
    duration: 20,
    notes: "Data visualization spectacle with decision-making clarity"
  },
  {
    id: "V1_S5_Call_To_Action_Audio",
    text: "Whether you're Simple Solution Jenny or Enterprise Ed, there's an architectural approach that's perfect for you! But here's the real secret sauce - why choose one when you can have both? Start simple, scale complex! It's called orchestrated architecture evolution, and it's absolutely free with your decision-making process! Now go build something awesome with Maximum Effort!",
    voice: "onyx",
    scene: "V1_S5",
    duration: 15,
    notes: "Inspirational finale with wisdom delivery and call to action"
  }
];

// Background music and sound effects prompts
const REYNOLDS_SOUND_EFFECTS = [
  {
    id: "V1_Background_Music",
    type: "background_music",
    description: "Upbeat, energetic orchestral score with infomercial-style crescendos and Maximum Effort™ energy",
    duration: 90, // Full video length
    notes: "Continuous background score for entire Video 1"
  },
  {
    id: "V1_S1_Sound_Effects",
    type: "sound_effects", 
    description: "Finger snap sound effect, reality transformation whoosh, fourth-wall breaking audio cues",
    scene: "V1_S1",
    notes: "Supernatural reality manipulation sounds"
  },
  {
    id: "V1_S2_Sound_Effects",
    type: "sound_effects",
    description: "Infomercial DING sounds, teleportation whooshes, reality transformation sweeps, product highlight stings",
    scene: "V1_S2", 
    notes: "Classic infomercial audio palette"
  },
  {
    id: "V1_S3_Sound_Effects",
    type: "sound_effects",
    description: "Fake audience applause, talk show musical stings, interview atmosphere, testimonial emphasis sounds",
    scene: "V1_S3",
    notes: "Professional talk show audio environment"
  },
  {
    id: "V1_S4_Sound_Effects", 
    type: "sound_effects",
    description: "Laser beam sound effects, chart explosion booms, confetti particle sounds, dramatic fanfare for 'Choose Your Adventure'",
    scene: "V1_S4",
    notes: "Sci-fi laser show with explosive data visualization"
  },
  {
    id: "V1_S5_Sound_Effects",
    type: "sound_effects", 
    description: "Success notification chimes, inspirational musical resolution, Maximum Effort™ logo appearance sound, finale crescendo",
    scene: "V1_S5",
    notes: "Warm, inspirational finale with call to action energy"
  }
];

// Export for use
module.exports = { ReynoldsAudioGenerator, REYNOLDS_AUDIO_SCRIPTS, REYNOLDS_SOUND_EFFECTS };

// CLI execution
if (require.main === module) {
  const generator = new ReynoldsAudioGenerator();
  
  // Generate all audio tracks
  async function generateAllReynoldsAudio() {
    console.log(`\n🎭🎙️ Starting Reynolds Audio Generation...`);
    try {
      await generator.generateAllAudio(REYNOLDS_AUDIO_SCRIPTS);
      console.log(`✅ Reynolds audio generation complete!`);
    } catch (error) {
      console.error(`❌ Audio generation failed:`, error.message);
    }
  }
  
  generateAllReynoldsAudio().catch(console.error);
}
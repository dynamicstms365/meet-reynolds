const axios = require("axios");
const dotenv = require("dotenv");
const fs = require("fs");
const path = require("path");

dotenv.config();

class ReynoldsSoraGenerator {
  constructor() {
    this.endpoint = process.env["ENDPOINT_URL"] || "https://ngl-openai-eus2.openai.azure.com/";
    this.deployment = process.env["DEPLOYMENT_NAME"] || "sora";
    this.subscriptionKey = process.env["AZURE_OPENAI_API_KEY"] || "***REMOVED***";
    this.apiVersion = "preview";
    this.outputDir = "./generated_videos";
    
    // Ensure output directory exists
    if (!fs.existsSync(this.outputDir)) {
      fs.mkdirSync(this.outputDir, { recursive: true });
    }
  }

  // Generate video with SORA API
  async generateVideo(prompt, duration, aspectRatio = "16:9", sceneId = "scene") {
    console.log(`🎭 Starting Maximum Effort™ generation for ${sceneId}...`);
    
    const dimensions = this.getAspectRatioDimensions(aspectRatio);
    const constructedUrl = `${this.endpoint}openai/v1/video/generations/jobs?api-version=${this.apiVersion}`;
    
    const headers = {
      'Api-Key': this.subscriptionKey,
      'Content-Type': 'application/json'
    };

    const body = {
      "prompt": prompt,
      "n_variants": "1",
      "n_seconds": duration.toString(),
      "height": dimensions.height.toString(),
      "width": dimensions.width.toString(),
      "model": this.deployment
    };

    try {
      // Submit generation job
      let response = await axios.post(constructedUrl, body, { headers });
      console.log(`✨ ${sceneId} job submitted:`, response.data.id);

      const jobId = response.data.id;
      let status = response.data.status;
      const statusUrl = `${this.endpoint}openai/v1/video/generations/jobs/${jobId}?api-version=${this.apiVersion}`;
      
      // Poll for completion
      console.log(`⏳ Polling ${sceneId} status...`);
      while (status !== "succeeded" && status !== "failed") {
        await new Promise((resolve) => setTimeout(resolve, 5000));
        response = await axios.get(statusUrl, { headers });
        status = response.data.status;
        console.log(`📊 ${sceneId} Status:`, status);
      }

      if (status === "succeeded") {
        const generations = response.data.generations ?? [];
        if (generations.length > 0) {
          console.log(`✅ ${sceneId} generation succeeded!`);
          const generationId = generations[0].id;
          const video_url = `${this.endpoint}openai/v1/video/generations/${generationId}/content/video?api-version=${this.apiVersion}`;
          const videoResponse = await axios.get(video_url, { headers, responseType: "arraybuffer" });
          
          if (videoResponse.status === 200) {
            const outputFilename = path.join(this.outputDir, `${sceneId}_${aspectRatio.replace(':', 'x')}.mp4`);
            fs.writeFileSync(outputFilename, videoResponse.data);
            console.log(`🎬 ${sceneId} saved as "${outputFilename}"`);
            return outputFilename;
          } else {
            throw new Error(`Failed to retrieve ${sceneId} video content`);
          }
        } else {
          throw new Error(`${sceneId} succeeded but no generations returned`);
        }
      } else {
        throw new Error(`${sceneId} generation failed: ${JSON.stringify(response.data, null, 2)}`);
      }
    } catch (error) {
      console.error(`❌ ${sceneId} generation error:`, error.message);
      throw error;
    }
  }

  // Get dimensions for aspect ratios
  getAspectRatioDimensions(aspectRatio) {
    const ratios = {
      "16:9": { width: 854, height: 480 },    // Landscape
      "1:1": { width: 480, height: 480 },     // Square
      "9:16": { width: 480, height: 854 }     // Vertical
    };
    return ratios[aspectRatio] || ratios["16:9"];
  }

  // Generate all scenes for a video in parallel
  async generateVideoScenes(scenes, aspectRatio = "16:9") {
    console.log(`🎭✨ Reynolds Maximum Effort™ Parallel Generation Starting!`);
    console.log(`📐 Aspect Ratio: ${aspectRatio}`);
    console.log(`🎬 Scenes to generate: ${scenes.length}`);

    const generationPromises = scenes.map(scene => 
      this.generateVideo(scene.prompt, scene.duration, aspectRatio, scene.id)
    );

    try {
      const results = await Promise.all(generationPromises);
      console.log(`🏆 ALL SCENES GENERATED WITH MAXIMUM EFFORT™!`);
      return results;
    } catch (error) {
      console.error(`💥 Parallel generation error:`, error.message);
      throw error;
    }
  }
}

// Reynolds Video 1 Scene Definitions
const VIDEO_1_SCENES = [
  {
    id: "V1_S1_Fourth_Wall_Opening",
    duration: 10,
    prompt: `Reynolds, a charismatic character in a red and black tactical suit with a form-fitting mask covering his entire head and large white eye patches, stands in a modern co-working space. He looks directly at the camera with confidence and snaps his fingers dramatically. Text overlay appears: "Welcome to Reynolds' Architectural Bootcamp!" The scene has a fourth-wall breaking energy with Reynolds acknowledging the audience directly. Professional lighting, 4K quality, modern office background with developers working at computers.`
  },
  {
    id: "V1_S2_Product_Demo_Spectacular",
    duration: 20,
    prompt: `Reynolds snaps his fingers and transforms a modern co-working space into a cheesy infomercial set with dramatic studio lighting and colorful backdrops. Two developers (Jenny and Mark) stand at podiums looking confused. Reynolds walks between them like a game show host, gesturing dramatically at holographic displays showing code architectures. Text overlay: "4 FILES vs 80+ FILES!" Infomercial-style arrows and graphics highlight the differences. Reality-bending sparkle effects as Reynolds teleports between positions.`
  },
  {
    id: "V1_S3_Testimonials_Section", 
    duration: 15,
    prompt: `Reynolds creates a talk show set with interview chairs and studio lighting. He holds a microphone like a professional host while Jenny and Mark sit as guests. Fake audience applause sounds play while Reynolds dramatically gestures between them. Text overlays show their "testimonials" in speech bubbles. Talk show graphics and "TESTIMONIALS" banner float in background. Split-screen displays show both architectural solutions running with metrics.`
  },
  {
    id: "V1_S4_Comparison_Chart_Extravaganza",
    duration: 20, 
    prompt: `Reynolds creates a giant floating holographic comparison chart between Jenny and Mark. He wields a laser pointer that shoots actual colorful laser beams at the chart. The 3D chart shows "Reynolds Teams Agent vs CopilotAgent" with spinning graphics and explosion effects. Text shows "2 Hours vs 2 Months", "Laser Focused vs Enterprise Galaxy". The chart explodes into confetti and sparkles with "CHOOSE YOUR ADVENTURE!" banner appearing with fanfare.`
  },
  {
    id: "V1_S5_Call_To_Action_Finale",
    duration: 15,
    prompt: `Reynolds stands in center with one arm around Jenny and Mark in an awkward but endearing group formation. All three face the camera directly with warm, inspirational lighting. Background shows both architectural solutions running harmoniously on floating screens. "MAXIMUM EFFORT™" logo appears with golden sparkle effects. Text overlay: "Start Simple, Scale Complex!" Reynolds winks at camera with white eye patches gleaming. Success metrics and happy user icons float gently around the group.`
  }
];

// Audio generation prompts for each scene
const AUDIO_SCRIPTS = [
  {
    id: "V1_S1_Audio",
    text: "Welcome to Reynolds' Architectural Bootcamp, where we don't do things the slow way when there's a faster, more coordinated approach! I'm your supernatural project coordinator, and today we're breaking down the MIND-BLOWING differences between simple and complex architectural solutions!",
    voice: "onyx", // Deep, confident voice
    scene: "V1_S1"
  },
  {
    id: "V1_S2_Audio", 
    text: "Let me show you the INCREDIBLE difference between these approaches! Four files versus eighty-plus files of pure orchestration power! Both solutions come with built-in AI assistance - no additional fees, no hidden costs! Maximum Effort applied to every architectural decision!",
    voice: "onyx",
    scene: "V1_S2"
  },
  {
    id: "V1_S3_Audio",
    text: "Let's hear from our satisfied customers! Jenny deployed her Teams bot in practically INSTANT developer time! And Ed here gets the FULL enterprise orchestration experience - handling hundreds of requests while coordinating GitHub, Power Platform, AND Azure services!",
    voice: "onyx", 
    scene: "V1_S3"
  },
  {
    id: "V1_S4_Audio",
    text: "Let me break this down for you decision-makers! Simple solution: TWO HOURS to Teams bot bliss! Complex solution: TWO MONTHS to enterprise orchestration NIRVANA! It's not about choosing sides - it's about choosing the RIGHT tool for YOUR situation!",
    voice: "onyx",
    scene: "V1_S4"
  },
  {
    id: "V1_S5_Audio",
    text: "Whether you're Simple Solution Jenny or Enterprise Ed, there's an architectural approach that's PERFECT for you! Here's the real secret: why choose one when you can have BOTH? Start simple, scale complex! Now GO BUILD SOMETHING AWESOME!",
    voice: "onyx",
    scene: "V1_S5"
  }
];

// Export for use
module.exports = { ReynoldsSoraGenerator, VIDEO_1_SCENES, AUDIO_SCRIPTS };

// CLI execution
if (require.main === module) {
  const generator = new ReynoldsSoraGenerator();
  
  // Generate all scenes in parallel for different aspect ratios
  async function generateAllFormats() {
    const aspectRatios = ["16:9", "1:1", "9:16"];
    
    for (const ratio of aspectRatios) {
      console.log(`\n🎭 Starting ${ratio} generation...`);
      try {
        await generator.generateVideoScenes(VIDEO_1_SCENES, ratio);
        console.log(`✅ ${ratio} generation complete!`);
      } catch (error) {
        console.error(`❌ ${ratio} generation failed:`, error.message);
      }
    }
  }
  
  generateAllFormats().catch(console.error);
}
# Reynolds Meme and Status Configuration

This document explains how to configure and use Reynolds' new meme and work status features.

## Configuration Settings

Add these settings to your `appsettings.json` to configure Reynolds' meme functionality:

```json
{
  "Reynolds": {
    "Memes": {
      "EnableRandom": true,
      "MinIntervalHours": 2,
      "MaxIntervalHours": 6,
      "TargetChannels": [
        "19:channel-id-1@thread.v2",
        "19:channel-id-2@thread.v2"
      ],
      "Categories": [
        "general",
        "project-management", 
        "development",
        "teamwork",
        "motivation"
      ]
    }
  }
}
```

## Available Commands

### Meme Commands
- `reynolds meme` - Get a random meme
- `reynolds meme development` - Get a development-related meme
- `reynolds meme motivation` - Get a motivational meme

### Status Commands
- `reynolds status` - Check Reynolds' current work status
- `what are you working on` - Alternative status check
- `reynolds help` - See all available commands

## Meme Categories

Reynolds comes with built-in memes in the following categories:

- **project-management** - Scope creep, project coordination, stakeholder management
- **development** - Coding, debugging, technical challenges
- **teamwork** - Collaboration, coordination, team dynamics  
- **motivation** - Inspirational content, Reynolds' "maximum effort" philosophy
- **personal** - Reynolds' signature name deflection humor

## Background Meme Scheduler

The random meme service runs as a background service that:

1. Checks every minute for meme opportunities
2. Sends memes based on configured intervals (2-6 hours by default)
3. Randomly selects from configured categories
4. Targets specified Teams channels
5. Can be enabled/disabled via configuration

## Work Status Tracking

Reynolds automatically tracks:

- Current task and description
- Repository being worked on
- Progress percentage
- Time spent on current task
- Recent activity history

Status updates are formatted with Reynolds' signature humor and personality.

## Usage Examples

### Getting a Random Meme
User: "reynolds meme"
Reynolds: "🎭 **Reynolds' Premium Meme Service** [shows meme with witty commentary]"

### Checking Status
User: "what are you working on?"
Reynolds: "🎭 **Reynolds is currently:** Organizational Intelligence Monitoring..."

### Category-Specific Memes
User: "reynolds meme project-management"
Reynolds: [Returns a project management themed meme]

## Integration Notes

- Memes are stored in-memory and pre-populated with Reynolds-themed content
- Work status is tracked across service restarts
- Background scheduler respects configuration changes
- All functionality maintains Reynolds' personality and humor
- Integrates seamlessly with existing Teams bot infrastructure
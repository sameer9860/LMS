# LMS - Learning Management System

## Setup Instructions

### 1. OpenAI API Configuration

To enable the auto-quiz feature, you need to set up your OpenAI API key:

#### Option A: Using appsettings.Secrets.json (Recommended for Development)

1. Open `appsettings.Secrets.json` in the project root
2. Replace `your-openai-api-key-here` with your actual OpenAI API key
3. This file is in `.gitignore` and won't be tracked by Git

```json
{
  "OpenAI": {
    "ApiKey": "sk-your-actual-api-key-here"
  }
}
```

#### Option B: Environment Variable (Recommended for Production)

1. Set the environment variable `OPENAI__APIKEY` on your server
2. Or set it in your IDE's launch settings

#### Option C: appsettings.json (Not Recommended)

You can also add it directly to `appsettings.json`, but make sure **never commit sensitive keys** to version control.

### 2. Files That Are NOT Tracked by Git

The following file(s) contain sensitive information and are excluded from Git:

- `appsettings.Secrets.json` - Contains OpenAI API key and other secrets

Before deploying to production, make sure to:
1. Set the `appsettings.Secrets.json` file or environment variables on your server
2. Never commit API keys or secrets to version control

### 3. Auto-Quiz Features

- Upload a PDF or text material
- The system will automatically generate 10 multiple-choice questions using OpenAI
- Questions will have 4 options (A, B, C, D) with correct answer and feedback
- Works exactly like manual quizzes but saves instructor time

### 4. Running the Application

```bash
dotnet run
```

Make sure `appsettings.Secrets.json` exists in the project root before running.

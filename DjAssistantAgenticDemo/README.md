# DJ Assistant – Agentic AI Demo (ASP.NET Core MVC + Semantic Kernel)

This is a minimal .NET 8 ASP.NET Core MVC web app demonstrating **two Semantic Kernel agents**:

- **Gap Filler Agent**: parses/enriches a single user input into SongName / ArtistName / Genre + Reason
- **Suggestion Agent**: generates up to 3 suggestions based on **Completed** records only

## Key behaviors
- Each input creates **one record** (no newline splitting)
- Suggestions are stored in a separate table and are **deleted/replaced** every time the Suggestion Agent runs
- Regeneration is triggered via HTTP POST for a specific record: `/records/{id}/regenerate`

## Configure Azure OpenAI
Set these secrets:

- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`
- `AZURE_OPENAI_CHAT_DEPLOYMENT`

## Run
```bash
cd DjAssistantAgenticDemo
dotnet restore
dotnet run
```
Open https://localhost:5001
Merge Conflict
zdr kp
kolio 123


dancho pishe neshto

# AutoTestMate (Multi-Agent, Clean Architecture, Semantic Kernel)

Agents generate unit tests for pasted C# code using **Semantic Kernel**, run them with `dotnet test`,
and visualize everything live:
- Agents flow
- SK Internals (Prompt â†’ LLM â†’ Stream â†’ Function calling â†’ Result)
- Global Timeline (Orchestrator, Agents, SK Internals, Function calling, Tool calling, Plugins, GraphPublisher)
- Enhanced Tool Calls table (in the full version; here we ship a compact dashboard)

## Prereqs
- .NET 8 SDK
- Set `OPENAI_API_KEY` in your environment (OpenAI-compatible)

## Run
Terminal 1 (Web UI):
  dotnet run --project src/AutoTestMate.Presentation.Web --urls=http://localhost:5173

Terminal 2 (CLI):
  dotnet run --project src/AutoTestMate.Presentation.Cli

Paste a C# method and end with `END`.
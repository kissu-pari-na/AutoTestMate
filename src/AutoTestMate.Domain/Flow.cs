namespace AutoTestMate.Domain;

public enum FlowStage { UserInput, ParseCode, PlanTests, GenerateTests, WriteFiles, RunTests, Summarize }
public sealed record FlowEvent(FlowStage Stage, string Message, DateTimeOffset At, string? Data = null);

namespace BattleTanks_Backend.Application.DTOs.Benchmark;

public class BenchmarkResultDto
{
    public DateTime ExecutedAt { get; set; }
    public List<BenchmarkTestDto> Tests { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class BenchmarkTestDto
{
    public string TestName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double MethodA_TimeMs { get; set; }
    public string MethodA_Name { get; set; } = string.Empty;
    public double MethodB_TimeMs { get; set; }
    public string MethodB_Name { get; set; } = string.Empty;
    public double ImprovementPercentage { get; set; }
    public string Winner { get; set; } = string.Empty;
    public int RecordsProcessed { get; set; }
    public int Iterations { get; set; }
}

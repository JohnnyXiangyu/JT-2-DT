namespace JT_2_DT;

/// <summary>
/// represents an external solver
/// </summary>
public interface ITwSolver
{
    /// <summary>
    /// Execute the solver on the given input.
    /// </summary>
    /// <param name="inputPath">path to a graph file</param>
    /// <param name="outputPath">path to store the output td file</param>
    void Execute(string inputPath, string outputPath);
}

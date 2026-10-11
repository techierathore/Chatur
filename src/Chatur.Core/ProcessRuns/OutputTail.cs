namespace Chatur.Core.ProcessRuns;

/// <summary>Keeps the last lines a command wrote, capped at a number of characters, safe to add to from two threads.</summary>
public sealed class OutputTail
{
    private readonly Queue<string> objLines = new();
    private readonly object objLock = new();
    private readonly int objMaxChars;
    private int objChars;

    /// <summary>Creates a tail that keeps at most <paramref name="aMaxChars"/> characters.</summary>
    /// <param name="aMaxChars">The most characters to keep; the oldest whole lines are dropped first.</param>
    public OutputTail(int aMaxChars)
    {
        objMaxChars = Math.Max(1, aMaxChars);
    }

    /// <summary>Adds one line of output.</summary>
    /// <param name="aLine">The line.</param>
    public void Add(string aLine)
    {
        var vLine = aLine.Length > objMaxChars ? aLine[^objMaxChars..] : aLine;
        lock (objLock)
        {
            objLines.Enqueue(vLine);
            objChars += vLine.Length + 1;
            while (objChars > objMaxChars && objLines.Count > 1)
            {
                objChars -= objLines.Dequeue().Length + 1;
            }
        }
    }

    /// <summary>The kept lines, joined by line feeds.</summary>
    public override string ToString()
    {
        lock (objLock)
        {
            return string.Join('\n', objLines);
        }
    }
}

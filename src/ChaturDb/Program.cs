namespace ChaturDb;

/// <summary>
/// Runs the migrations by hand: <c>dotnet run --project src/ChaturDb -- "Data Source=./chatur.db"</c>.
/// The app itself calls <see cref="ChaturDbMigrator.Migrate"/> directly at startup — this entry point
/// is a convenience for running migrations without starting the whole app.
/// </summary>
public static class Program
{
    /// <summary>
    /// Reads the connection string from the command line and runs the migrations.
    /// </summary>
    /// <param name="aArgs">One argument: the SQLite connection string.</param>
    /// <returns>0 when every migration succeeded; 1 otherwise.</returns>
    public static int Main(string[] aArgs)
    {
        if (aArgs.Length != 1)
        {
            Console.Error.WriteLine("Usage: ChaturDb <connection string>");
            return 1;
        }

        var vResult = ChaturDbMigrator.Migrate(aArgs[0]);
        if (!vResult.Successful)
        {
            Console.Error.WriteLine(vResult.Error);
            return 1;
        }

        return 0;
    }
}

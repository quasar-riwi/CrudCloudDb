namespace CrudCloud.api.Utils;

public class PlanLimits
{
    public static readonly Dictionary<string, int> MaxPerMotor = new()
    {
        { "Gratis", 2 },
        { "Intermedio", 5 },
        { "Avanzado", 10 }
    };

    public static readonly string[] MotoresPermitidos = 
        { "PostgreSQL", "MySQL", "MongoDB", "SQLServer" };
}
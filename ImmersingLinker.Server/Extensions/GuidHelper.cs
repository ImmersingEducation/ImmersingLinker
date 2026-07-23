namespace ImmersingLinker.Server.Extensions;

public static class GuidHelper
{
    public static Guid? ParseGuidFromString(string guidString)
    {
        try
        {
            return Guid.Parse(guidString);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

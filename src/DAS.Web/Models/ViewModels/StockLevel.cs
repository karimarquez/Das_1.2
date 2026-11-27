namespace DAS.Web.Models.ViewModels;

public static class StockLevel
{
    public static string CssClass(decimal stock, decimal minimumStock)
    {
        var percent = minimumStock <= 0
            ? 100
            : Math.Clamp((double)(stock / (minimumStock * 2)) * 100, 4, 100);
        var step = (int)(Math.Round(percent / 5, MidpointRounding.AwayFromZero) * 5);
        return $"stock-w-{step}";
    }
}

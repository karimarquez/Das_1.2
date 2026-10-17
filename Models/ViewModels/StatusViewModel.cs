namespace DAS.Models.ViewModels;

public class StatusViewModel
{
    public StatusViewModel(int statusCode, string title, string message)
    {
        StatusCode = statusCode;
        Title = title;
        Message = message;
    }

    public int StatusCode { get; }
    public string Title { get; }
    public string Message { get; }
}

namespace Retailer.Application.Common.Messaging;

public class MessageRequest
{
    public MessageRequest(string to, string body)
    {
        To = to;
        Body = body;
    }

    public string To { get; }

    public string Body { get; }
}

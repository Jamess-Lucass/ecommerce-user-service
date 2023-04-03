public record ErrorResponse
{
    public ErrorResponse(Error error)
    {
        Errors = new List<Error>() { error };
    }

    public ErrorResponse(long code, string message)
    {
        Errors = new List<Error>() { new Error(code, message) };
    }

    public IEnumerable<Error> Errors { get; set; }
}

public record Error
{
    public Error(long code, string message)
    {
        Code = code;
        Message = message;
    }

    public long Code { get; set; }
    public string Message { get; set; } = "";
}
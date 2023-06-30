namespace Core.Application
{
    public interface IPasswordProvider
    {
        string GetPassword(string prompt);
    }
}

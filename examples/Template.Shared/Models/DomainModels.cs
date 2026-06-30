namespace NesterApp.Models;

public record Post(int Id, int UserId, string Title, string Body)
{
  public static Post Empty => new(0, 0, "", "");
}

public record Comment(int Id, int PostId, string Name, string Email, string Body);

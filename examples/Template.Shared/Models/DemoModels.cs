using Everlong.Nester.Auth;

namespace NesterApp.Models;

public class DemoUser(string username, IEnumerable<string> roles) : IIdentityUser
{
  public string GetIdentity() => username;

  public IEnumerable<string> GetRoles() => roles;

  public IEnumerable<KeyValuePair<string, string>>? GetAdditionalClaims() => null;
}

public record TextMessage(string Content);

public record ImageMessage(string Url, string Caption);

public record AlertMessage(string Title, string Message, AlertSeverity Severity);

public enum AlertSeverity
{
  Info,
  Warning,
  Error
}

public interface IShape;

public class Circle : IShape
{
  public double Radius { get; set; } = 10;
}

public class Rectangle : IShape
{
  public double Width { get; set; } = 30;
  public double Height { get; set; } = 15;
}

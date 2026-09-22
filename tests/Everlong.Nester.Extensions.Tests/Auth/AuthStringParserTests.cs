using Everlong.Nester.Auth;
using Xunit;

namespace Everlong.Nester.Extensions.Tests.Auth;

/// <summary>
///   Table-driven coverage of <see cref="AuthStringParser.ParseAuthInfo" />:
///   positional vs named segments, empty/whitespace handling, unknown keys,
///   case-insensitivity, and overwrite semantics.
/// </summary>
public class AuthStringParserTests
{
  [Theory]
  [InlineData(null, null, null)]                            // null → authenticated only
  [InlineData("", null, null)]
  [InlineData("PolicyName", "PolicyName", null)]            // positional policy
  [InlineData("PolicyName; Role1,Role2", "PolicyName", "Role1,Role2")]
  [InlineData("Policy=PolicyName", "PolicyName", null)]     // named policy
  [InlineData("P=PolicyName", "PolicyName", null)]
  [InlineData("Roles=Role1,Role2", null, "Role1,Role2")]    // named roles
  [InlineData("R=Role1,Role2", null, "Role1,Role2")]
  [InlineData("Policy=", "", null)]                         // empty named value
  [InlineData("Roles=", null, "")]
  [InlineData("R=,", null, ",")]                            // empty role tokens are preserved
  [InlineData("A;B=C;D", "A", "D")]                         // unknown key segment ignored
  [InlineData("=X", null, null)]                            // empty key ignored
  [InlineData("Policy=A;Roles=B", "A", "B")]
  [InlineData("R=A;P=B", "B", "A")]                         // named order-independent
  [InlineData("Admin;R=Role1", "Admin", "Role1")]           // positional + named mix
  [InlineData("Admin;", "Admin", null)]                     // trailing empty segment skipped
  [InlineData(";R=X", null, "X")]                           // leading empty segment skipped
  [InlineData("A; B; C", "A", "B")]                         // third positional slot ignored
  [InlineData("policy=X", "X", null)]                       // case-insensitive keys
  [InlineData("A ; B", "A", "B")]                           // segment trimming
  [InlineData("P=A;R=B;Policy=C", "C", "B")]                // later named segment wins
  public void ParseAuthInfo_VariousFormats(string? input, string? expectedPolicy, string? expectedRoles)
  {
    (string? policy, string? roles) = AuthStringParser.ParseAuthInfo(input);

    Assert.Equal(expectedPolicy, policy);
    Assert.Equal(expectedRoles, roles);
  }
}

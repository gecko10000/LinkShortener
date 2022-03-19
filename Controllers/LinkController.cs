using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Data.SQLite;

namespace LinkShortener.Controllers;

[ApiController]
[Route("")]
public class LinkController : ControllerBase
{

  private static Dictionary<string, string> Links = new Dictionary<string, string>();

  private SQLiteConnection openConnection()
  {
    new DirectoryInfo("data").Create();
    var connection = new SQLiteConnection(@"URI=file:data/links.db");
    connection.Open();
    using var command = new SQLiteCommand(connection);
    command.CommandText = @"CREATE TABLE IF NOT EXISTS links (long TEXT UNIQUE, short TEXT UNIQUE);";
    command.ExecuteNonQuery();
    return connection;
  }

  private string? GetExisting(SQLiteConnection c, string l)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT short FROM links WHERE long=@long;";
    command.Parameters.AddWithValue("@long", l);
    return (string?) command.ExecuteScalar();
  }

  private bool CheckExists(SQLiteConnection c, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT long FROM links WHERE short=@short;";
    command.Parameters.AddWithValue("@short", s);
    return command.ExecuteScalar() is not null;
  }

  private void Insert(SQLiteConnection c, string l, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"INSERT INTO links(long, short) VALUES(@long, @short);";
    command.Parameters.AddWithValue("@long", l);
    command.Parameters.AddWithValue("@short", s);
    command.ExecuteNonQuery();
  }

  [HttpPost("set")]
  public IActionResult Shorten([FromForm] string link)
  {
    using var connection = openConnection();
    using var command = new SQLiteCommand(connection);

    // Get existing first
    string? existing = GetExisting(connection, link);
    if (existing is not null)
    {
      return Ok(existing);
    }

    string shortened = GenerateRandomString(4, connection);
    Insert(connection, link, shortened);
    return Ok(shortened);
  }

  private string GenerateRandomString(int length, SQLiteConnection connection)
  {
    StringBuilder builder;
    Random r = new Random();
    do
    {
      builder = new StringBuilder(length);
      for (int i = 0; i < length; i++)
      {
        builder.Append(RandomChar(r));
      }
    } while (CheckExists(connection, builder.ToString()));
    return builder.ToString();
  }

  private static string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ123456789";

  private char RandomChar(Random r)
  {
    return chars[r.Next(chars.Length)];
  }

  [HttpGet("{link}")]
  public IActionResult ProcessLink(string link)
  {
    return Links.TryGetValue(link, out string? v) ? Redirect(v) : BadRequest("This link does not exist.");
  }
}
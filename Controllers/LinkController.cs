using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace LinkShortener.Controllers;

[ApiController]
[Route("")]
public class LinkController : ControllerBase
{

  private SQLiteConnection openConnection()
  {
    new DirectoryInfo("data").Create();
    var connection = new SQLiteConnection(@"URI=file:data/links.db");
    connection.Open();
    using var command = new SQLiteCommand(connection);
    command.CommandText = @"CREATE TABLE IF NOT EXISTS links (long TEXT UNIQUE, short TEXT UNIQUE, visits INT, ip TEXT);";
    command.ExecuteNonQuery();
    return connection;
  }

  private string? GetExistingShort(SQLiteConnection c, string l)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT short FROM links WHERE long=@long;";
    command.Parameters.AddWithValue("@long", l);
    return (string?) command.ExecuteScalar();
  }

  private string? GetExistingLong(SQLiteConnection c, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT long FROM links WHERE short=@short;";
    command.Parameters.AddWithValue("@short", s);
    return (string?) command.ExecuteScalar();
  }

  private bool CheckExists(SQLiteConnection c, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT long FROM links WHERE short=@short;";
    command.Parameters.AddWithValue("@short", s);
    return command.ExecuteScalar() is not null;
  }

  private void IncrementVisits(SQLiteConnection c, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"UPDATE links SET visits = visits + 1 WHERE short=@short;";
    command.Parameters.AddWithValue("@short", s);
    command.ExecuteNonQuery();
  }

  private int GetVisits(SQLiteConnection c, string s)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"SELECT visits FROM links WHERE short=@short;";
    command.Parameters.AddWithValue("@short", s);
    return (int) command.ExecuteScalar();
  }

  private void Insert(SQLiteConnection c, string l, string s, string ip)
  {
    using var command = new SQLiteCommand(c);
    command.CommandText = @"INSERT INTO links(long, short, visits, ip) VALUES(@long, @short, 0, @ip);";
    command.Parameters.AddWithValue("@long", l);
    command.Parameters.AddWithValue("@short", s);
    command.Parameters.AddWithValue("@ip", ip);
    command.ExecuteNonQuery();
  }

  private static readonly Regex REGEX = new Regex(@"(https?:\/\/)?([^\/\n]+)(\S*)", RegexOptions.Compiled);

  private string? CleanLink(string link)
  {
    Match match = REGEX.Match(link);
    if (!match.Success)
    {
      return null;
    }
    string afterDomain = match.Groups[3].Value;
    return match.Groups[2].Value.ToLower() + (afterDomain.Equals("") ? "" : "/") + afterDomain;
  }

  private static HashSet<string> RateLimited = new HashSet<string>();

  [HttpPost("set")]
  public IActionResult Shorten([FromForm] string link)
  {
    string? cleaned = CleanLink(link);
    if (cleaned is null)
    {
      return BadRequest("Malformed link.");
    }
    using var connection = openConnection();
    using var command = new SQLiteCommand(connection);

    // Get existing first
    string? existing = GetExistingShort(connection, cleaned);
    if (existing is not null)
    {
      return Ok(existing);
    }

    // rate limit creation of new ones
    string ip = Request.Headers["CF-CONNECTING-IP"];
    if (!RateLimited.Add(ip))
    {
      return BadRequest("Slow down!");
    }
    Task.Run(() => {
      Thread.Sleep(1000 * 10);
      RateLimited.Remove(ip);
    });

    string shortened = GenerateRandomString(4, connection);
    Insert(connection, cleaned, shortened, ip);
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

  // no I, l, O, or 0
  private const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ123456789";

  private char RandomChar(Random r)
  {
    return chars[r.Next(chars.Length)];
  }

  [HttpGet("{link}")]
  public IActionResult ProcessLink(string link)
  {
    using var connection = openConnection();
    string? l = GetExistingLong(connection, link);
    if (l is not null)
    {
      IncrementVisits(connection, link);
    }
    return l is null ? NonExistent() : Redirect("http://" + l);
  }

  [HttpGet("{link}/stats")]
  public IActionResult LinkStats(string link)
  {
    using var connection = openConnection();
    string? l = GetExistingLong(connection, link);
    return l is null ? NonExistent() : Ok(new {
      visits = GetVisits(connection, link),
      link = "http://" + l
    });
  }

  private IActionResult NonExistent()
  {
    return BadRequest("This link does not exist.");
  }

}
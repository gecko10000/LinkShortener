using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LinkShortener.Controllers;

[ApiController]
[Route("")]
public class LinkController : ControllerBase
{

  private static Dictionary<string, string> Links = new Dictionary<string, string>();

  [HttpPost("set")]
  public IActionResult Shorten([FromForm] string link)
  {
    string shortened = GenerateRandomString(4);
    Links.Add(shortened, link);
    return Ok(shortened);
  }

  private string GenerateRandomString(int length)
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
    } while (Links.ContainsKey(builder.ToString()));
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
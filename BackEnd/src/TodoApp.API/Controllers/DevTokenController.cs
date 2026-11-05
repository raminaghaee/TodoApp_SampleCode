using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace TodoApp.API.Controllers;

[ApiController]
public class DevTokenController : ControllerBase
{
	[HttpGet("api/v1/dev/token")]
	[AllowAnonymous]
	public IActionResult GetToken([FromServices] IConfiguration config)
	{
		var key = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

		var token = new JwtSecurityToken(
			issuer: config["Jwt:Issuer"],
			audience: config["Jwt:Audience"],
			claims: new[]
			{
				new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
				new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())
			},
			expires: DateTime.UtcNow.AddHours(8),
			signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

		return Ok(new JwtSecurityTokenHandler().WriteToken(token));
	}
}

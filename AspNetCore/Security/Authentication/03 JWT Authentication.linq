<Query Kind="Statements">
  <NuGetReference Version="6.0.16">Microsoft.AspNetCore.Authentication.JwtBearer</NuGetReference>
  <NuGetReference>Swashbuckle.AspNetCore.SwaggerGen</NuGetReference>
  <NuGetReference>Swashbuckle.AspNetCore.SwaggerUI</NuGetReference>
  <Namespace>Microsoft.AspNetCore.Authentication.JwtBearer</Namespace>
  <Namespace>Microsoft.AspNetCore.Builder</Namespace>
  <Namespace>Microsoft.AspNetCore.Http</Namespace>
  <Namespace>Microsoft.Extensions.Configuration</Namespace>
  <Namespace>Microsoft.Extensions.Configuration.Memory</Namespace>
  <Namespace>Microsoft.Extensions.DependencyInjection</Namespace>
  <Namespace>Microsoft.Extensions.Hosting</Namespace>
  <Namespace>Microsoft.IdentityModel.Tokens</Namespace>
  <Namespace>System.IdentityModel.Tokens.Jwt</Namespace>
  <Namespace>System.Security.Claims</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
  <Namespace>Microsoft.AspNetCore.Authorization</Namespace>
  <Namespace>Microsoft.OpenApi.Models</Namespace>
  <Namespace>Swashbuckle.AspNetCore.SwaggerUI</Namespace>
  <IncludeAspNet>true</IncludeAspNet>
</Query>

#load ".\UserService"
//https://www.tektutorialshub.com/asp-net-core/jwt-authentication-in-asp-net-core/

//JWT全称JSON Web Token，是一种用于身份验证和授权的开放标准。
//它是一种轻量级的、安全的、独立于语言和平台的方式，用于在应用程序之间传递信息。
//JWT通常由三部分组成：头部、载荷和签名。
//	标头包含有关颁发者用于生成令牌的签名算法和令牌的类型；
//	{
// 		"alg": "HS256",
// 		"typ": "JWT"
//	}
//	载荷包含额外的数据，如用户信息，权限等；
//	{
// 		“sub”: “1234567890”,
// 		“name”: “John Doe”,
// 		“iat”: 1516239022,
// 		"role": "Admin",
// 		"userdefined":"Whatever"
//	}
//	签名是对头部和载荷的组合进行加密得到的，用于验证JWT的真实性。
//JWT认证在前后端分离的应用中广泛使用，可以提高应用程序的安全性，同时也可以减少服务器负载。

//JWT 声明
//1.注册声明（registered claims）：这些声明是JWT的标准属性，虽然不是强制的，但是推荐使用。包括：
//
//	iss（issuer）：表示签发该JWT令牌的实体，一般为请求方。通常情况下，iss 所对应的值可以是一个URL或者一个企业或组织的名称。
//	sub（subject）：表示JWT的主题，一般为用户或应用标识。
//	aud（audience）：表示JWT的接收方，可以是单个或多个接收方。一般情况下，aud 所对应的值可以是接收JWT的应用程序或者系统。如果 JWT 只会发送到一个应用程序中，那么 aud 的值可以是该应用程序的自定义域名或者系统名称。
//	exp（expiration time）：表示JWT的过期时间，该时间必须小于签发时间。
//	nbf（not before）：表示JWT的生效时间，该时间必须小于等于签发时间。
//	iat（issued at）：表示JWT的签发时间。
//	jti（JWT ID）：表示JWT的唯一标识，一般用于防止重复使用。
//2.公共声明（public claims）：这些声明是自定义的，用于满足应用程序的需要。
//任何人都可以定义自己的公共声明，但需要避免和注册声明重复。例如：
//
//	name：表示用户的姓名。
//	gender：表示用户的性别。
//	avatar：表示用户的头像。
//	role：表示用户的角色或权限。
//3.私有声明（private claims）：这些声明是供给定应用程序使用的自定义声明，它们不会被其他应用程序使用。

//刷新Access Token
//JWT 带有使用 exp 声明的到期日期。如果没有到期日期，令牌的有效期为很长时间。
//	只要令牌的签名有效且令牌未过期，服务器就会信任令牌。如果任何黑客掌握了令牌，他可以使用它来冒充真正的用户。
//	即使用户更改其密码，令牌仍然有效。使其失效的唯一方法是更改，这会使所有令牌失效。
//
//因此，我们需要设置一个更短的到期日期。但这会给真正的用户带来问题，因为他们每次令牌过期时都需要重新登录才能获得新令牌。
//
//这就是刷新令牌出现的原因。颁发者将刷新令牌与访问令牌一起颁发。与访问令牌不同，颁发者将安全地存储刷新令牌。
//	刷新令牌也会过期，但持续时间将比访问令牌长。
//
//访问令牌过期后，用户会将刷新令牌提交给颁发者。颁发者验证刷新令牌，并颁发新的访问令牌以及新的刷新令牌。
//	还会删除旧的刷新令牌，以便用户无法再次使用它。
//
//所有这些都将在用户不知情的情况下在幕后发生。
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	EnvironmentName = Environments.Development
});

builder.Configuration.AddInMemoryCollection(
	new[] {
		new KeyValuePair<string, string>("Jwt:Secret", "F-JaNdRfUserjd89#5*6Xn2r5usErw8x/A?D(G+KbPeShV"),
		new KeyValuePair<string, string>("Jwt:Issuer", "http://localhost:5000/"),
		new KeyValuePair<string, string>("Jwt:Audience", "http://localhost:5000/"),
		new KeyValuePair<string, string>("Jwt:AccessTokenExpiration", "5"),
		new KeyValuePair<string, string>("Jwt:RefreshTokenExpiration", "10"),
	});

var jwtTokenConfig =builder.Configuration.GetSection("Jwt").Get<JwtTokenConfig>();
builder.Services.AddSingleton(jwtTokenConfig);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{  
		options.SaveToken = true;
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidIssuer = jwtTokenConfig.Issuer,
			ValidateAudience = true,
			ValidAudience = jwtTokenConfig.Audience,
			ValidateIssuerSigningKey = true,
			RequireExpirationTime = false,
			ValidateLifetime = true,
			ClockSkew = TimeSpan.Zero,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtTokenConfig.Secret))
		};
	});
	
builder.Services.AddScoped<JWTAuthService>();
builder.Services.AddScoped<SignInManager>();
builder.Services.AddSingleton<IUserService,UserService>();
builder.Services.AddSingleton<IRefreshTokenService,RefreshTokenService>();

// Authorization middleware
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = @"JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.
                          Example: 'Bearer 12345abcdef'",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] { }
		}
	});
});


// Configure pipeline
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => "Hello World!");
app.MapGet("/home", [Authorize] async (HttpContext context) =>
{
	var result = "";
	var user = context.User;
	if (user.Identity.IsAuthenticated)
	{
		var username = user.Identity.Name;
		var claims = user.Claims;

		// 打印用户信息
		foreach (var claim in claims)
		{
			result += (claim.Type + ": " + claim.Value + "\n\r");
		}
	}

	return result;
});
app.MapPost("/login", async (LoginRequest login, SignInManager _signInManager) =>
{
   	var result= await _signInManager.SignIn(login.UserName, login.Password);

	if (!result.Success) 
		return Results.Unauthorized();

  	return Results.Ok(new LoginResult()
    {
        UserName = result.User.Id.ToString(),
        AccessToken = result.AccessToken,
        RefreshToken = result.RefreshToken
    });
});

app.MapPost("/refreshtoken", async (RefreshTokenRequest request,SignInManager _signInManager) =>
{
	var result = await _signInManager.RefreshToken(request.AccessToken, request.RefreshToken);

	if (!result.Success) 
		return Results.Unauthorized();

  	return Results.Ok(new LoginResult()
    {
        UserName = result.User.Id.ToString(),
		  AccessToken = result.AccessToken,
		  RefreshToken = result.RefreshToken
	  });
});

Process.Start(new ProcessStartInfo
{
	FileName = "http://localhost:5000/swagger/index.html",
	UseShellExecute = true,
});

app.Run();

public class JWTAuthService
{
	private readonly JwtTokenConfig _jwtTokenConfig;
	public JWTAuthService(JwtTokenConfig jwtTokenConfig)
	{
		_jwtTokenConfig = jwtTokenConfig;
	}

	//创建JWT Token
	public string GenerateSecurityToken(Claim[] claims)
	{
		var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenConfig.Secret));
		var creds  = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

		var tokenOptions = new JwtSecurityToken(
			issuer: _jwtTokenConfig.Issuer,
			audience: _jwtTokenConfig.Audience,
			notBefore: DateTime.Now,
			claims: claims,
			expires: DateTime.Now.AddMinutes(_jwtTokenConfig.AccessTokenExpiration),
			signingCredentials: creds 
		);

		var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
		return tokenString;
	}

	//创建 刷新Token，需要合理唯一 不被轻易被猜到。
	public string BuildRefreshToken()
	{
		var randomNumber = new byte[32];
		using (var randomNumberGenerator = RandomNumberGenerator.Create())
		{
			randomNumberGenerator.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}
	}
	
	// 从 JWT Token 中过去 User ( ClaimsPrincipal ) .
	public ClaimsPrincipal GetPrincipalFromToken(string token)
	{
		JwtSecurityTokenHandler tokenValidator = new JwtSecurityTokenHandler();
		var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtTokenConfig.Secret));
		var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

		var parameters = new TokenValidationParameters
		{
			ValidateAudience = false,
			ValidateIssuer = false,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = key,
			ValidateLifetime = false
		};

		try
		{
			var principal = tokenValidator.ValidateToken(token, parameters, out var securityToken);

			if (!(securityToken is JwtSecurityToken jwtSecurityToken) || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine($"Token validation failed");
				return null;
			}

			return principal;
		}
		catch (Exception e)
		{
			Console.WriteLine($"Token validation failed: {e.Message}");
			return null;
		}
	}
}

public class JwtTokenConfig
{
	public string Secret { get; set; }
	public string Issuer { get; set; }
	public string Audience { get; set; }
	public int AccessTokenExpiration { get; set; }
	public int RefreshTokenExpiration { get; set; }
}

public class SignInManager
{
	private readonly JWTAuthService _JwtAuthService;
	private readonly JwtTokenConfig _jwtTokenConfig;
	private readonly IUserService _userService;
	private readonly IRefreshTokenService _refreshTokenService;

	public SignInManager(
						 JWTAuthService JWTAuthService,
						 JwtTokenConfig jwtTokenConfig,
						 IUserService userService,
						 IRefreshTokenService refreshTokenService
						 )
	{
		_userService = userService;
		_JwtAuthService = JWTAuthService;
		_jwtTokenConfig = jwtTokenConfig;
		_refreshTokenService = refreshTokenService;
	}

	public async Task<SignInResult> SignIn(string userName, string password)
	{
		SignInResult result = new SignInResult();

		if (string.IsNullOrWhiteSpace(userName)) return result;
		if (string.IsNullOrWhiteSpace(password)) return result;

		//从数据库中验证用户名和密码
		var user = await _userService.Authenticate(userName,password);
		if (user != null)
		{
			//创建 claims
			var claims = BuildClaims(user);
			result.User = user;
			//使用_JwtAuthService创建 Access token & Refresh token
			result.AccessToken = _JwtAuthService.GenerateSecurityToken(claims);
			result.RefreshToken = _JwtAuthService.BuildRefreshToken();
			
			//保存 RefreshTokens to database
			_refreshTokenService.Add(new RefreshToken { UserId = user.Id, Token = result.RefreshToken, IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddMinutes(_jwtTokenConfig.RefreshTokenExpiration) });
			result.Success = true;
		};

		return result;
	}

	//用于验证当前 Access Token & Refresh Token 如过正确重新 Access Token & Refresh Token
	public async System.Threading.Tasks.Task<SignInResult> RefreshToken(string AccessToken, string RefreshToken)
	{
		//使用当前Access Token恢复出 claimsPrincipal。
		ClaimsPrincipal claimsPrincipal = _JwtAuthService.GetPrincipalFromToken(AccessToken);
		SignInResult result = new SignInResult();

		if (claimsPrincipal == null) return result;

		//查找用户是否还存在。
		string id = claimsPrincipal.Claims.First(c => c.Type == "id").Value;
		var user = await _userService.FindAsync(Convert.ToInt32(id));

		if (user == null) return result;

		//查询数据库中的 RefreshToken 是否尚未过期
		var token = _refreshTokenService.GetAll()
				.Where(f => f.UserId == user.Id
						&& f.Token == RefreshToken
						&& f.ExpiresAt >= DateTime.Now)
				.FirstOrDefault();

		if (token == null) return result;

		var claims = BuildClaims(user);

		//创建新的AccessToken和RefreshToken
		result.User = user;
		result.AccessToken = _JwtAuthService.GenerateSecurityToken(claims);
		result.RefreshToken = _JwtAuthService.BuildRefreshToken();
		
		//删除旧的Token 并添加新的过去token。
		_refreshTokenService.Remove(token);
		_refreshTokenService.Add(new RefreshToken { UserId = user.Id, Token = result.RefreshToken, IssuedAt = DateTime.Now, ExpiresAt = DateTime.Now.AddMinutes(_jwtTokenConfig.RefreshTokenExpiration) });

		result.Success = true;

		return result;
	}

	private Claim[] BuildClaims(User user)
	{
		//User is Valid
		var claims = new[]
		{
				new Claim("id",user.Id.ToString()),
				new Claim(ClaimTypes.Name,user.Username),
 				new Claim("UserDefined", "whatever"),
                //Add Custom Claims here
            };

		return claims;
	}
}

public class SignInResult
{
	public bool Success { get; set; }
	public User User { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }

	public SignInResult()
	{
		Success = false;
	}
}

public class RefreshTokenService : IRefreshTokenService
{
	private readonly List<RefreshToken> tokens = new List<RefreshToken>();
	public void Add(RefreshToken token)
	{
		tokens.Add(token);
	}

	public IList<RefreshToken> GetAll()
	{
		return tokens;
	}

	public void Remove(RefreshToken token)
	{
		tokens.Remove(token);
	}
}

public interface IRefreshTokenService
{
	public void Add(RefreshToken token);
	public void Remove(RefreshToken token);
	public IList<RefreshToken> GetAll();
}

public class RefreshToken
{
	public string Token { get; set; }
	public int UserId { get; set; }
	public DateTime IssuedAt { get; set; }
	public DateTime ExpiresAt { get; set; }
}

public class LoginResult
{
	public string UserName { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
}

public class RefreshTokenRequest
{
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
}
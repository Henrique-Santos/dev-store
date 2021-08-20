using DevStore.Core.Communication;
using DevStore.WebAPI.Core.Usuario;
using DevStore.WebApp.MVC.Extensions;
using DevStore.WebApp.MVC.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevStore.WebApp.MVC.Services
{
    public interface IAuthService
    {
        Task<UserLoginResponse> Login(UserLogin userLogin);

        Task<UserLoginResponse> Register(UserRegister userRegister);

        Task DoLogin(UserLoginResponse resposta);
        Task Logout();

        bool ExpiredToken();

        Task<bool> ValidRefreshToken();
    }

    public class AuthService : Service, IAuthService
    {
        private readonly HttpClient _httpClient;

        private readonly IAspNetUser _user;
        private readonly IAuthenticationService _authenticatioDevStorervice;

        public AuthService(HttpClient httpClient,
                                   IOptions<AppSettings> settings,
                                   IAspNetUser user,
                                   IAuthenticationService authenticatioDevStorervice)
        {
            httpClient.BaseAddress = new Uri(settings.Value.AuthUrl);

            _httpClient = httpClient;
            _user = user;
            _authenticatioDevStorervice = authenticatioDevStorervice;
        }

        public async Task<UserLoginResponse> Login(UserLogin userLogin)
        {
            var loginContent = GetContent(userLogin);

            var response = await _httpClient.PostAsync("/api/identity/auth", loginContent);

            if (!ManageResponseErrors(response))
            {
                return new UserLoginResponse
                {
                    ResponseResult = await DeserializeResponse<ResponseResult>(response)
                };
            }

            return await DeserializeResponse<UserLoginResponse>(response);
        }

        public async Task<UserLoginResponse> Register(UserRegister userRegister)
        {
            var registroContent = GetContent(userRegister);

            var Response = await _httpClient.PostAsync("/api/identity/new-account", registroContent);

            if (!ManageResponseErrors(Response))
            {
                return new UserLoginResponse
                {
                    ResponseResult = await DeserializeResponse<ResponseResult>(Response)
                };
            }

            return await DeserializeResponse<UserLoginResponse>(Response);
        }

        public async Task<UserLoginResponse> UseRefreshToken(string refreshToken)
        {
            var refreshTokenContent = GetContent(refreshToken);

            var Response = await _httpClient.PostAsync("/api/identity/refresh-token", refreshTokenContent);

            if (!ManageResponseErrors(Response))
            {
                return new UserLoginResponse
                {
                    ResponseResult = await DeserializeResponse<ResponseResult>(Response)
                };
            }

            return await DeserializeResponse<UserLoginResponse>(Response);
        }

        public async Task DoLogin(UserLoginResponse resposta)
        {
            var token = FormatToken(resposta.AccessToken);

            var claims = new List<Claim>();
            claims.Add(new Claim("JWT", resposta.AccessToken));
            claims.Add(new Claim("RefreshToken", resposta.RefreshToken));
            claims.AddRange(token.Claims);

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
                IsPersistent = true
            };

            await _authenticatioDevStorervice.SignInAsync(
                _user.GetHttpContext(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        public async Task Logout()
        {
            await _authenticatioDevStorervice.SignOutAsync(
                _user.GetHttpContext(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                null);
        }

        public static JwtSecurityToken FormatToken(string jwtToken)
        {
            return new JwtSecurityTokenHandler().ReadToken(jwtToken) as JwtSecurityToken;
        }

        public bool ExpiredToken()
        {
            var jwt = _user.GetUserToken();
            if (jwt is null) return false;

            var token = FormatToken(jwt);
            return token.ValidTo.ToLocalTime() < DateTime.Now;
        }

        public async Task<bool> ValidRefreshToken()
        {
            var resposta = await UseRefreshToken(_user.GetUserRefreshToken());

            if (resposta.AccessToken != null && resposta.ResponseResult == null)
            {
                await DoLogin(resposta);
                return true;
            }

            return false;
        }
    }
}

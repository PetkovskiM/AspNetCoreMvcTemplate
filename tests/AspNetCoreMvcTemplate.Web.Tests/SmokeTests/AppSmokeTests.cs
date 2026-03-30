using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace AspNetCoreMvcTemplate.Web.Tests.SmokeTests
{
    public class AppSmokeTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient client;

        public AppSmokeTests(WebApplicationFactory<Program> factory)
        {
            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        [Fact]
        public async Task HomePage_ReturnsSuccess()
        {
            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task LoginPage_ReturnsSuccess()
        {
            // Act
            var response = await client.GetAsync("/Account/Login");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RegisterPage_ReturnsSuccess()
        {
            // Act
            var response = await client.GetAsync("/Account/Register");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedPage_RedirectsAnonymousUserToLogin()
        {
            // Use the protected route you already have in the app.
            // Replace /Home/Privacy if your protected page is different.
            var response = await client.GetAsync("/Home/Privacy");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

            var redirectUrl = response.Headers.Location?.ToString();

            Assert.NotNull(redirectUrl);
            Assert.Contains("/Account/Login", redirectUrl);
            Assert.Contains("ReturnUrl", redirectUrl);
        }
    }
}

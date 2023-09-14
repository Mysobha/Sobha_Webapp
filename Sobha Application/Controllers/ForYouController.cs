﻿using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Sobha_Application.Models;
using System.Net.Http.Headers;

namespace Sobha_Application.Controllers
{
    public class ForYouController : Controller
    {
        private readonly ILogger<YourSobhaController> _logger;
        private readonly IConfiguration _configuration;
        public ForYouController(ILogger<YourSobhaController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<IActionResult> IndexAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                Guid guidResult = Guid.Empty;
                SharePointList SharePointFinallist = new SharePointList();
                dynamic userJobDetails = "";
                dynamic userPhoto = "";
                string username = "";

                ////Actve dircetory details////
                string BaseUrl = _configuration.GetSection("BaseUrl").GetSection("url").Value;
                string clientID = _configuration.GetSection("AzureAd").GetSection("ClientId").Value;
                string clientSecret = _configuration.GetSection("AzureAd").GetSection("ClientSecret").Value;

                /////Logged in user details////
                //ViewBag.userName = HttpContext.User.Claims.ToList()[2].Value;
                string userID = HttpContext.User.Claims.ToList()[3].Value;
                string tenantID = HttpContext.User.Claims.ToList()[7].Value;
                username = HttpContext.User.Claims.ToList()[2].Value;
                bool isValid = Guid.TryParse(tenantID, out guidResult);

                if (isValid == false)
                {
                    tenantID = _configuration.GetSection("AzureAd").GetSection("TenantId").Value;
                    userID = HttpContext.User.Claims.ToList()[2].Value;
                    username = HttpContext.User.Claims.ToList()[1].Value;
                }

                try
                {

                    var httpClient = new HttpClient();

                    var scopes = new[] { "https://graph.microsoft.com/.default" };

                    var clientSecretCredential = new ClientSecretCredential(tenantID, clientID, clientSecret);
                    var tokenRequestContext = new TokenRequestContext(scopes);

                    //////Token fetched///////
                    var token = clientSecretCredential.GetTokenAsync(tokenRequestContext).Result.Token;
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);


                    ////////API call for user profile image///////
                    using (var pictureResult = await httpClient.GetAsync(BaseUrl + "/users/" + userID + "/photo/$value"))
                    {

                        if (pictureResult.IsSuccessStatusCode)
                        {
                            var stream = await pictureResult.Content.ReadAsStreamAsync();
                            byte[] bytes = new byte[stream.Length];
                            stream.Read(bytes, 0, (int)stream.Length);
                            userPhoto = "data:" + pictureResult.Content.Headers.ContentType.ToString() + ";base64," + Convert.ToBase64String(bytes);

                        }
                    }

                    ///////API call for user personal details////////
                    var response = await httpClient.GetAsync(BaseUrl + "/users/" + userID + "?$select=displayName,givenName,postalCode,identities,jobTitle,Department,EmployeeID");
                    if (response.IsSuccessStatusCode)
                    {
                        var stringifiedResponse = await response.Content.ReadAsStringAsync();
                        dynamic userDetails = JObject.Parse(stringifiedResponse);
                        userJobDetails = userDetails.jobTitle;
                    }

                    SharePointFinallist.Username = username;
                    SharePointFinallist.UserJobTitle = userJobDetails == null ? "" : userJobDetails.jobTitle;
                    SharePointFinallist.UserPhoto = userPhoto == null ? "" : userPhoto;

                }
                catch (Exception ex)
                {

                    throw;
                }

                return View(SharePointFinallist);
            }
            return RedirectToAction("Index", "Account");
        }
    }
}

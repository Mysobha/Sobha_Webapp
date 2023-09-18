using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sobha_Application.Models;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.Claims;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models.ExternalConnectors;
using Azure;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Graph.Models;
using System.Text.Json.Nodes;

namespace Sobha_Application.Controllers
{
    public class HomePageController : Controller
    {
        private readonly ILogger<HomePageController> _logger;
        //private readonly GraphServiceClient _graphServiceClient;
        private readonly IConfiguration _configuration;
        public HomePageController(ILogger<HomePageController> logger, IConfiguration configuration)
        {
            _logger = logger;
            //_graphServiceClient = graphServiceClient;
            _configuration = configuration;
        }
        public async Task<IActionResult> IndexAsync()
        {
            if (User.Identity.IsAuthenticated)
            {
                Guid guidResult = Guid.Empty;
                SharePointList Spotlightlist = new SharePointList();
                SharePointList OrgUpdatelist = new SharePointList();
                OrgSpotlightListView orgSpotlightListView = new OrgSpotlightListView();

                dynamic userJobDetails = "";
                dynamic userPhoto = "";
                string username = "";
                ////Actve dircetory details////
                string BaseUrl = _configuration.GetSection("BaseUrl").GetSection("url").Value;
                string clientID = _configuration.GetSection("AzureAd").GetSection("ClientId").Value;
                string clientSecret = _configuration.GetSection("AzureAd").GetSection("ClientSecret").Value;

                ///SharePoint Library Detail///

                string SharepointlibraryTokenEndpoint = _configuration.GetSection("SharePointLibrary").GetSection("TokenEndpoint").Value;
                string SharepointlibraryclientID = _configuration.GetSection("SharePointLibrary").GetSection("ClientId").Value;
                string SharepointlibraryclientSecret = _configuration.GetSection("SharePointLibrary").GetSection("ClientSecret").Value;
                string SharepointlibraryResource = _configuration.GetSection("SharePointLibrary").GetSection("Resource").Value;

                /////Logged in user details////

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
                        userJobDetails = JObject.Parse(stringifiedResponse);

                    }


                    orgSpotlightListView.Username = username;
                    orgSpotlightListView.UserJobTitle = userJobDetails == null ? "" : userJobDetails.jobTitle;
                    orgSpotlightListView.UserPhoto = userPhoto == null ? "" : userPhoto;

                    //Quick Links getting from appsetting.json file//

                    orgSpotlightListView.EmoloyeeSelfService = _configuration["QuickLinkURL:Employee Self Service"];
                    orgSpotlightListView.WorldClient = _configuration["QuickLinkURL:WorldClient"];
                    orgSpotlightListView.AdministrationHelpDesk = _configuration["QuickLinkURL:Administration Help Desk"];
                    orgSpotlightListView.AuditManagementSystem = _configuration["QuickLinkURL:Audit Management System"];
                    orgSpotlightListView.ClubHouseApplication = _configuration["QuickLinkURL:Club House Application"];
                    orgSpotlightListView.CustomerCareCellApplication = _configuration["QuickLinkURL:Customer Care Cell Application"];
                    orgSpotlightListView.DocumentManagementSystemApplication = _configuration["QuickLinkURL:Document Management System Application"];
                    orgSpotlightListView.DocumentManagementSystemApplicationforCRM = _configuration["QuickLinkURL:Document Management System Application (for CRM)"];
                    orgSpotlightListView.IdeaSpaceApplication = _configuration["QuickLinkURL:Idea Space Application"];
                    orgSpotlightListView.ProjectClosureMaintenanceApplication = _configuration["QuickLinkURL:Project Closure & Maintenance Application"];
                    orgSpotlightListView.QualitySafetyTechnologyHomePage = _configuration["QuickLinkURL:Quality Safety & Technology Home Page"];
                    orgSpotlightListView.SafetyReportingApplication = _configuration["QuickLinkURL:Safety Reporting Application"];
                    orgSpotlightListView.SobhaTechnologyManual = _configuration["QuickLinkURL:Sobha Technology Manual"];
                    orgSpotlightListView.PITHelpDesk = _configuration["QuickLinkURL:P&IT Help Desk"];

                    ///// API for fetch site content///
                    var SiteDataEndPoint = _configuration["SharePointOnline:SiteDataEndPoint"];

                    response = await httpClient.GetAsync(SiteDataEndPoint);
                    if (response.IsSuccessStatusCode)
                    {
                        var siteData = response.Content.ReadAsStringAsync().Result;
                        var sharepointSite = JsonConvert.DeserializeObject<SharePointSite>(siteData);

                        var ListsEndPoint = _configuration["SharePointOnline:ListsEndPoint"];
                        ListsEndPoint = string.Format(ListsEndPoint, (sharepointSite == null ? "SOBHAID" : sharepointSite.id));
                        response = await httpClient.GetAsync(ListsEndPoint);

                        if (response.IsSuccessStatusCode)
                        {
                            var listData = response.Content.ReadAsStringAsync().Result;
                            var sharePointList = JsonConvert.DeserializeObject<SharePointList>(listData);

                            //SpotLight List ID and Details
                            var listid = sharePointList == null ? "" : sharePointList.value.FirstOrDefault(obj => obj.displayName == "Spotlight").id;

                            var ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilter"];
                            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, (listid == "" ? "SOBHAID" : listid));

                            response = await httpClient.GetAsync(ListDataEndPoint);

                            if (response.IsSuccessStatusCode)
                            {
                                var ListData = response.Content.ReadAsStringAsync().Result;
                                Spotlightlist = JsonConvert.DeserializeObject<SharePointList>(ListData);
                            }


                            //Org Update List ID and Details

                            listid = sharePointList == null ? "" : sharePointList.value.FirstOrDefault(obj => obj.displayName == "Org Update").id;

                            ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilter"];

                            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, (listid == "" ? "SOBHAID" : listid));

                            response = await httpClient.GetAsync(ListDataEndPoint);

                            if (response.IsSuccessStatusCode)
                            {
                                var ListData = response.Content.ReadAsStringAsync().Result;
                                OrgUpdatelist = JsonConvert.DeserializeObject<SharePointList>(ListData);
                            }
                            orgSpotlightListView.SpotLightsLists = new List<SharePointList>();
                            orgSpotlightListView.OrgUpdateLists = new List<SharePointList>();


                            orgSpotlightListView.SpotLightsLists.Add(Spotlightlist);
                            orgSpotlightListView.OrgUpdateLists.Add(OrgUpdatelist);
                        }

                    }
                    ///API call for SharePoint tokn for asset library images///

                    dynamic result = "";
                    string AccessToken = "";
                    string fileextension = "";
                    var request = new HttpRequestMessage(HttpMethod.Post, SharepointlibraryTokenEndpoint);
                    var collection = new List<KeyValuePair<string, string>>();
                    collection.Add(new("grant_type", "client_credentials"));
                    collection.Add(new("client_id", SharepointlibraryclientID));
                    collection.Add(new("client_secret", SharepointlibraryclientSecret));
                    collection.Add(new("resource", SharepointlibraryResource));
                    var content = new FormUrlEncodedContent(collection);
                    request.Content = content;
                    response = await httpClient.SendAsync(request);
                    if (response.IsSuccessStatusCode)
                    {
                        var stringifiedResponse = await response.Content.ReadAsStringAsync();
                        result = JObject.Parse(stringifiedResponse);
                        AccessToken = result.access_token;

                        var webUrl = _configuration["SharePointLibrary:WebUrl"];

                        using (var client = new HttpClient())
                        {
                            foreach (var item in orgSpotlightListView.SpotLightsLists)
                            {
                               
                                if (item.value.Count > 0)
                                {
                                    foreach (var itemval in item.value.Take(3))
                                    {
                                        bool status = false;

                                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                                        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");
                                        //client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json;odata=verbose");
                                        if (!string.IsNullOrEmpty(itemval.fields.Image))
                                        {
                                            dynamic Image = JObject.Parse(itemval.fields.Image);
                                            var fileUrl = Image.serverRelativeUrl;
                                            fileextension = Image.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.ImageBase64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image1) && status == false)
                                        {
                                            dynamic Image1 = JObject.Parse(itemval.fields.Image1);
                                            var fileUrl = Image1.serverRelativeUrl;
                                            fileextension = Image1.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image1Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }

                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image2) && status == false)
                                        {
                                            dynamic Image2 = JObject.Parse(itemval.fields.Image2);
                                            var fileUrl = Image2.serverRelativeUrl;
                                            fileextension = Image2.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image2Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image3) && status == false)
                                        {
                                            dynamic Image3 = JObject.Parse(itemval.fields.Image3);
                                            var fileUrl = Image3.serverRelativeUrl;
                                            fileextension = Image3.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image3Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image4) && status == false)
                                        {
                                            dynamic Image4 = JObject.Parse(itemval.fields.Image4);
                                            var fileUrl = Image4.serverRelativeUrl;
                                            fileextension = Image4.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image4Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image5) && status == false)
                                        {
                                            dynamic Image5 = JObject.Parse(itemval.fields.Image5);
                                            var fileUrl = Image5.serverRelativeUrl;
                                            fileextension = Image5.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image5Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status= true;
                                            }
                                        }

                                    }
                                }
                            }

                            foreach (var item in orgSpotlightListView.OrgUpdateLists)
                            {
                                
                                if (item.value.Count > 0)
                                {
                                    foreach (var itemval in item.value.Take(3))
                                    {
                                        bool status = false;
                                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
                                        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=verbose");

                                        if (!string.IsNullOrEmpty(itemval.fields.Image))
                                        {
                                            dynamic Image = JObject.Parse(itemval.fields.Image);
                                            var fileUrl = Image.serverRelativeUrl;
                                            fileextension = Image.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.ImageBase64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image1) && status == false)
                                        {
                                            dynamic Image1 = JObject.Parse(itemval.fields.Image1);
                                            var fileUrl = Image1.serverRelativeUrl;
                                            fileextension = Image1.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image1Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status= true;
                                            }

                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image2) && status == false)
                                        {
                                            dynamic Image2 = JObject.Parse(itemval.fields.Image2);
                                            var fileUrl = Image2.serverRelativeUrl;
                                            fileextension = Image2.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image2Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status = true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image3) && status == false)
                                        {
                                            dynamic Image3 = JObject.Parse(itemval.fields.Image3);
                                            var fileUrl = Image3.serverRelativeUrl;
                                            fileextension = Image3.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image3Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status= true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image4) && status == false)
                                        {
                                            dynamic Image4 = JObject.Parse(itemval.fields.Image4);
                                            var fileUrl = Image4.serverRelativeUrl;
                                            fileextension = Image4.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image4Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status= true;
                                            }
                                        }
                                        if (!string.IsNullOrEmpty(itemval.fields.Image5) && status == false)
                                        {
                                            dynamic Image5 = JObject.Parse(itemval.fields.Image5);
                                            var fileUrl = Image5.serverRelativeUrl;
                                            fileextension = Image5.fileName;
                                            var requestUrl = String.Format("{0}/_api/web/getfilebyserverrelativeurl('{1}')/$value", webUrl, fileUrl);
                                            request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                                            response = await client.SendAsync(request);
                                            if (response.IsSuccessStatusCode)
                                            {
                                                byte[] siteImageData = response.Content.ReadAsByteArrayAsync().Result;
                                                var base64 = Convert.ToBase64String(siteImageData);
                                                itemval.fields.Image5Base64 = "data:" + fileextension.Split('.')[1] + ";base64," + base64;
                                                status=true;
                                            }
                                        }

                                    }
                                }
                            }
                        }
                    }
                    }
                catch (Exception ex)
                {

                    throw;
                }

                return View(orgSpotlightListView);
            }
            return RedirectToAction("Index", "Account");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;
using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sobha_Application.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Text.Json.Nodes;

namespace Sobha_Application.Controllers
{
    public class SpotLightController : Controller
    {
        private readonly ILogger<YourSobhaController> _logger;
        private readonly IConfiguration _configuration;
        public SpotLightController(ILogger<YourSobhaController> logger, IConfiguration configuration)
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

                string userID = HttpContext.User.Claims.ToList()[3].Value;
                string useremailID = HttpContext.User.Claims.ToList()[4].Value;
                string tenantID = HttpContext.User.Claims.ToList()[7].Value;
                username = HttpContext.User.Claims.ToList()[2].Value;
                bool isValid = Guid.TryParse(tenantID, out guidResult);
                if (isValid == false)
                {
                    tenantID = _configuration.GetSection("AzureAd").GetSection("TenantId").Value;
                    userID = HttpContext.User.Claims.ToList()[2].Value;
                    username = HttpContext.User.Claims.ToList()[1].Value;
                    useremailID = HttpContext.User.Claims.ToList()[3].Value;
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

                    ///////Punch In - Punch Out///////////////////////

                    var PunchInPunchOutURL = _configuration["PunchInPunchOut:URL"] + "?email=" + useremailID + "&fromDate=" + DateTime.Now.ToString("yyyy-MM-dd") + "&toDate=" + DateTime.Now.ToString("yyyy-MM-dd");

                    var request = new HttpRequestMessage(HttpMethod.Get, PunchInPunchOutURL);

                    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("SobhaAPI" + ":" + "Sdl23@D365"));

                    request.Headers.Add("Authorization", "Basic " + svcCredentials);
                    var responsepunch = await httpClient.SendAsync(request);
                    if (responsepunch.IsSuccessStatusCode)
                    {
                        var PunchInPunchOutResponse = await responsepunch.Content.ReadAsStringAsync();
                        JsonNode data = JsonNode.Parse(PunchInPunchOutResponse);

                        if (data.ToJsonString() != "[]")
                        {
                            string punchIntime = data[0]["inTime"].ToString();
                            TimeSpan NineOclock = TimeSpan.Parse("09:00");
                            TimeSpan PunchIN = TimeSpan.Parse(punchIntime);
                            TimeSpan PunchOUT = TimeSpan.Parse(punchIntime);
                            string AMPM = "";
                            if (PunchIN <= NineOclock)
                            {
                                PunchOUT = PunchOUT.Add(TimeSpan.Parse("08:45"));
                                AMPM = PunchOUT > TimeSpan.Parse("12:00") ? "PM" : "AM";
                                SharePointFinallist.PunchOut = PunchOUT.ToString().Substring(0, PunchOUT.ToString().Length - 3) + " " + AMPM;


                            }
                            else
                            {
                                SharePointFinallist.PunchOut = "17:00 PM";
                            }
                            AMPM = PunchIN < TimeSpan.Parse("12:00") ? "AM" : "PM";

                            SharePointFinallist.PunchIN = PunchIN.ToString().Substring(0, PunchIN.ToString().Length - 3) + " " + AMPM;

                        }


                    }

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

                            var listid = sharePointList == null ? "" : sharePointList.value.FirstOrDefault(obj => obj.displayName == "Spotlight").id;

                            var ListDataEndPoint = _configuration["SharePointOnline:ListDataByFilter"];
                            ListDataEndPoint = string.Format(ListDataEndPoint, sharepointSite.id, (listid == "" ? "SOBHAID" : listid));

                            response = await httpClient.GetAsync(ListDataEndPoint);

                            if (response.IsSuccessStatusCode)
                            {
                                var ListData = response.Content.ReadAsStringAsync().Result;
                                SharePointFinallist = JsonConvert.DeserializeObject<SharePointList>(ListData);


                                ///SharePoint Library Detail///

                                string SharepointlibraryTokenEndpoint = _configuration.GetSection("SharePointLibrary").GetSection("TokenEndpoint").Value;
                                string SharepointlibraryclientID = _configuration.GetSection("SharePointLibrary").GetSection("ClientId").Value;
                                string SharepointlibraryclientSecret = _configuration.GetSection("SharePointLibrary").GetSection("ClientSecret").Value;
                                string SharepointlibraryResource = _configuration.GetSection("SharePointLibrary").GetSection("Resource").Value;

                                dynamic result = "";
                                string AccessToken = "";
                                string fileextension = "";

                                ///API call for get the token for fetch asset library images///

                                request = new HttpRequestMessage(HttpMethod.Post, SharepointlibraryTokenEndpoint);
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
                                        if (SharePointFinallist.value.Count > 0)
                                        {
                                            foreach (var itemval in SharePointFinallist.value)
                                            {

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

                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(itemval.fields.Image1))
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

                                                    }

                                                }
                                                if (!string.IsNullOrEmpty(itemval.fields.Image2))
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

                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(itemval.fields.Image3))
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

                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(itemval.fields.Image4))
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

                                                    }
                                                }
                                                if (!string.IsNullOrEmpty(itemval.fields.Image5))
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

                                                    }
                                                }

                                            }
                                        }

                                    }
                                }
                            }

                        }

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

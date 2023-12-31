﻿using Microsoft.AspNetCore.Authentication.Cookies;
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
using static System.Net.WebRequestMethods;
using CCA.Util;
using Microsoft.AspNetCore.Http;
using System.Text;
using System;

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
                    orgSpotlightListView.DepartmentPolicies = _configuration["QuickLinkURL:Department Policies"] + useremailID;

                    ///Aru -> Payslips
                    ///
             
                    string enUserforPayslips = getUserforJavaDecryption(useremailID);

                    orgSpotlightListView.Payslips = _configuration["QuickLinkURL:Payslips"] + enUserforPayslips+"&type=payslips";
                    orgSpotlightListView.investment = _configuration["QuickLinkURL:Payslips"] + enUserforPayslips + "&type=plannedinvestment";
                    orgSpotlightListView.investmentActual = _configuration["QuickLinkURL:Payslips"] + enUserforPayslips + "&type=actualinvestment";
                    ///////Punch In - Punch Out///////////////////////

                    //useremailID = "armugam.karanam@sobha.com";

                    //  //var PunchInPunchOutURL = _configuration["PunchInPunchOut:URL"] + "?email=" + useremailID + "&fromDate=" + DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd") + "&toDate=" + DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd");
                    try
                    {
                        var PunchInPunchOutURL = _configuration["PunchInPunchOut:URL"] + "?email=" + useremailID + "&fromDate=" + DateTime.Now.ToString("yyyy-MM-dd") + "&toDate=" + DateTime.Now.ToString("yyyy-MM-dd");

                       var  requestatt = new HttpRequestMessage(HttpMethod.Get, PunchInPunchOutURL);

                        string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("SobhaAPI" + ":" + "Sdl23@D365"));

                        requestatt.Headers.Add("Authorization", "Basic " + svcCredentials);

                        var responsepunch = await httpClient.SendAsync(requestatt);
                        if (responsepunch.IsSuccessStatusCode)
                        {
                            var PunchInPunchOutResponse = await responsepunch.Content.ReadAsStringAsync();
                            JsonNode data = JsonNode.Parse(PunchInPunchOutResponse);

                            if (data.ToJsonString() != "[]")
                            {
                                string punchIntime = data[0]["inTime"].ToString();
                                TimeSpan NineOclock = TimeSpan.Parse("09:00");
                                TimeSpan Eightfifteen = TimeSpan.Parse("08:15");
                                TimeSpan PunchIN = TimeSpan.Parse(punchIntime);
                                TimeSpan PunchOUT = TimeSpan.Parse(punchIntime);
                                string AMPM = "";
                                if (PunchIN <= NineOclock)
                                {
                                    PunchOUT = PunchOUT.Add(TimeSpan.Parse("08:45"));
                                    AMPM = PunchOUT > TimeSpan.Parse("12:00") ? "PM" : "AM";
                                    orgSpotlightListView.PunchOut = PunchIN < Eightfifteen ? "17:00 PM" : PunchOUT.ToString().Substring(0, PunchOUT.ToString().Length - 3) + " " + AMPM;

                                }
                                else
                                {
                                    orgSpotlightListView.PunchOut = "17:00 PM";
                                }
                                AMPM = PunchIN < TimeSpan.Parse("12:00") ? "AM" : "PM";

                                orgSpotlightListView.PunchIn = PunchIN.ToString().Substring(0, PunchIN.ToString().Length - 3) + " " + AMPM;

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        //orgSpotlightListView.PunchIn=ex.Message.ToString();
                    }
                    ///////////////////Birthday//////////////////////////
                    try
                    {
                        var BirthdayAnniversaryURL = _configuration["BirthdayAnniversary:Birthday"] + "?date=" + DateTime.Now.ToString("yyyy-MM-dd");

                        var requestBirthday = new HttpRequestMessage(HttpMethod.Get, BirthdayAnniversaryURL);

                        var CredentialsBirthday = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("SobhaAPI" + ":" + "Sdl23@D365"));

                        requestBirthday.Headers.Add("Authorization", "Basic " + CredentialsBirthday);

                        var responsebirthday = await httpClient.SendAsync(requestBirthday);
                        if (responsebirthday.IsSuccessStatusCode)
                        {
                            var birthdayanniversaryResponse = await responsebirthday.Content.ReadAsStringAsync();
                            JsonNode data = JsonNode.Parse(birthdayanniversaryResponse);

                            if (data.ToJsonString() != "[]")
                            {
                                var jsonData = JsonConvert.DeserializeObject<dynamic>(data.ToString());
                                orgSpotlightListView.Birthday = new List<dynamic>();
                                foreach (var datajson in jsonData)
                                {
                                    orgSpotlightListView.Birthday.Add(datajson.empCode+","+datajson.empName+"," + datajson.design+ "," + datajson.deptname+ "," + datajson.emoffemid);

                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    { }
///////////////////Anniversary//////////////////////////

try
{
    var AnniversaryURL = _configuration["BirthdayAnniversary:Anniversary"] + "?date=" + DateTime.Now.ToString("yyyy-MM-dd");

    var requestAnniversary = new HttpRequestMessage(HttpMethod.Get, AnniversaryURL);

    var CredentialsAnniversary = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("SobhaAPI" + ":" + "Sdl23@D365"));

    requestAnniversary.Headers.Add("Authorization", "Basic " + CredentialsAnniversary);
    var responseanniversary = await httpClient.SendAsync(requestAnniversary);
    if (responseanniversary.IsSuccessStatusCode)
    {
        var birthdayanniversaryResponse = await responseanniversary.Content.ReadAsStringAsync();
        JsonNode data = JsonNode.Parse(birthdayanniversaryResponse);

        if (data.ToJsonString() != "[]")
        {
            var jsonData = JsonConvert.DeserializeObject<dynamic>(data.ToString());
           // orgSpotlightListView.Anniversary = new List<KeyValuePair<string, string>>();
            orgSpotlightListView.Anniversary = new List<dynamic>();
                                foreach (var datajson in jsonData)
                                {
                                    string empname = datajson.empName;
                                    string years = datajson.years;
                                    // orgSpotlightListView.Anniversary.Add(new KeyValuePair<string, string>(empname, years));
                                    orgSpotlightListView.Anniversary.Add(datajson.empName + "," + datajson.years + "," + datajson.design + "," + datajson.deptname + "," + datajson.emoffemid);

                                }
        }
    }
}
catch (Exception ex)
{ }

///////P&IT HelpDesk Without login///////////////// 

CCACrypto cc = new CCACrypto();
string encUser = cc.Encrypt(useremailID, "www.sobha.com");
orgSpotlightListView.PITHelpDesk = _configuration["QuickLinkURL:P&IT Help Desk"] + encUser;
//Admin HelpDesk
orgSpotlightListView.AdministrationHelpDesk = _configuration["QuickLinkURL:Administration Help Desk"] + encUser;
//IdeaSpace
orgSpotlightListView.IdeaSpaceApplication = _configuration["QuickLinkURL:Idea Space Application"] + encUser;
//Internal Audit
orgSpotlightListView.AuditManagementSystem = _configuration["QuickLinkURL:Audit Management System"] + encUser;


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
var Topthreespotlight = item.value.OrderByDescending(a => a.createdDateTime).Take(3);

foreach (var itemval in Topthreespotlight)
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
        status = true;
    }
}

}
}
}

foreach (var item in orgSpotlightListView.OrgUpdateLists)
{

if (item.value.Count > 0)
{
var Topthreeorgupdate = item.value.OrderByDescending(a => a.createdDateTime).Take(3);

foreach (var itemval in Topthreeorgupdate)
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
        status = true;
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
        private string getUserforJavaDecryption(string useremailID)
        {
            string encryUser = "";
            try
            {

                for (int i = 0; i < useremailID.Length; i++)
                {
                    encryUser += (char)(useremailID[i] + 1);
                }
                return encryUser;
            }

            catch (Exception ex)
            {
                string strex = ex.Message.ToString();
            }

            return encryUser;
        }
        }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;

namespace Sobha_Application.Models
{
    public class SharePointList
    {
        [JsonProperty(PropertyName = "@odata.context")]
        public string odataContext { get; set; }
        public string Username { get; set; }
        public string UserPhoto { get; set; }
        public string UserJobTitle { get; set; }
        public string PunchIN { get;set; }
        public string PunchOut { get; set; }
        public List<value> value { get; set; }
    }
    public class OrgSpotlightListView
    {
        public List<SharePointList> SpotLightsLists { get; set; }
        public List<SharePointList> OrgUpdateLists { get; set; }
        public string Username { get; set; }
        public string UserPhoto { get; set; }
        public string UserJobTitle { get; set; }
        public string EmoloyeeSelfService { get; set; }
        public string WorldClient { get; set; }
        public string AdministrationHelpDesk { get; set; }
        public string AuditManagementSystem { get; set; }
        public string ClubHouseApplication { get; set; }
        public string CustomerCareCellApplication { get; set; }
        public string DocumentManagementSystemApplication { get; set; }
        public string DocumentManagementSystemApplicationforCRM { get; set; }
        public string IdeaSpaceApplication { get; set; }
        public string ProjectClosureMaintenanceApplication { get; set; }
        public string QualitySafetyTechnologyHomePage { get; set; }
        public string SafetyReportingApplication { get; set; }
        public string SobhaTechnologyManual { get; set; }
        public string DepartmentPolicies { get; set; }
        public string PITHelpDesk { get; set; }
        public string PunchIn { get;set; }
        public string PunchOut { get;set;}

    }

}

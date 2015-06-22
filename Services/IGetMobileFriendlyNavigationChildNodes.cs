using System.Collections.Generic;
using MrCMS.Entities.Documents.Web;
using MrCMS.Web.Apps.MobileFriendlyNavigation.Models.MobileFriendlyNavigation;

namespace MrCMS.Web.Apps.MobileFriendlyNavigation.Services
{
    public interface IGetMobileFriendlyNavigationChildNodes
    {
        Dictionary<Webpage, List<MobileFriendlyNavigationChildNode>> GetNodes(IEnumerable<Webpage> parents);
    }
}
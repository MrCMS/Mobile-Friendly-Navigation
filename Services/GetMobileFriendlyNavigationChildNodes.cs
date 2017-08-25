using System.Collections.Generic;
using System.Linq;
using Microsoft.Ajax.Utilities;
using MrCMS.Entities.Documents.Web;
using MrCMS.Web.Apps.MobileFriendlyNavigation.Models.MobileFriendlyNavigation;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;

namespace MrCMS.Web.Apps.MobileFriendlyNavigation.Services
{
    public class GetMobileFriendlyNavigationChildNodes : IGetMobileFriendlyNavigationChildNodes
    {
        private readonly ISession _session;

        public GetMobileFriendlyNavigationChildNodes(ISession session)
        {
            _session = session;
        }

        public Dictionary<Webpage, List<MobileFriendlyNavigationChildNode>> GetNodes(IEnumerable<Webpage> parents)
        {
            var parentIds = parents.Select(parent => parent.Id).ToList();
            var parentIdsString = string.Join(",", parentIds);
            var query =
                "select doc.Id as Id, doc.ParentId as ParentId, doc.Name as Name, case when " +
                "doc.RedirectUrl<> '' then doc.RedirectUrl else doc.UrlSegment end as UrlSegment, " +
                "doc.PublishOn, doc.DocumentType, doc.DisplayOrder," +
                "(Select count(*) as Id From Document Where DocumentType not in ('MrCMS.Entities.Documents.Media.MediaCategory', 'MrCMS.Entities.Documents.Media.MediaFile', 'MrCMS.Entities.Documents.Layout.Layout') AND IsDeleted = 0 AND SiteId = 1 AND Published = 1 AND RevealInNavigation = 1 AND ParentId = doc.Id) as ChildCount" +
                " FROM Document doc " +
                "WHERE DocumentType not in ('MrCMS.Entities.Documents.Media.MediaCategory', 'MrCMS.Entities.Documents.Media.MediaFile', 'MrCMS.Entities.Documents.Layout.Layout') " +
                "AND ParentId IN " +
                "(" + parentIdsString + ")" +
                " AND IsDeleted = 0 and Published = 1 and RevealInNavigation = 1 AND SiteID = 1--pass this in also " +
                "ORDER BY DisplayOrder ASC";

            var nodes = _session.CreateSQLQuery(query)
                .SetResultTransformer(Transformers.AliasToBean<MobileFriendlyNavigationChildNode>())
                .List<MobileFriendlyNavigationChildNode>().GroupBy(node => node.ParentId)
                .ToDictionary(grouping => grouping.Key, g => g.ToList());

            return parents.ToDictionary(webpage => webpage,
                webpage =>
                    nodes.ContainsKey(webpage.Id) ? nodes[webpage.Id] : new List<MobileFriendlyNavigationChildNode>());
        }
    }
}
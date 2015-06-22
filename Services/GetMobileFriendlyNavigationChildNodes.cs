using System.Collections.Generic;
using System.Linq;
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
            Webpage webpageAlias = null;
            MobileFriendlyNavigationChildNode nodeAlias = null;

            var countSubNodes = QueryOver.Of<Webpage>()
                .Where(x => x.Parent.Id == webpageAlias.Id && x.RevealInNavigation && x.PublishOn != null)
                .ToRowCountQuery();

            var parentIds = parents.Select(webpage => webpage.Id).ToList();
            var nodes = _session.QueryOver(() => webpageAlias)
                .Where(node => node.RevealInNavigation && node.Published)
                .Where(node => node.Parent.Id.IsIn(parentIds))
                .OrderBy(x => x.DisplayOrder).Asc
                .SelectList(x => x.Select(y => y.Id).WithAlias(() => nodeAlias.Id)
                    .Select(y => y.Parent.Id).WithAlias(() => nodeAlias.ParentId)
                    .Select(y => y.Name).WithAlias(() => nodeAlias.Name)
                    .Select(y => y.UrlSegment).WithAlias(() => nodeAlias.UrlSegment)
                    .Select(y => y.PublishOn).WithAlias(() => nodeAlias.PublishOn)
                    .Select(y => y.DocumentType).WithAlias(() => nodeAlias.DocumentType)
                    .Select(y => y.DisplayOrder).WithAlias(() => nodeAlias.DisplayOrder)
                    .SelectSubQuery(countSubNodes).WithAlias(() => nodeAlias.ChildCount))
                .TransformUsing(Transformers.AliasToBean<MobileFriendlyNavigationChildNode>())
                .List<MobileFriendlyNavigationChildNode>().ToList()
                .GroupBy(node => node.ParentId)
                .ToDictionary(grouping => grouping.Key, g => g.ToList());
            return parents.ToDictionary(webpage => webpage,
                webpage =>
                    nodes.ContainsKey(webpage.Id) ? nodes[webpage.Id] : new List<MobileFriendlyNavigationChildNode>());
        }
    }
}
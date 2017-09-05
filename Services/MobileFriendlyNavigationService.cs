using System;
using System.Collections.Generic;
using System.Linq;
using MrCMS.Entities.Documents.Web;
using MrCMS.Entities.Multisite;
using MrCMS.Web.Apps.MobileFriendlyNavigation.Models.MobileFriendlyNavigation;
using MrCMS.Website.Caching;
using NHibernate;
using StackExchange.Profiling;

namespace MrCMS.Web.Apps.MobileFriendlyNavigation.Services
{
    public class MobileFriendlyNavigationService : IMobileFriendlyNavigationService
    {
        private readonly ISession _session;
        private readonly IProcessRootNodes _processRootNodes;
        private readonly IProcessChildNodes _processChildNodes;
        private readonly ICacheManager _cacheManager;
        private readonly Site _site;
        private readonly IGetMobileFriendlyNavigationChildNodes _getMobileFriendlyNavigationChildNodes;

        public MobileFriendlyNavigationService(ISession session, IProcessRootNodes processRootNodes,
            IProcessChildNodes processChildNodes, ICacheManager cacheManager, Site site, IGetMobileFriendlyNavigationChildNodes getMobileFriendlyNavigationChildNodes)
        {
            _session = session;
            _processRootNodes = processRootNodes;
            _processChildNodes = processChildNodes;
            _cacheManager = cacheManager;
            _site = site;
            _getMobileFriendlyNavigationChildNodes = getMobileFriendlyNavigationChildNodes;
        }

        public List<MobileFriendlyNavigationRootNode> GetRootNodes(Webpage rootWebpage)
        {
            using (MiniProfiler.Current.Step("Get Root Nodes"))
            {
                return _cacheManager.Get(
                    string.Format("mobile-friendly-nav.{0}.{1}", _site.Id, (rootWebpage == null ? "0" : rootWebpage.Id.ToString())), () =>
                    {
                        var rootNodes = _session.QueryOver<Webpage>()
                            .Where(
                                node => node.Parent == rootWebpage && node.RevealInNavigation && node.Published)
                            .OrderBy(node => node.DisplayOrder).Asc
                            .Cacheable()
                            .List();

                        var mobileFriendlyNavigationChildNodes = _getMobileFriendlyNavigationChildNodes.GetNodes(rootNodes);
                        var mobileFriendlyNavigationRootNodes = rootNodes
                            .Select(root => new MobileFriendlyNavigationRootNode
                            {
                                Id = root.Id,
                                Name = root.Name,
                                UrlSegment = GetNavigationUrl(root),
                                Children = GetChildNodeTransforms(mobileFriendlyNavigationChildNodes, root),
                                DocumentType = root.GetType().FullName,
                                DisplayOrder = root.DisplayOrder
                            }).ToList();

                        mobileFriendlyNavigationRootNodes = _processRootNodes.Process(mobileFriendlyNavigationRootNodes);

                        return mobileFriendlyNavigationRootNodes.OrderBy(node => node.DisplayOrder).ToList();
                    }, TimeSpan.FromMinutes(5), CacheExpiryType.Absolute);
            }
        }

        private string GetNavigationUrl(Webpage webpage)
        {
            if (webpage is Redirect)
            {
                var redirect = (Redirect)webpage;
                if (string.IsNullOrWhiteSpace(redirect.RedirectUrl))
                    return redirect.UrlSegment;
                if (redirect.RedirectUrl.ToLower().Contains("http"))
                    return redirect.RedirectUrl;
                return "/" + redirect.RedirectUrl.TrimStart('/');
            }

            return webpage.UrlSegment;
        }

        public List<MobileFriendlyNavigationChildNode> GetChildNodes(Webpage parent)
        {
            var nodes = _getMobileFriendlyNavigationChildNodes.GetNodes(new List<Webpage> { parent });
            return GetChildNodeTransforms(nodes, parent);
        }

        private List<MobileFriendlyNavigationChildNode> GetChildNodeTransforms(Dictionary<Webpage, List<MobileFriendlyNavigationChildNode>> mobileFriendlyNavigationChildNodes, Webpage parent)
        {
            if (parent == null)
                return new List<MobileFriendlyNavigationChildNode>();
            var nodes = mobileFriendlyNavigationChildNodes[parent];
            if (nodes.Any() && !(parent is SitemapPlaceholder))
            {
                foreach (var node in nodes)
                    node.DisplayOrder = node.DisplayOrder + 1;
                nodes.Insert(0, new MobileFriendlyNavigationChildNode
                {
                    ChildCount = 0,
                    Id = parent.Id,
                    Name = parent.Name,
                    ParentId = parent.Id,
                    PublishOn = parent.PublishOn,
                    UrlSegment = GetNavigationUrl(parent),
                    DocumentType = parent.GetType().FullName,
                    DisplayOrder = 0
                });
            }
            return _processChildNodes.Process(nodes, parent).OrderBy(node => node.DisplayOrder).ToList();
        }
    }
}
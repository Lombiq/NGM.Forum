﻿using System.Collections.Generic;
using System.Linq;
using NGM.Forum.Extensions;
using NGM.Forum.Models;
using Orchard;
using Orchard.Autoroute.Models;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;

namespace NGM.Forum.Services {
    public interface IThreadService : IDependency {
        ThreadPart Get(ForumPart forumPart, string slug, VersionOptions versionOptions);
        ContentItem Get(int id, VersionOptions versionOptions);
        IEnumerable<ThreadPart> Get(ForumPart forumPart);
        IEnumerable<ThreadPart> Get(ForumPart forumPart, VersionOptions versionOptions);
        IEnumerable<ThreadPart> Get(ForumPart forumPart, int skip, int count);
        IEnumerable<ThreadPart> Get(ForumPart forumPart, int skip, int count, VersionOptions versionOptions, ModerationOptions moderationOptions);
        int ThreadCount(ForumPart forumPart, VersionOptions versionOptions);
    }

    public class ThreadService : IThreadService {
        private readonly IContentManager _contentManager;

        public ThreadService(IContentManager contentManager) {
            _contentManager = contentManager;
        }

        public ThreadPart Get(ForumPart forumPart, string slug, VersionOptions versionOptions) {
            return _contentManager
                .Query<ThreadPart, ThreadPartRecord>()
                .Join<AutoroutePartRecord>()
                .Where(r => r.DisplayAlias.EndsWith(slug))
                .Join<CommonPartRecord>()
                .Where(cr => cr.Container == forumPart.Record.ContentItemRecord)
                .List()
                .FirstOrDefault();
        }

        public ContentItem Get(int id, VersionOptions versionOptions) {
            return _contentManager.Get(id, versionOptions);
        }

        public IEnumerable<ThreadPart> Get(ForumPart forumPart) {
            return Get(forumPart, VersionOptions.Published);
        }

        public IEnumerable<ThreadPart> Get(ForumPart forumPart, VersionOptions versionOptions) {
            return GetForumQuery(forumPart, versionOptions, ModerationOptions.All)
                .OrderByDescending(cr => cr.CreatedUtc)
                .ForPart<ThreadPart>()
                .List();
        }

        public IEnumerable<ThreadPart> Get(ForumPart forumPart, int skip, int count) {
            return Get(forumPart, skip, count, VersionOptions.Published, ModerationOptions.All);
        }

        // The order by on this record needs to be revisited.
        public IEnumerable<ThreadPart> Get(ForumPart forumPart, int skip, int count, VersionOptions versionOptions, ModerationOptions moderationOptions) {
            return GetForumQuery(forumPart, versionOptions, moderationOptions)
                .Join<ThreadPartRecord>()
                .OrderByDescending(t => t.IsSticky)
                .Join<CommonPartRecord>()
                .OrderByDescending(cr => cr.CreatedUtc)
                .ForPart<ThreadPart>()
                .Slice(skip, count);
        }

        public int ThreadCount(ForumPart forumPart, VersionOptions versionOptions) {
            return GetForumQuery(forumPart, versionOptions, ModerationOptions.All).Count();
        }

        private IContentQuery<ContentItem, CommonPartRecord> GetForumQuery(ContentPart<ForumPartRecord> forum, VersionOptions versionOptions, ModerationOptions moderationOptions) {
            var query = _contentManager.Query(versionOptions, Constants.Parts.Thread);

            if (!Equals(moderationOptions, ModerationOptions.All)) {
                query = query.Join<ModerationPartRecord>().Where(trd => trd.Approved == moderationOptions.IsApproved);
            }

            return query.Join<CommonPartRecord>().Where(
                cr => cr.Container == forum.Record.ContentItemRecord)
                .WithQueryHintsFor(Constants.Parts.Thread);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExtendedGiantBombClient.Model;
using GiantBomb.Api.Model;
using RestSharp.Portable;

namespace ExtendedGiantBombClient
{
    public partial class ExtendedGiantBombRestClient
    {
        public virtual IEnumerable<Video> SearchForVideos(string query, int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null) => 
            SearchForVideosAsync(query, page, pageSize, limitFields).Result;

        public virtual async Task<IEnumerable<Video>> SearchForVideosAsync(string query, int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null)
        {
            var result = await InternalSearchForVideos(query, page, pageSize, limitFields).ConfigureAwait(false);

            if (result.StatusCode == GiantBombBase.StatusOk)
                return result.Results;

            return null;
        }

        internal async Task<GiantBombResults<Video>> InternalSearchForVideos(string query, int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null)
        {
            var request = GetListResource("search", page, pageSize, limitFields);

            request.AddParameter("query", query);
            request.AddParameter("resources", "video");

            return await ExecuteAsync<GiantBombResults<Video>>(request).ConfigureAwait(false);
        }
    }

    public partial class ExtendedGiantBombRestClient
    {
        public virtual Video GetVideo(int id, string[] limitFields = null) =>
            GetVideoAsync(id, limitFields).Result;

        public virtual async Task<Video> GetVideoAsync(int id, string[] limitFields = null)
        {
            return await GetSingleResourceAsync<Video>("video", ExtendedResourceTypes.Videos, id, limitFields).ConfigureAwait(false);
        }

        public virtual IEnumerable<Video> GetVideos(int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null) => 
            GetVideosAsync(page, pageSize, limitFields).Result;

        public virtual async Task<IEnumerable<Video>> GetVideosAsync(int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null)
        {
            return await GetListResourceAsync<Video>("videos", page, pageSize, limitFields).ConfigureAwait(false);
        }

        public virtual IEnumerable<Video> GetAllVideos(string[] limitFields = null) =>
            GetAllVideosAsync(limitFields).Result;

        public virtual async Task<IEnumerable<Video>> GetAllVideosAsync(string[] limitFields = null)
        {
            var results = new List<Video>();
            var result = await InternalGetAllVideos(limitFields: limitFields).ConfigureAwait(false);

            if (result == null || result.StatusCode != GiantBombBase.StatusOk)
                return null;

            results.AddRange(result.Results);

            if (result.NumberOfTotalResults > result.Limit)
            {
                double remaining = Math.Ceiling(Convert.ToDouble(result.NumberOfTotalResults) / Convert.ToDouble(result.Limit));

                // Start on page 2
                for (var i = 2; i <= remaining; i++)
                {
                    result = await InternalGetAllVideos(i, result.Limit, limitFields).ConfigureAwait(false);

                    if (result.StatusCode != GiantBombBase.StatusOk)
                        break;

                    results.AddRange(result.Results);
                }
            }

            return results;
        }

        internal async Task<GiantBombResults<Video>> InternalGetAllVideos(int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null)
        {
            var request = GetListResource("videos", page, pageSize, limitFields);

            request.AddParameter("resources", "video");

            return await ExecuteAsync<GiantBombResults<Video>>(request).ConfigureAwait(false);
        }
    }
}

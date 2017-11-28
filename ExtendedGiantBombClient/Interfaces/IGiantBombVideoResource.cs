using System.Collections.Generic;
using System.Threading.Tasks;
using ExtendedGiantBombClient.Model;
using GiantBomb.Api.Model;

namespace ExtendedGiantBombClient.Interfaces
{
    public interface IGiantBombVideoResource
    {
        /// <summary>
        /// Gets a single video by id
        /// </summary>
        /// <param name="id">The Video's ID</param>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        Video GetVideo(int id, string[] limitFields = null);

        /// <summary>
        /// Gets a single video by id
        /// </summary>
        /// <param name="id">The Video's ID</param>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        Task<Video> GetVideoAsync(int id, string[] limitFields = null);

        /// <summary>
        /// Gets page of Videos
        /// </summary>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        IEnumerable<Video> GetVideos(int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null);

        /// <summary>
        /// Gets page of Videos
        /// </summary>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        Task<IEnumerable<Video>> GetVideosAsync(int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null);

        /// <summary>
        /// Gets all Videos
        /// </summary>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        IEnumerable<Video> GetAllVideos(string[] limitFields = null);

        /// <summary>
        /// Gets all Videos
        /// </summary>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        Task<IEnumerable<Video>> GetAllVideosAsync(string[] limitFields = null);

        /// <summary>
        /// Uses search api to find Videos by query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        IEnumerable<Video> SearchForVideos(string query, int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null);

        /// <summary>
        /// Uses search api to find Videos by query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="limitFields">List of field names to include in the response. Use this if you want to reduce the size of the response payload.</param>
        /// <returns></returns>
        Task<IEnumerable<Video>> SearchForVideosAsync(string query, int page = 1, int pageSize = GiantBombBase.DefaultLimit, string[] limitFields = null);

    }
}
using BooruSharp.Search.Post;
using System.Threading.Tasks;

namespace BooruSharp.Booru
{
    /// <summary>
    /// Danbooru.
    /// <para>https://danbooru.donmai.us/</para>
    /// </summary>
    public sealed class DanbooruDonmai : Template.Danbooru
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DanbooruDonmai"/> class.
        /// </summary>
        public DanbooruDonmai()
            : base("danbooru.donmai.us", BooruOptions.NoMoreThan2Tags)
        { }

        /// <inheritdoc/>
        public override bool IsSafe => false;

        public override Task<SearchResult[]> GetLastPostsAsync(int limit, int page, params string[] tagsArg)
        {
            return base.GetLastPostsAsync(limit, page, "page", tagsArg);
        }
    }
}

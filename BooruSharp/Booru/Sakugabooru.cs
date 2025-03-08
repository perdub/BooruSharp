using BooruSharp.Search.Post;
using System.Threading.Tasks;

namespace BooruSharp.Booru
{
    /// <summary>
    /// Sakugabooru.
    /// <para>https://www.sakugabooru.com/</para>
    /// </summary>
    public sealed class Sakugabooru : Template.Moebooru
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Sakugabooru"/> class.
        /// </summary>
        public Sakugabooru()
            : base("sakugabooru.com", BooruOptions.NoLastComments)
        { }

        /// <inheritdoc/>
        public override bool IsSafe => false;

        public override Task<SearchResult[]> GetLastPostsAsync(int limit, int page, params string[] tagsArg)
        {
            return base.GetLastPostsAsync(limit, page, "page", tagsArg);
        }
    }
}

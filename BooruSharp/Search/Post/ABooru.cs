﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace BooruSharp.Booru
{
    public abstract partial class ABooru
    {
        private const int _limitedTagsSearchCount = 2;
        private const int _increasedPostLimitCount = 20001;

        private string GetLimit(int quantity)
            => (_format == UrlFormat.Philomena || _format == UrlFormat.BooruOnRails ? "per_page=" : "limit=") + quantity;

        /// <summary>
        /// Searches for a post using its MD5 hash.
        /// </summary>
        /// <param name="md5">The MD5 hash of the post to search.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="Search.FeatureUnavailable"/>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        public virtual async Task<Search.Post.SearchResult> GetPostByMd5Async(string md5)
        {
            if (!HasPostByMd5API)
                throw new Search.FeatureUnavailable();

            if (md5 == null)
                throw new ArgumentNullException(nameof(md5));

            return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), "md5=" + md5));
        }

        /// <summary>
        /// Searches for a post using its ID.
        /// </summary>
        /// <param name="id">The ID of the post to search.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Search.FeatureUnavailable"/>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        public virtual async Task<Search.Post.SearchResult> GetPostByIdAsync(int id)
        {
            if (!HasPostByIdAPI)
                throw new Search.FeatureUnavailable();

            if (_format == UrlFormat.Danbooru) return await GetSearchResultFromUrlAsync(BaseUrl + "posts/" + id + ".json");
            if (_format == UrlFormat.Philomena) return await GetSearchResultFromUrlAsync($"{BaseUrl}api/v1/json/images/{id}");
            if (_format == UrlFormat.BooruOnRails) return await GetSearchResultFromUrlAsync($"{BaseUrl}api/v3/posts/{id}");
            if (_format == UrlFormat.PostIndexJson) return await GetSearchResultFromUrlAsync(_imageUrl + "?tags=id:" + id);
            return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), "id=" + id));
        }

        /// <summary>
        /// Gets the total number of available posts. If <paramref name="tagsArg"/> array is specified
        /// and isn't empty, the total number of posts containing these tags will be returned.
        /// </summary>
        /// <param name="tagsArg">The optional array of tags.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Search.FeatureUnavailable"/>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        /// <exception cref="Search.TooManyTags"/>
        public virtual async Task<int> GetPostCountAsync(params string[] tagsArg)
        {
            if (!HasPostCountAPI)
                throw new Search.FeatureUnavailable();

            string[] tags = tagsArg != null
                ? tagsArg.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                : Array.Empty<string>();

            if (NoMoreThanTwoTags && tags.Length > _limitedTagsSearchCount)
                throw new Search.TooManyTags();

            if (_format == UrlFormat.Philomena || _format == UrlFormat.BooruOnRails)
            {
                var url = CreateUrl(_imageUrl, GetLimit(1), TagsToString(tags));
                var json = await GetJsonAsync(url);
                var token = (JToken)JsonConvert.DeserializeObject(json);
                return token["total"].Value<int>();
            }
            else
            {
                var url = CreateUrl(_imageUrlXml, GetLimit(1), TagsToString(tags));
                XmlDocument xml = await GetXmlAsync(url);
                return int.Parse(xml.ChildNodes.Item(1).Attributes[0].InnerXml);
            }
        }

        /// <summary>
        /// Searches for a random post. If <paramref name="tagsArg"/> array is specified
        /// and isn't empty, random post containing those tags will be returned.
        /// </summary>
        /// <param name="tagsArg">The optional array of tags that must be contained in the post.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        /// <exception cref="Search.TooManyTags"/>
        public virtual async Task<Search.Post.SearchResult> GetRandomPostAsync(params string[] tagsArg)
        {
            string[] tags = tagsArg != null
                ? tagsArg.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                : Array.Empty<string>();

            if (NoMoreThanTwoTags && tags.Length > _limitedTagsSearchCount)
                throw new Search.TooManyTags();

            string tagString = TagsToString(tags);

            if (_format == UrlFormat.IndexPhp)
            {
                if (this is Template.Gelbooru)
                    return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), tagString) + "+sort:random");

                if (tags.Length == 0)
                {
                    // We need to request /index.php?page=post&s=random and get the id given by the redirect
                    string id = await GetRandomIdAsync(tagString);
                    return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), "id=" + id));
                }

                // The previous option doesn't work if there are tags so we contact the XML endpoint to get post count
                Uri url = CreateUrl(_imageUrlXml, GetLimit(1), tagString);
                XmlDocument xml = await GetXmlAsync(url);
                int max = int.Parse(xml.ChildNodes.Item(1).Attributes[0].InnerXml);

                if (max == 0)
                    throw new Search.InvalidTags();

                if (SearchIncreasedPostLimit && max > _increasedPostLimitCount)
                    max = _increasedPostLimitCount;

                return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), tagString, "pid=" + Random.Next(0, max)));
            }
            if (_format == UrlFormat.Philomena || _format == UrlFormat.BooruOnRails)
            {
                return await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), tagString, "sf=random"));
            }

            return NoMoreThanTwoTags
                // +order:random count as a tag so we use random=true instead to save one
                ? await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), tagString, "random=true"))
                : await GetSearchResultFromUrlAsync(CreateUrl(_imageUrl, GetLimit(1), tagString) + "+order:random");
        }

        /// <summary>
        /// Searches for multiple random posts. If <paramref name="tagsArg"/> array is
        /// specified and isn't empty, random posts containing those tags will be returned.
        /// </summary>
        /// <param name="limit">The number of posts to get.</param>
        /// <param name="tagsArg">The optional array of tags that must be contained in the posts.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="Search.FeatureUnavailable"/>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        /// <exception cref="Search.TooManyTags"/>
        public virtual async Task<Search.Post.SearchResult[]> GetRandomPostsAsync(int limit, params string[] tagsArg)
        {
            if (!HasMultipleRandomAPI)
                throw new Search.FeatureUnavailable();

            string[] tags = tagsArg != null
                ? tagsArg.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray()
                : Array.Empty<string>();

            if (NoMoreThanTwoTags && tags.Length > _limitedTagsSearchCount)
                throw new Search.TooManyTags();

            string tagString = TagsToString(tags);

            if (_format == UrlFormat.IndexPhp)
                return await GetSearchResultsFromUrlAsync(CreateUrl(_imageUrl, GetLimit(limit), tagString) + "+sort:random");
            if (_format == UrlFormat.Philomena || _format == UrlFormat.BooruOnRails)
                return await GetSearchResultsFromUrlAsync(CreateUrl(_imageUrl, GetLimit(limit), tagString, "sf=random"));
            else if (NoMoreThanTwoTags)
                // +order:random count as a tag so we use random=true instead to save one
                return await GetSearchResultsFromUrlAsync(CreateUrl(_imageUrl, GetLimit(limit), tagString, "random=true"));
            else
                return await GetSearchResultsFromUrlAsync(CreateUrl(_imageUrl, GetLimit(limit), tagString) + "+order:random");
        }

        /// <summary>
        /// Gets the latest posts on the website. If <paramref name="tagsArg"/> array is
        /// specified and isn't empty, latest posts containing those tags will be returned.
        /// </summary>
        /// <param name="tagsArg">The optional array of tags that must be contained in the posts.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        public virtual async Task<Search.Post.SearchResult[]> GetLastPostsAsync(params string[] tagsArg)
        {
            return GetPostsSearchResult(JsonConvert.DeserializeObject(await GetJsonAsync(CreateUrl(_imageUrl, TagsToString(tagsArg)))));
        }



        /// <summary>
        /// Gets the latest posts on the website. If <paramref name="tagsArg"/> array is
        /// specified and isn't empty, latest posts containing those tags will be returned.
        /// </summary>
        /// <param name="limit">The number of posts to get.</param>
        /// <param name="tagsArg">The optional array of tags that must be contained in the posts.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        public virtual async Task<Search.Post.SearchResult[]> GetLastPostsAsync(int limit, params string[] tagsArg)
        {
            return GetPostsSearchResult(JsonConvert.DeserializeObject(await GetJsonAsync(CreateUrl(_imageUrl, "limit=" + limit, TagsToString(tagsArg)))));
        }

        /// <summary>
        /// Gets the latest posts on the website. If <paramref name="tagsArg"/> array is
        /// specified and isn't empty, latest posts containing those tags will be returned.
        /// </summary>
        /// <param name="limit">The number of posts to get.</param>
        /// <param name="tagsArg">The optional array of tags that must be contained in the posts.</param>
        /// <param name="page">The page number</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <exception cref="System.Net.Http.HttpRequestException"/>
        public virtual async Task<Search.Post.SearchResult[]> GetLastPostsAsync(int limit, int page, params string[] tagsArg)
        {
            return await GetLastPostsAsync(limit, page, "pid", tagsArg);
        }

        internal virtual async Task<Search.Post.SearchResult[]> GetLastPostsAsync(int limit, int page, string pageParamId, params string[] tagsArg)
        {
            return GetPostsSearchResult(JsonConvert.DeserializeObject(await GetJsonAsync(CreateUrl(_imageUrl, "limit=" + limit, pageParamId + "=" + page, TagsToString(tagsArg)))));
        }

        private async Task<Search.Post.SearchResult> GetSearchResultFromUrlAsync(string url)
        {
            return GetPostSearchResult(ParseFirstPostSearchResult(JsonConvert.DeserializeObject(await GetJsonAsync(url))));
        }

        private Task<Search.Post.SearchResult> GetSearchResultFromUrlAsync(Uri url)
        {
            return GetSearchResultFromUrlAsync(url.AbsoluteUri);
        }

        private async Task<Search.Post.SearchResult[]> GetSearchResultsFromUrlAsync(string url)
        {
            return GetPostsSearchResult(JsonConvert.DeserializeObject(await GetJsonAsync(url)));
        }

        private Task<Search.Post.SearchResult[]> GetSearchResultsFromUrlAsync(Uri url)
        {
            return GetSearchResultsFromUrlAsync(url.AbsoluteUri);
        }

        /// <summary>
        /// Converts a letter to its matching <see cref="Search.Post.Rating"/>.
        /// </summary>
        protected Search.Post.Rating GetRating(char c)
        {
            c = char.ToLower(c);
            switch (c)
            {
                case 'g': return Search.Post.Rating.General;
                case 's': return Search.Post.Rating.Safe;
                case 'q': return Search.Post.Rating.Questionable;
                case 'e': return Search.Post.Rating.Explicit;
                default: throw new ArgumentException($"Invalid rating '{c}'.", nameof(c));
            }
        }
    }
}

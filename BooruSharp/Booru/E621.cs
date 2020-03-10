﻿namespace BooruSharp.Booru
{
    public class E621 : Booru
    {
        public E621(BooruAuth auth = null) : base("beta.e621.net", auth, UrlFormat.danbooru, 750, BooruOptions.wikiSearchUseTitle, BooruOptions.noTagById)
        { }

        public override bool IsSafe()
            => false;
    }
}

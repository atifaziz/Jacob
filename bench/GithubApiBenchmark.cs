// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;

#pragma warning disable CA1050

/// Benchmarks that are based on a partial response from the Github API.
/// The benchmark is based on the response of merging a branch via the API.
/// https://docs.github.com/en/rest/branches/branches#merge-a-branch.
[MemoryDiagnoser]
public class GithubApiBenchmark
{
    const string TestData = /*lang=json,strict*/ """
        {
          "url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
          "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e",
          "node_id": "MDY6Q29tbWl0NmRjYjA5YjViNTc4NzVmMzM0ZjYxYWViZWQ2OTVlMmU0MTkzZGI1ZQ==",
          "html_url": "https://github.com/octocat/Hello-World/commit/6dcb09b5b57875f334f61aebed695e2e4193db5e",
          "comments_url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e/comments",
          "commit": {
            "url": "https://api.github.com/repos/octocat/Hello-World/git/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
            "author": {
              "name": "Monalisa Octocat",
              "email": "mona@github.com",
              "date": "2011-04-14T16:00:49Z"
            },
            "committer": {
              "name": "Monalisa Octocat",
              "email": "mona@github.com",
              "date": "2011-04-14T16:00:49Z"
            },
            "message": "Fix all the bugs",
            "tree": {
              "url": "https://api.github.com/repos/octocat/Hello-World/tree/6dcb09b5b57875f334f61aebed695e2e4193db5e",
              "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e"
            },
            "comment_count": 0,
            "verification": {
              "verified": false,
              "reason": "unsigned",
              "signature": null,
              "payload": null
            }
          },
          "author": {
            "login": "octocat",
            "id": 1,
            "node_id": "MDQ6VXNlcjE=",
            "avatar_url": "https://github.com/images/error/octocat_happy.gif",
            "gravatar_id": "",
            "url": "https://api.github.com/users/octocat",
            "html_url": "https://github.com/octocat",
            "followers_url": "https://api.github.com/users/octocat/followers",
            "following_url": "https://api.github.com/users/octocat/following{/other_user}",
            "gists_url": "https://api.github.com/users/octocat/gists{/gist_id}",
            "starred_url": "https://api.github.com/users/octocat/starred{/owner}{/repo}",
            "subscriptions_url": "https://api.github.com/users/octocat/subscriptions",
            "organizations_url": "https://api.github.com/users/octocat/orgs",
            "repos_url": "https://api.github.com/users/octocat/repos",
            "events_url": "https://api.github.com/users/octocat/events{/privacy}",
            "received_events_url": "https://api.github.com/users/octocat/received_events",
            "type": "User",
            "site_admin": false
          },
          "committer": {
            "login": "octocat",
            "id": 1,
            "node_id": "MDQ6VXNlcjE=",
            "avatar_url": "https://github.com/images/error/octocat_happy.gif",
            "gravatar_id": "",
            "url": "https://api.github.com/users/octocat",
            "html_url": "https://github.com/octocat",
            "followers_url": "https://api.github.com/users/octocat/followers",
            "following_url": "https://api.github.com/users/octocat/following{/other_user}",
            "gists_url": "https://api.github.com/users/octocat/gists{/gist_id}",
            "starred_url": "https://api.github.com/users/octocat/starred{/owner}{/repo}",
            "subscriptions_url": "https://api.github.com/users/octocat/subscriptions",
            "organizations_url": "https://api.github.com/users/octocat/orgs",
            "repos_url": "https://api.github.com/users/octocat/repos",
            "events_url": "https://api.github.com/users/octocat/events{/privacy}",
            "received_events_url": "https://api.github.com/users/octocat/received_events",
            "type": "User",
            "site_admin": false
          },
          "parents": [
            {
              "url": "https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e",
              "sha": "6dcb09b5b57875f334f61aebed695e2e4193db5e"
            }
          ],
          "stats": {
            "additions": 104,
            "deletions": 4,
            "total": 108
          },
          "files": [
            {
              "filename": "file1.txt",
              "additions": 10,
              "deletions": 2,
              "changes": 12,
              "status": "modified",
              "raw_url": "https://github.com/octocat/Hello-World/raw/7ca483543807a51b6079e54ac4cc392bc29ae284/file1.txt",
              "blob_url": "https://github.com/octocat/Hello-World/blob/7ca483543807a51b6079e54ac4cc392bc29ae284/file1.txt",
              "patch": "@@ -29,7 +29,7 @@\n....."
            }
          ]
        }
        """;

    private static readonly IJsonReader<Uri> UriReader =
        from s in JsonReader.String()
        select new Uri(s);

    private static readonly IJsonReader<CommitAuthor> CommitAuthorJsonReader =
        JsonReader.Object(JsonReader.Property("name", JsonReader.String()),
                          JsonReader.Property("email", JsonReader.String()),
                          JsonReader.Property("date", JsonReader.DateTime()),
                          (name, email, date) => new CommitAuthor(name, email, date));

    private static readonly IJsonReader<Tree> TreeReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("sha", JsonReader.String()),
                          (url, sha) => new Tree(url, sha));

    private static readonly IJsonReader<Verification> VerificationReader =
        JsonReader.Object(JsonReader.Property("verified", JsonReader.Boolean()),
                          JsonReader.Property("reason", JsonReader.String()),
                          JsonReader.Property("signature", JsonReader.String()),
                          JsonReader.Property("payload", JsonReader.String()),
                          (verified, reason, signature, payload) =>
                              new Verification(verified, reason, signature, payload));

    private static readonly IJsonReader<Commit> CommitJsonReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("author", CommitAuthorJsonReader),
                          JsonReader.Property("committer", CommitAuthorJsonReader),
                          JsonReader.Property("message", JsonReader.String()),
                          JsonReader.Property("tree", TreeReader),
                          JsonReader.Property("comment_count", JsonReader.Int32()),
                          JsonReader.Property("verification", VerificationReader),
                          (url, author, committer, message, tree, commentCount, verification) =>
                              new Commit(url, author, committer, message, tree, commentCount, verification));

    private static readonly IJsonReader<Author> AuthorJsonReader =
        JsonReader.Object(JsonReader.Property("login", JsonReader.String()),
                          JsonReader.Property("id", JsonReader.Int32()),
                          JsonReader.Property("node_id", JsonReader.String()),
                          JsonReader.Property("avatar_url", UriReader),
                          JsonReader.Property("gravatar_id", JsonReader.String()),
                          JsonReader.Property("url", UriReader),
                          JsonReader.Property("html_url", UriReader),
                          JsonReader.Property("followers_url", UriReader),
                          JsonReader.Property("following_url", UriReader),
                          JsonReader.Property("gists_url", UriReader),
                          JsonReader.Property("starred_url", UriReader),
                          JsonReader.Property("subscriptions_url", UriReader),
                          JsonReader.Property("organizations_url", UriReader),
                          JsonReader.Property("repos_url", UriReader),
                          JsonReader.Property("events_url", UriReader),
                          JsonReader.Property("received_events_url", UriReader),
                          /*
                           * Omitted because JsonReader.Object supports 16 properties.
                           * JsonReader.Property("type", JsonReader.String()),
                           * JsonReader.Property("site_admin", JsonReader.Boolean()
                           */
                          (p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16) =>
                              new Author(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16));

    static readonly IJsonReader<MergeBranchResponse> MergeBranchResponseJsonReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("sha", JsonReader.String()),
                          JsonReader.Property("node_id", JsonReader.String()),
                          JsonReader.Property("html_url", UriReader),
                          JsonReader.Property("comments_url", UriReader),
                          JsonReader.Property("commit", CommitJsonReader),
                          JsonReader.Property("author", AuthorJsonReader),
                          JsonReader.Property("committer", AuthorJsonReader),
                          (url, sha, node_id, htmlUrl, commentsUrl, commit, author, committer) =>
                              new MergeBranchResponse(url, sha, node_id, htmlUrl, commentsUrl, commit, author, committer));

    [Params(10, 100, 1000, 10000)] public int ObjectCount { get; set; }

    byte[] jsonDataBytes = Array.Empty<byte>();

    [GlobalSetup]
    public void Setup()
    {
        var json = $"[{string.Join(",", Enumerable.Repeat(TestData, ObjectCount))}]";
        this.jsonDataBytes = Encoding.UTF8.GetBytes(json);
    }

    [Benchmark]
    public MergeBranchResponse[] JsonReaderBenchmark() =>
        JsonReader.Array(MergeBranchResponseJsonReader).Read(this.jsonDataBytes);

    [Benchmark(Baseline = true)]
    public MergeBranchResponse[] SystemTextJsonBenchmark() =>
        JsonSerializer.Deserialize<MergeBranchResponse[]>(this.jsonDataBytes)!;
}

public sealed record MergeBranchResponse([property: JsonPropertyName("url")] Uri Url,
                                         [property: JsonPropertyName("sha")] string Sha,
                                         [property: JsonPropertyName("node_id")] string NodeId,
                                         [property: JsonPropertyName("html_url")] Uri HtmlUrl,
                                         [property: JsonPropertyName("comments_url")] Uri CommentsUrl,
                                         [property: JsonPropertyName("commit")] Commit Commit,
                                         [property: JsonPropertyName("author")] Author Author,
                                         [property: JsonPropertyName("committer")] Author Committer);

public sealed record Commit([property: JsonPropertyName("url")] Uri Url,
                            [property: JsonPropertyName("author")] CommitAuthor Author,
                            [property: JsonPropertyName("committer")] CommitAuthor Committer,
                            [property: JsonPropertyName("message")] string Message,
                            [property: JsonPropertyName("tree")] Tree Tree,
                            [property: JsonPropertyName("comment_count")] int CommentCount,
                            [property: JsonPropertyName("verification")] Verification Verification);

public sealed record CommitAuthor([property: JsonPropertyName("name")] string Name,
                                  [property: JsonPropertyName("email")] string Email,
                                  [property: JsonPropertyName("date")] DateTime Date);

public sealed record Tree([property: JsonPropertyName("url")] Uri Url,
                          [property: JsonPropertyName("sha")] string Sha);

public sealed record Verification([property: JsonPropertyName("verified")] bool Verified,
                                  [property: JsonPropertyName("reason")] string Reason,
                                  [property: JsonPropertyName("signature")] string Signature,
                                  [property: JsonPropertyName("payload")] string Payload);

public sealed record Author([property: JsonPropertyName("login")] string Login,
                            [property: JsonPropertyName("id")] int Id,
                            [property: JsonPropertyName("node_id")] string NodeId,
                            [property: JsonPropertyName("avatar_url")] Uri AvatarUrl,
                            [property: JsonPropertyName("gravatar_id")] string GravatarId,
                            [property: JsonPropertyName("url")] Uri Url,
                            [property: JsonPropertyName("html_url")] Uri HtmlUrl,
                            [property: JsonPropertyName("followers_url")] Uri FollowersUrl,
                            [property: JsonPropertyName("following_url")] Uri FollowingUrl,
                            [property: JsonPropertyName("gists_url")] Uri GistsUrl,
                            [property: JsonPropertyName("starred_url")] Uri StarredUrl,
                            [property: JsonPropertyName("subscriptions_url")] Uri SubscriptionsUrl,
                            [property: JsonPropertyName("organizations_url")] Uri OrganizationsUrl,
                            [property: JsonPropertyName("repos_url")] Uri ReposUrl,
                            [property: JsonPropertyName("events_url")] Uri EventsUrl,
                            [property: JsonPropertyName("received_events_url")] Uri ReceivedEventsUrl
                            /*
                             * Omitted because JsonReader.Object supports 16 properties.
                             * [property: JsonPropertyName("type")] string Type,
                             * [property: JsonPropertyName("site_admin")] bool SiteAdmin
                             */);

// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
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
public class GitHubApiBenchmark
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

    static readonly IJsonReader<Uri> UriReader =
        from s in JsonReader.String()
        select new Uri(s);

    static readonly IJsonReader<CommitUser> CommitUserJsonReader =
        JsonReader.Object(JsonReader.Property("name", JsonReader.String()),
                          JsonReader.Property("email", JsonReader.String()),
                          JsonReader.Property("date", JsonReader.DateTime()),
                          (p1, p2, p3) => new CommitUser(p1, p2, p3));

    static readonly IJsonReader<Tree> TreeJsonReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("sha", JsonReader.String()),
                          (p1, p2) => new Tree(p1, p2));

    static readonly IJsonReader<Verification> VerificationJsonReader =
        JsonReader.Object(JsonReader.Property("verified", JsonReader.Boolean()),
                          JsonReader.Property("reason", JsonReader.String()),
                          JsonReader.Property("signature", JsonReader.String().OrNull()),
                          JsonReader.Property("payload", JsonReader.String().OrNull()),
                          (p1, p2, p3, p4) => new Verification(p1, p2, p3, p4));

    static readonly IJsonReader<Commit> CommitJsonReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("author", CommitUserJsonReader),
                          JsonReader.Property("committer", CommitUserJsonReader),
                          JsonReader.Property("message", JsonReader.String()),
                          JsonReader.Property("tree", TreeJsonReader),
                          JsonReader.Property("comment_count", JsonReader.Int32()),
                          JsonReader.Property("verification", VerificationJsonReader),
                          (p1, p2, p3, p4, p5, p6, p7) =>
                              new Commit(p1, p2, p3, p4, p5, p6, p7));

    static readonly IJsonReader<GitHubUser> GitHubUserJsonReader =
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
                              new GitHubUser(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14, p15, p16));

    static readonly IJsonReader<Stats> StatsJsonReader =
        JsonReader.Object(JsonReader.Property("additions", JsonReader.Int32()),
                          JsonReader.Property("deletions", JsonReader.Int32()),
                          JsonReader.Property("total", JsonReader.Int32()),
                          (p1, p2, p3) => new Stats(p1, p2, p3));

    static readonly IJsonReader<File> FileJsonReader =
        JsonReader.Object(JsonReader.Property("filename", JsonReader.String()),
                          JsonReader.Property("additions", JsonReader.Int32()),
                          JsonReader.Property("deletions", JsonReader.Int32()),
                          JsonReader.Property("changes", JsonReader.Int32()),
                          JsonReader.Property("status", JsonReader.String()),
                          JsonReader.Property("raw_url", UriReader),
                          JsonReader.Property("blob_url", UriReader),
                          JsonReader.Property("patch", JsonReader.String()),
                          (p1, p2, p3, p4, p5, p6, p7, p8) =>
                              new File(p1, p2, p3, p4, p5, p6, p7, p8));

    static readonly IJsonReader<MergeBranchResponse> MergeBranchResponseJsonReader =
        JsonReader.Object(JsonReader.Property("url", UriReader),
                          JsonReader.Property("sha", JsonReader.String()),
                          JsonReader.Property("node_id", JsonReader.String()),
                          JsonReader.Property("html_url", UriReader),
                          JsonReader.Property("comments_url", UriReader),
                          JsonReader.Property("commit", CommitJsonReader),
                          JsonReader.Property("author", GitHubUserJsonReader),
                          JsonReader.Property("committer", GitHubUserJsonReader),
                          JsonReader.Property("parents", ImmutableArrayReader(TreeJsonReader)),
                          JsonReader.Property("stats", StatsJsonReader),
                          JsonReader.Property("files", ImmutableArrayReader(FileJsonReader)),
                          (p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11) =>
                              new MergeBranchResponse(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11));

    static IJsonReader<ImmutableArray<T>> ImmutableArrayReader<T>(IJsonReader<T> reader) =>
        JsonReader.Array(reader, list => list.ToImmutableArray());

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
                                         [property: JsonPropertyName("author")] GitHubUser Author,
                                         [property: JsonPropertyName("committer")] GitHubUser Committer,
                                         [property: JsonPropertyName("parents")] ImmutableArray<Tree> Parents,
                                         [property: JsonPropertyName("stats")] Stats Stats,
                                         [property: JsonPropertyName("files")] ImmutableArray<File> Files);

public sealed record Commit([property: JsonPropertyName("url")] Uri Url,
                            [property: JsonPropertyName("author")] CommitUser Author,
                            [property: JsonPropertyName("committer")] CommitUser Committer,
                            [property: JsonPropertyName("message")] string Message,
                            [property: JsonPropertyName("tree")] Tree Tree,
                            [property: JsonPropertyName("comment_count")] int CommentCount,
                            [property: JsonPropertyName("verification")] Verification Verification);

public sealed record CommitUser([property: JsonPropertyName("name")] string Name,
                                [property: JsonPropertyName("email")] string Email,
                                [property: JsonPropertyName("date")] DateTime Date);

public sealed record Tree([property: JsonPropertyName("url")] Uri Url,
                          [property: JsonPropertyName("sha")] string Sha);

public sealed record Verification([property: JsonPropertyName("verified")] bool Verified,
                                  [property: JsonPropertyName("reason")] string Reason,
                                  [property: JsonPropertyName("signature")] string? Signature,
                                  [property: JsonPropertyName("payload")] string? Payload);

public sealed record GitHubUser([property: JsonPropertyName("login")] string Login,
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

public sealed record Stats([property: JsonPropertyName("additions")] int Additions,
                           [property: JsonPropertyName("deletions")] int Deletions,
                           [property: JsonPropertyName("total")] int Total);

public sealed record File([property: JsonPropertyName("filename")] string Filename,
                          [property: JsonPropertyName("additions")] int Additions,
                          [property: JsonPropertyName("deletions")] int Deletions,
                          [property: JsonPropertyName("changes")] int Changes,
                          [property: JsonPropertyName("status")] string Status,
                          [property: JsonPropertyName("raw_url")] Uri RawUrl,
                          [property: JsonPropertyName("blob_url")] Uri BlobUrl,
                          [property: JsonPropertyName("patch")] string Patch);

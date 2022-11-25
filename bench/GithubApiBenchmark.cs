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

    private static readonly IJsonReader<Author> AuthorJsonReader =
        JsonReader.Object(JsonReader.Property("name", JsonReader.String()),
                          JsonReader.Property("email", JsonReader.String()),
                          JsonReader.Property("date", JsonReader.DateTime()),
                          (name, email, date) => new Author(name, email, date));

    private static readonly IJsonReader<Commit> CommitJsonReader =
        JsonReader.Object(JsonReader.Property("url", from s in JsonReader.String() select new Uri(s)),
                          JsonReader.Property("author", AuthorJsonReader),
                          JsonReader.Property("committer", AuthorJsonReader),
                          JsonReader.Property("message", JsonReader.String()),
                          JsonReader.Property("comment_count", JsonReader.Int32()),
                          (url, author, committer, message, commentCount) =>
                              new Commit(url, author, committer, message, commentCount));

    static readonly IJsonReader<MergeBranchResponse> MergeBranchResponseJsonReader =
        JsonReader.Object(JsonReader.Property("url", from s in JsonReader.String() select new Uri(s)),
                          JsonReader.Property("sha", JsonReader.String()),
                          JsonReader.Property("commit", CommitJsonReader),
                          (url, sha, commit) => new MergeBranchResponse(url, sha, commit));

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
                                         [property: JsonPropertyName("commit")] Commit Commit);

public sealed record Commit([property: JsonPropertyName("url")] Uri Url,
                            [property: JsonPropertyName("author")] Author Author,
                            [property: JsonPropertyName("committer")] Author Committer,
                            [property: JsonPropertyName("message")] string Message,
                            [property: JsonPropertyName("comment_count")] int CommentCount);

public sealed record Author([property: JsonPropertyName("name")] string Name,
                            [property: JsonPropertyName("email")] string Email,
                            [property: JsonPropertyName("date")] DateTime Date);

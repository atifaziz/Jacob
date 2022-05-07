// Copyright (c) 2021 Atif Aziz.
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
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
    static readonly MergeBranchResponse TestData =
        new(new Uri("https://api.github.com/repos/octocat/Hello-World/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e"),
            "6dcb09b5b57875f334f61aebed695e2e4193db5e",
            new Commit(new Uri("https://api.github.com/repos/octocat/Hello-World/git/commits/6dcb09b5b57875f334f61aebed695e2e4193db5e"),
                       new Author("Monalisa Octocat", "mona@github.com",
                                  DateTime.Parse("2011-04-14T16:00:49Z",
                                                 CultureInfo.InvariantCulture)),
                       new Author("Monalisa Octocat", "mona@github.com",
                                  DateTime.Parse("2011-04-14T16:00:49Z",
                                                 CultureInfo.InvariantCulture)),
                       "fix all the bugs",
                       12));

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
        var json = JsonSerializer.Serialize(Enumerable.Repeat(TestData, ObjectCount));
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
                            [property: JsonPropertyName("committer")]
                            Author Committer,
                            [property: JsonPropertyName("message")]
                            string Message,
                            [property: JsonPropertyName("comment_count")]
                            int CommentCount);

public sealed record Author([property: JsonPropertyName("name")] string Name,
                            [property: JsonPropertyName("email")] string Email,
                            [property: JsonPropertyName("date")] DateTime Date);

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Prototype {
	internal static class Program {
		const int MaxPages = 3;
		
		static string GetApiKey() {
			if ( !File.Exists("api_key.txt") ) {
				Console.WriteLine("Can't find 'api_key.txt'");
				return null;
			}
			try {
				return File.ReadAllText("api_key.txt").Trim();
			} catch ( Exception e ) {
				Console.WriteLine($"Something went wrong while reading API key: {e}");
				return null;
			}
		}
		
		
		static void Main(string[] args) {
			var apiKey = GetApiKey();
			if ( !string.IsNullOrEmpty(apiKey) ) {
				Run(apiKey).Wait();
			}
		}

		static async Task Run(string apiKey) {
			var service = Initialize(apiKey);
			while ( true ) {
				Console.WriteLine("Write request (empty for exit):");
				var searchStr = Console.ReadLine();
				if ( string.IsNullOrEmpty(searchStr) ) {
					return;
				}
				Console.WriteLine("Send request...");
				var results = await ExecuteBatchSearch(service, searchStr);
				Console.WriteLine($"All results ({results.Count}):");
				foreach ( var item in results ) {
					var url = "https://www.youtube.com/watch?v=" + item.Id.VideoId;
					Console.WriteLine($"'{item.Snippet.ChannelTitle}': '{item.Snippet.Title}' => {url}");
				}
				Console.WriteLine();
			}
		}

		static async Task<List<SearchResult>> ExecuteBatchSearch(YouTubeService service, string searchStr) {
			var page       = 0;
			var pageToken  = string.Empty;
			var allResults = new List<SearchResult>();
			while ( true ) {
				var result = await SendSearchRequest(service, searchStr, pageToken);
				if ( result == null ) {
					Console.WriteLine("Null response.");
					break;
				}
				Console.WriteLine(
					$"Result: page: {page}, total results: {result.PageInfo.TotalResults}, " +
					$"items: {result.Items.Count}, next page token: {result.NextPageToken}");
				if ( result.Items.Count > 0 ) {
					allResults.AddRange(result.Items);
					pageToken = result.NextPageToken;
					if ( string.IsNullOrEmpty(pageToken) ) {
						break;
					}
					page++;
					if ( page >= MaxPages ) {
						break;
					}
				} else {
					break;
				}
			}
			return allResults;
		}

		static async Task<SearchListResponse> SendSearchRequest(YouTubeService service, string searchStr, string pageToken) {
			try {
				var request = service.Search.List("snippet");
				request.Q = searchStr;
				request.Type = "video";
				request.MaxResults = 50;
				request.PageToken = pageToken;
				return await request.ExecuteAsync();
			} catch ( Exception e ) {
				Console.WriteLine($"Something went wrong while sending request: {e}");
				return null;
			}
		}
		
		static YouTubeService Initialize(string apiKey) {
			Console.WriteLine($"Initialize with apiKey: '{apiKey}'");
			var initializer = new BaseClientService.Initializer() {
				ApplicationName = "TestApp",
				ApiKey          = apiKey,
			};
			return new YouTubeService(initializer);
		}
	}
}
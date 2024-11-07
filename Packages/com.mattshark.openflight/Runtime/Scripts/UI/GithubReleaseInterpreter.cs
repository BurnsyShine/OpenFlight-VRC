﻿/**
 * @ Maintainer: Happyrobot33
 */

using UdonSharp;
using VRC.SDKBase;
using VRC.SDK3.StringLoading;
using VRC.SDK3.Data;

namespace OpenFlightVRC
{
	/// <summary>
	/// Interprets the Github release json from the OpenFlight-VRC repository
	/// </summary>
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class GithubReleaseInterpreter : LoggableUdonSharpBehaviour
	{
		private VRCUrl URL = new VRCUrl("https://api.github.com/repos/Mattshark89/OpenFlight-VRC/releases?per_page=20");
		public OpenFlight OF;
		public AvatarListLoader AvatarListLoader;
		private DataDictionary[] _releases;

		[ReadOnlyInspector]
		public string outputText = "";
		[ReadOnlyInspector]
		public bool onLatestRelease = false;
		[ReadOnlyInspector]
		public string releasesBehind = "0";
		[ReadOnlyInspector]
		public string latestReleaseVersion = "?.?.?";

		void Start()
		{
			_logCategory = nameof(GithubReleaseInterpreter);

			//subscribe to the avatar list loader callback
			AvatarListLoader.AddCallback(AvatarListLoaderCallback.AvatarListLoaded, this, nameof(LoadURL));
		}

		public void LoadURL()
		{
			Log(LogLevel.Info, "Loading Github Releases URL...");
			VRCStringDownloader.LoadUrl(URL, (VRC.Udon.Common.Interfaces.IUdonEventReceiver)this);
		}

		public override void OnStringLoadSuccess(IVRCStringDownload data)
		{
			string result = data.Result;
			Log(LogLevel.Info, "Loaded Github Releases URL!");

			//deserialize
			bool success = VRCJson.TryDeserializeFromJson(result, out DataToken json);

			//parse into releases
			CollectReleases(json);
		}

		public override void OnStringLoadError(IVRCStringDownload data)
		{
			Log(LogLevel.Error, "Failed to load Github Releases URL!");
		}

		/// <summary>
		/// Collects all releases from a DataToken.
		/// </summary>
		/// <param name="json">The DataToken to collect releases from.</param>
		private void CollectReleases(DataToken json)
		{
			int releaseCount = json.DataList.Count;
			_releases = new DataDictionary[releaseCount];
			Log(LogLevel.Info, json.DataList.Count + " releases found.");

			for (int i = 0; i < releaseCount; i++)
			{
				json.DataList.TryGetValue(i, out DataToken release);
				_releases[i] = ParseRelease(release);
			}

			outputText = "";

			//check if the release we are on is a prerelease
			bool isPrerelease = IsPrerelease(OF.OpenFlightVersion.ToString());
			if (isPrerelease)
			{
				Log(LogLevel.Info, "World is on a prerelease");
			}

			//check if on latest release. If the world is on a prerelease, this compares to those. if it isnt, we want to find the latest release that isnt a prerelease
			//string releaseVersion = releases[0]["tag_name"].ToString();
			string releaseVersion = _releases[0]["tag_name"].ToString();
			for (int i = 0; i < _releases.Length; i++)
			{
				if (!isPrerelease && IsPrerelease(_releases[i]["tag_name"].ToString()))
				{
					continue;
				}
				releaseVersion = _releases[i]["tag_name"].ToString();
				break;
			}
			//remove "OpenFlight-"
			latestReleaseVersion = releaseVersion.Substring(11);
			onLatestRelease = latestReleaseVersion == OF.OpenFlightVersion.ToString();

			//check how many releases behind
			int behind = 0;
			bool foundRelease = false;

			//Format releases into output
			foreach (DataDictionary release in _releases)
			{
				//stop if we've reached the release the world is on
				if (release["tag_name"].ToString() == "OpenFlight-" + OF.OpenFlightVersion.ToString())
				{
					releasesBehind = behind.ToString();
					foundRelease = true;
					break;
				}

				//if the world is on a prerelease, show all prereleases aswell. Otherwise, skip prereleases
				if (!isPrerelease && IsPrerelease(release["tag_name"].ToString()))
				{
					Log(LogLevel.Info, "Skipping prerelease " + release["tag_name"].ToString());
					continue;
				}

				outputText += "# " + release["name"].ToString() + "\n";
				outputText += "###### Released on " + release["published_at"].ToString() + "\n";
				//outputText += RemoveMarkdown(release["body"].ToString()) + "\n\n";
				outputText += release["body"].ToString() + "\n\n";
				behind++;
			}

			outputText = outputText.MarkdownToRichText();

			//if we didn't find the release the world is on, set the releases behind to +
			if (!foundRelease)
			{
				releasesBehind = behind.ToString() + "+";
			}

			Log(LogLevel.Info, "Releases behind: " + releasesBehind);

			//If the world is on the latest release, set the output text to say so
			if (onLatestRelease)
			{
				outputText = "You are on the latest release!";
				Log(LogLevel.Info, "On latest release!");
			}
		}

		/// <summary>
		/// Checks if a release version is a prerelease.
		/// </summary>
		/// <param name="releaseVersion">The release version to check.</param>
		/// <returns>True if the release version is a prerelease, false otherwise.</returns>
		private bool IsPrerelease(string releaseVersion)
		{
			return releaseVersion.Contains("beta") || releaseVersion.Contains("alpha") || releaseVersion.Contains("rc");
		}

		/// <summary>
		/// Parses a release DataToken into a DataDictionary.
		/// </summary>
		/// <param name="json">The DataToken to parse.</param>
		/// <returns>The parsed DataDictionary.</returns>
		private DataDictionary ParseRelease(DataToken json)
		{
			DataDictionary releaseDict = json.DataDictionary;
			DataDictionary release = new DataDictionary();
			release.Add("tag_name", GetKeyAsString(releaseDict, "tag_name"));
			release.Add("name", GetKeyAsString(releaseDict, "name"));
			release.Add("draft", GetKeyAsString(releaseDict, "draft"));
			release.Add("prerelease", GetKeyAsString(releaseDict, "prerelease"));
			release.Add("created_at", GetKeyAsString(releaseDict, "created_at"));
			release.Add("published_at", GetKeyAsString(releaseDict, "published_at"));
			release.Add("body", GetKeyAsString(releaseDict, "body"));
			return release;
		}

		/// <summary>
		/// Gets a key from a DataDictionary as a string.
		/// </summary>
		/// <param name="dict">The DataDictionary to get the key from.</param>
		/// <param name="key">The key to get.</param>
		/// <returns>The key as a string.</returns>
		private string GetKeyAsString(DataDictionary dict, string key)
		{
			dict.TryGetValue(key, out DataToken token);
			switch (token.TokenType)
			{
				case TokenType.String:
					return token.String;
				case TokenType.Double:
					return token.Double.ToString();
				case TokenType.Boolean:
					return token.Boolean.ToString();
				default:
					return "";
			}
		}
/* 
		/// <summary>
		/// Removes all markdown from a string.
		/// </summary>
		/// <param name="markdown">The string to remove markdown from.</param>
		/// <returns>The string without markdown.</returns>
		private string RemoveMarkdown(string markdown)
		{
			//remove bold
			markdown = markdown.Replace("**", "");
			//remove italics
			markdown = markdown.Replace("*", "");
			//remove headers
			markdown = markdown.Replace("#", "");
			return markdown;
		} */
	}
}

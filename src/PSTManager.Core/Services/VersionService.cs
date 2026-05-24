using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PSTManager.Core.Enums;
using PSTManager.Core.Models;
using Semver;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PSTManager.Core.Services;

public class VersionService
{
    private readonly HttpClient _http;
    private readonly LogService _log;
    private const string PstRepo = "deafdudecomputers/PalworldSaveTools";
    private const string PstmRepo = "CyrixJD115/PST-Manager";
    private const string PstmVersionUrl = "https://raw.githubusercontent.com/CyrixJD115/PST-Manager/main/version.yaml";

    public VersionService(HttpClient http, LogService log)
    {
        _http = http;
        _log = log;
    }

    public async Task<PstVersion?> GetLatestStableVersionAsync()
    {
        var apiUrl = $"https://api.github.com/repos/{PstRepo}/releases";
        _log.Info($"GetLatestStableVersion: fetching {apiUrl}");

        try
        {
            var releases = await _http.GetFromJsonAsync<List<GitHubRelease>>(apiUrl);

            if (releases == null)
            {
                _log.Warn("GetLatestStableVersion: releases response was null");
                return null;
            }

            _log.Info($"GetLatestStableVersion: got {releases.Count} releases");

            foreach (var release in releases)
            {
                _log.Debug($"GetLatestStableVersion: checking tag={release.TagName ?? "(null)"}, prerelease={release.Prerelease}");

                if (release.Prerelease) continue;
                if (string.IsNullOrEmpty(release.TagName)) continue;
                if (release.TagName.Contains("-beta", StringComparison.OrdinalIgnoreCase)) continue;
                if (release.TagName.Contains("-pre", StringComparison.OrdinalIgnoreCase)) continue;
                if (release.TagName.Contains("-alpha", StringComparison.OrdinalIgnoreCase)) continue;

                var version = release.TagName.TrimStart('v');
                if (SemVersion.TryParse(version, SemVersionStyles.Strict, out _))
                {
                    _log.Info($"GetLatestStableVersion: found stable release {release.TagName}");
                    return new PstVersion
                    {
                        Tag = release.TagName,
                        VersionNumber = version,
                        Branch = BranchType.Stable
                    };
                }

                _log.Debug($"GetLatestStableVersion: tag {release.TagName} failed semver parse");
            }

            _log.Warn("GetLatestStableVersion: no stable release found");
            return null;
        }
        catch (Exception ex)
        {
            _log.Error("GetLatestStableVersion: failed", ex);
            return null;
        }
    }

    public string GetStableDownloadUrl()
    {
        var url = $"https://github.com/{PstRepo}/archive/refs/heads/main.zip";
        _log.Info($"GetStableDownloadUrl: {url}");
        return url;
    }

    public string GetBetaDownloadUrl()
    {
        var url = $"https://github.com/{PstRepo}/archive/refs/heads/beta.zip";
        _log.Info($"GetBetaDownloadUrl: {url}");
        return url;
    }

    public async Task<string?> GetLatestPstmVersionAsync()
    {
        _log.Info($"GetLatestPstmVersion: fetching {PstmVersionUrl}");

        try
        {
            var yaml = await _http.GetStringAsync(PstmVersionUrl);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var result = deserializer.Deserialize<VersionYaml>(yaml);
            var version = result?.Version?.Trim('"').TrimStart('v');
            _log.Info($"GetLatestPstmVersion: {version}");
            return version;
        }
        catch (Exception ex)
        {
            _log.Error("GetLatestPstmVersion: failed", ex);
            return null;
        }
    }

    public bool IsNewerVersion(string? current, string? latest)
    {
        if (string.IsNullOrEmpty(latest)) return false;
        if (string.IsNullOrEmpty(current)) return true;

        if (SemVersion.TryParse(current, SemVersionStyles.Strict, out var cur) &&
            SemVersion.TryParse(latest, SemVersionStyles.Strict, out var lat))
        {
            return lat.CompareSortOrderTo(cur) > 0;
        }

        return false;
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }
    }

    private class VersionYaml
    {
        public string? Version { get; set; }
    }
}

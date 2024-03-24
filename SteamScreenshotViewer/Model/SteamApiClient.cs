using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace SteamScreenshotViewer.Model;

public static partial class SteamApiClient
{
    private static HttpClient httpClient = new();

    /* request for name only are not possible
      * smallest possible response containing name is the packages filter
      * it is assumed that the first package is always "Buy <GameName>"
      * responses for this filter are about 500 Bytes (if only 1 package option is offered)
      * or around 1 KiloByte if multiple packages are offered
      * packages only include bundles and multi-copy sales (e.g. 4-pack) but not dlc's
      * For comparison using filters=basic returns anything from 4 - 12 KiloBytes.
      * For many games this means using 500 Bytes instead of ~6 KiloBytes ~~> 10 times less data transferred
      example response (counter strike):
        {
          "730": {
            "success": true,
            "data": {
              "packages": [
                329385,
                298963,
                54029
              ],
              "package_groups": [
                {
                  "name": "default",
                  "title": "Buy Counter-Strike 2",  <--- this is parsed for app name
                  "description": "",
                  "selection_text": "Select a purchase option",
                  "save_text": "",
                  "display_type": 0,
                  "is_recurring_subscription": "false",
                  "subs": [
                    {
                      "packageid": 298963,
                      "percent_savings_text": " ",
                      "percent_savings": 0,
                      "option_text": "Counter-Strike 2 - Free",
                      "option_description": "",
                      "can_get_free_license": "0",
                      "is_free_license": true,
                      "price_in_cents_with_discount": 0
                    },
                    {
                      "packageid": 54029,
                      "percent_savings_text": " ",
                      "percent_savings": 0,
                      "option_text": "Prime Status Upgrade - 14,29€",
                      "option_description": "",
                      "can_get_free_license": "0",
                      "is_free_license": false,
                      "price_in_cents_with_discount": 1429
                    }
                  ]
                }
              ]
            }
          }
        }
      */
    private const string BaseRequestFilterPackages = @"https://store.steampowered.com/api/appdetails?filters=packages";
    private const string BaseRequestFilterBasic = @"https://store.steampowered.com/api/appdetails?filters=basic";
    private const string ExtractNameFromPackageRegex = @"Buy\W(.*)";

    [GeneratedRegex(ExtractNameFromPackageRegex)]
    private static partial Regex MyRegex();

    private static string BuildRequestFilterPackages(string appId)
    {
        return $"{BaseRequestFilterPackages}&appids={appId}";
    }

    private static string BuildRequestFilterBasic(string appId)
    {
        return $"{BaseRequestFilterBasic}&appids={appId}";
    }

    public static async Task<(ISteamApp, ApiResponse)> GetAppNameAsync(ISteamApp app)
    {
        ApiResponse response = await GetAppNameAsync(app.Id);

        return (app, response);
    }

    public static async Task<ApiResponse> GetAppNameAsync(string appId)
    {
        ApiResponse response = await TryGetAppNameFromPackages(appId);

        if (response.ShouldRetry)
        {
            Console.WriteLine("attempting to resolve using filters=basic: " + appId);
            response = await TryGetAppNameFromBasic(appId);
        }

        return response;
    }


    private static bool TryGetDataNodeIfSuccess(string response, string appId,
        [NotNullWhen(true)] out JsonNode? dataNode)
    {
        JsonNode appNode = JsonNode.Parse(response)?[appId]
                           ?? throw new NullReferenceException("response json did not include app node");
        if (IsSuccessful(appNode))
        {
            dataNode = appNode["data"] ?? throw new NullReferenceException("response json did not contain data node");
            return true;
        }

        dataNode = null;
        return false;
    }

    private static async Task<ApiResponse> TryGetAppNameFromPackages(string appId)
    {
        try
        {
            string response = await GetResponseString(BuildRequestFilterPackages(appId));
            if (!TryGetDataNodeIfSuccess(response, appId, out JsonNode? dataNode))
            {
                // Success=false -> steamapi does not know that app id anymore
                // --> retrying with different filters makes no sense
                return ApiResponse.Failure(FailureCause.SteamApi);
            }

            JsonArray packages = ((JsonArray?)dataNode["package_groups"])
                                 ?? throw new NullReferenceException(
                                     "response json did not contain package_groups node");
            if (packages.Count == 0)
            {
                // no package options -> app free or no longer sold on steam
                // success was true meaning api does still have data on that id
                // -> retry with different filter
                return ApiResponse.Retry();
            }

            string buyGamePackageName = packages[0]?["title"]?.ToString()
                                        ?? throw new NullReferenceException(
                                            "response json did not include package info");
            Match match = MyRegex().Match(buyGamePackageName);
            if (!match.Success)
            {
                // retry with different filter
                return ApiResponse.Retry();
            }

            return ApiResponse.Success(match.Groups[1].Value);
        }
        catch (NullReferenceException e)
        {
            //TODO logging 
            // most likely failed to parse response json
            // -> retry with different filter
            return ApiResponse.Retry();
        }
        catch (HttpRequestException e)
        {
            //TODO logging
            // network issues
            return ApiResponse.Failure(FailureCause.Network);
        }
    }

    private static async Task<ApiResponse> TryGetAppNameFromBasic(string appId)
    {
        try
        {
            string response = await GetResponseString(BuildRequestFilterBasic(appId));
            if (!TryGetDataNodeIfSuccess(response, appId, out JsonNode? dataNode))
            {
                // response contains success = "false"
                return ApiResponse.Failure(FailureCause.SteamApi);
            }

            string name = dataNode["name"]?.ToString()
                          ?? throw new NullReferenceException("json response did not contain name");
            return ApiResponse.Success(name);
        }
        catch (NullReferenceException e)
        {
            //TODO logging 
            // most likely failed to parse response json
            return ApiResponse.Failure(FailureCause.SteamApi);
        }
        catch (HttpRequestException e)
        {
            //TODO logging
            // network issues
            return ApiResponse.Failure(FailureCause.Network);
        }
    }

    private static bool IsSuccessful(JsonNode responseJson)
    {
        return responseJson["success"]!.ToString() == "true";
    }

    /// <summary/>
    /// <param name="request"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException">if any network related issue arises</exception>
    private static async Task<string> GetResponseString(string request)
    {
        string? response = null;
        try
        {
            response = await httpClient.GetStringAsync(request);
        }
        catch (TaskCanceledException timeout)
        {
            throw new HttpRequestException("request failed due to timeout", timeout);
        }

        return response;
    }
}
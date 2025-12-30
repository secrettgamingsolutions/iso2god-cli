using System;
using System.IO;
using System.Net;
using System.Text;

namespace Chilano.Iso2God
{
    /// <summary>
    /// Service to lookup Xbox 360 game titles by TitleID using XboxUnity API with CSV fallback
    /// </summary>
    internal class TitleLookupService
    {
        /// <summary>
        /// Lookup game title using XboxUnity API, with local CSV fallback
        /// </summary>
        public static string LookupTitleByTitleId(string titleId)
        {
            return LookupTitleByTitleId(titleId, IsoDetailsPlatform.Xbox360);
        }

        public static string LookupTitleByTitleId(string titleId, IsoDetailsPlatform platform)
        {
            if (string.IsNullOrEmpty(titleId))
            {
                return null;
            }

            Console.WriteLine("+ Starting title lookup for TitleID: " + titleId);
            
            string apiResult = null;
            string csvResult = null;

            // Step 1: Try XboxUnity online API
            Console.WriteLine("+ [1/2] Attempting XboxUnity API lookup...");
            try
            {
                apiResult = XboxUnityScraper.LookupTitleByTitleId(titleId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("+ XboxUnity API exception: " + ex.Message);
            }

            // Step 2: Try local CSV database (always, for verification)
            Console.WriteLine("+ [2/2] Attempting local CSV lookup...");
            try
            {
                csvResult = GameListCsvReader.LookupTitleByTitleId(titleId, platform);
            }
            catch (Exception ex)
            {
                Console.WriteLine("+ CSV lookup exception: " + ex.Message);
            }

            // Summary of results
            Console.WriteLine("");
            Console.WriteLine("=== LOOKUP RESULTS SUMMARY ===");
            Console.WriteLine("  API Result: " + (string.IsNullOrEmpty(apiResult) ? "(not found)" : apiResult));
            Console.WriteLine("  CSV Result: " + (string.IsNullOrEmpty(csvResult) ? "(not found)" : csvResult));
            
            // Prefer API result, but show both
            if (!string.IsNullOrEmpty(apiResult))
            {
                Console.WriteLine("  ? Using API result: " + apiResult);
                
                if (!string.IsNullOrEmpty(csvResult) && !csvResult.Equals(apiResult, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("  ! Note: CSV has different name: " + csvResult);
                }
                
                return apiResult;
            }
            
            if (!string.IsNullOrEmpty(csvResult))
            {
                Console.WriteLine("  ? Using CSV result: " + csvResult);
                return csvResult;
            }

            Console.WriteLine("  ? Title not found in any source");
            Console.WriteLine("==============================");
            Console.WriteLine("");
            
            return null;
        }
    }
}

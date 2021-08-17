using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CT_API_GUI
{
    /**
     * @class HttpCalls - class containing all API caller functions to collect JSON strings from CT API website
     *
     * Author: Andrew Holmes
     */
    public static class HttpCalls
    {
        /**
         * @function HttpCaller - a function that takes a url as a string and returns the API response
         *
         * INPUT: string url - the url of the desired API information
         * RETURN: string responseBody - the string containing the returned JSON from CT website
         */
        public static async Task<string> HttpCaller(string url)
        {
            try
            {
                //Create HttpClient
                var client = new HttpClient();
                //Await the response from the url passed
                var responseBody = await client.GetStringAsync(url);
                //Return the response
                return responseBody;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Could not connect to Chronotrack Server...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
        }

        /**
         * @function IntervalHttpCall - a function that takes an interval ID and returns the results for that interval
         *
         * INPUT: string id - the id of the desired interval to get results for
         * RETURN: string responseBody - the string containing the returned JSON from CT website
         */
        public static async Task<string> IntervalHttpCall(string id)
        {
            try
            {
                var intervalResultsUrl =
                    "https://api.chronotrack.com:443/api/interval/" + id +
                    "/results?format=json&client_id=727dae7f&user_id=" +
                    "aholmes%40dcscorp.com&user_pass=4453e390b44be3e2954b4d49635edd1884c38994&page=1&size=50";
                var client = new HttpClient();
                var responseBody = await client.GetStringAsync(intervalResultsUrl);
                return responseBody;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Could not connect to Chronotrack Server...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
        }
    }
}
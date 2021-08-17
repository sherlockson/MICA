using System;
using System.Collections.Generic;
using System.Linq;
using Geolocation;
using Newtonsoft.Json.Linq;

namespace CT_API_GUI
{
    /*
         * @Class - interval
         * @MemberVariables:
         *  STRING:
         *      results_interval_id - UID of each interval point
         *      results_interval_name - unique name of each interval point
         *      results_end_chip_time - time at which the runner crossed the specific interval point
         *      results_generation_gen_time - backup to chip time to detect runner crossings
         *      interval_id - UID of the interval point (different name for different API function calls)
         *      race_id - UID of the race the interval point belongs to 
         *  DOUBLE:
         *      completes - count of how many total runners have passed through a point
         *      percentage - calculation of the total percentage of runners who have crossed the interval point
         *  INT:
         *      racesBelongTo - the number of races that share the interval point
         *
         * @MemberFunctions
         *      interval() - Default constructor
         * 
         *      GetIntervalIds() - Function to get initial interval locations and their IDs
         *          INPUTS:
         *              string response - the response body passed by the HTTP caller
         *          RETURN:
         *              List<interval> - parsed JSON object in enumerable list form as interval objects
         * 
         *      ParseIntervalResults() - Function to update existing global interval list on runner movement
         *          INPUTS:
         *              string response - the response body passed by the HTTP Caller
         *           RETURN:
         *              int - number of runners that have passed over the interval
         */
    public class interval
    {
        public string results_interval_id;
        public string results_interval_name;
        public string results_end_chip_time;
        public string result_generation_gen_time;
        public string interval_id;
        public string race_id;

        public double completes;
        public double percentage;
        public int racesBelongTo;
        public Coordinate location;

        public interval()
        {
        }

        public static List<interval> GetIntervalIds(string response)
        {
            //Parse the response as a JObject (API returns the JSON formatted as a JArray)
            var intervalList = JObject.Parse(response);
            //Create a temporary list
            var intervals = new List<interval>();
            //Add range of all "event intervals" into the temporary list as interval objects
            intervals.AddRange((intervalList["event_interval"] ??
                                throw new InvalidOperationException()).Select(i => i.ToObject<interval>()));
            //Return the temp list as the final list
            return intervals;
        }

        public static int ParseIntervalResults(string response)
        {
            //Parse JSON string into a JObject
            var resultsJson = JObject.Parse(response);
            //Initialize a temp list of intervals
            var resultsList = new List<interval>();

            //Parse the results as an enumerable list of interval objects
            resultsList.AddRange(
                (resultsJson["interval_results"] ?? throw new InvalidOperationException()).Select(result =>
                    result.ToObject<interval>()));

            //LINQ return of the count of each interval that does not have an empty or null string for end_chip_time
            return resultsList.Count(result => !string.IsNullOrEmpty(result.results_end_chip_time));
        }
    }
}
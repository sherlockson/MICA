using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MICA
{
    /*
     * @Class - race
     * @MemberVariables:
     *   STRING:
     *       race_id - UID of the race object in the CT API
     *       race_name - unique name of the race
     *   List<T>:
     *       raceIntervals - enumerable list of interval objects attributed to the race
     *       raceEntries - enumerable list of entry (runner) objects attributed to the race
     *
     * @MemberFunctions:
     *       race() - default constructor
     *
     *       ParseRaces() - function to parse the JSON object of event races collected from the CT API
     *           INPUTS:
     *               string response - the string JSON object collected from the API call
     *           RETURN:
     *               List<races> races - the enumerable list of race objects
     */
    public class race
    {
        public string race_id;
        public string race_name;
        public List<interval> raceIntervals;
        public List<entry> raceEntries;

        public race()
        {
        }

        public static List<race> ParseRaces(string response)
        {
            //Parse the string as a JSON object using Newtonsoft JSON library
            var raceList = JObject.Parse(response);
            //Initialize a temporary list of race objects
            var races = new List<race>();

            //Iterate over the JSON array of name "event_race" and ToObject them as race objects, finally adding them to the temp list
            races.AddRange(
                (raceList["event_race"] ?? throw new InvalidOperationException()).Select(p =>
                    p.ToObject<race>()));

            //For each parsed object, initialize internal lists
            foreach (var race1 in races)
            {
                race1.raceEntries = new List<entry>();
                race1.raceIntervals = new List<interval>();
            }

            //Return the list
            return races;
        }
    }
}
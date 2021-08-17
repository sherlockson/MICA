using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace MICA
{
    /*
         * @Class - entry (runner)
         * @MemberVariables:
         *  STRING:
         *      entry_id - runner unique identifier
         *      entry_bib - bib number of each runner
         *      race_id - race id of the race the runner is participating in 
         *
         * @MemberFunctions:
         *      entry() - default constructor
         * 
         *      ParseRunners() - function to parse each runner in the race to an enumerable list
         *          INPUTS:
         *              string response - the response body from the HTTP caller
         *          OUTPUTS:
         *           List<entry> - an enumerable list of parsed entity (runner) objects
         */
    public class entry
    {
        public string entry_id;
        public string entry_bib;
        public string race_id;

        public entry()
        {
        }

        public static List<entry> ParseRunners(string response)
        {
            //Parse response into a JObject
            var entryList = JObject.Parse(response);
            //Create a temporary list of entry objets
            var entries = new List<entry>();
            //Add each JToken within the "event_entry" range of the JObject and parse it to an entry object
            entries.AddRange(
                (entryList["event_entry"] ?? throw new InvalidOperationException()).Select(p =>
                    p.ToObject<entry>()));

            //Return the final list
            return entries;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Geolocation;

// ReSharper disable LocalizableElement

namespace MICA
{
    /**
     * @class Api - The class containing all necessary methods and information needed to collect, parse, and pass
     * data pertaining to AF Marathon racers and runner intervals.
     *
     * Author - Andrew Holmes
     */
    public static class Api
    {
        private static List<race> raceList; //Global list of all race objects
        private static List<interval> intervalList; //Global list of interval objects
        private static List<entry> racerList; //Global list of racer (entry in CT API) objects
        private static string marathonId;
        private static UdpSocket sender;
        private static Dictionary<string, Coordinate> ids;
        private static int networkChoice = 0;
        private static string address;
        private static int port;


        //The URL of the top-level Event Interval object containing the information for all intervals within the race
        private static string EventIntervalUrl { get; set; }

        //The URL of the in-depth entry (runner) objects containing the information for all runners in the race
        private static string PeopleUrl { get; set; }

        //The URL of the in-depth race objects containing the information for all races in the marathon event
        private static string RaceUrl { get; set; }

        /**
         * @Task Main - The main driver program of the API Hook. Calls setup function then loops infinitely
         */
        public static async Task Main()
        {
            ConsoleInput();
            Console.WriteLine("Beginning Setup Process...");
            await Setup();
            Console.WriteLine("Beginning API Loop");
            while (true)
            {
                await Task.Run(Loop);
            }
        }
        
        private static void SetUrls()
        {
            EventIntervalUrl =
                $"https://api.chronotrack.com:443/api/event/{marathonId}/interval?format=json&client_id=727dae7f&user_id=aholmes%40dcscorp.com&user_pass=4453e390b44be3e2954b4d49635edd1884c38994&page=1&size=50";

            PeopleUrl =
                $"https://api.chronotrack.com:443/api/event/{marathonId}/entry?format=json&client_id=727dae7f&user_id=aholmes%40dcscorp.com&user_pass=4453e390b44be3e2954b4d49635edd1884c38994&page=1&size=50&include_test_entries=true&elide_json=false";

            RaceUrl =
                $"https://api.chronotrack.com:443/api/event/{marathonId}/race?format=json&client_id=727dae7f&user_id=aholmes%40dcscorp.com&user_pass=4453e390b44be3e2954b4d49635edd1884c38994&page=1&size=50&include_not_wants_results=true";
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"URLs set with Event ID: {marathonId}");
            Console.ForegroundColor = ConsoleColor.White;
        }

        /*
         * @void ConsoleInput - function to present the user with a menu of options to setup runtime enviornment
         * @OUTPUT - networkChoice variable set, marathonId set, appropriate IP address and port set
         */
        private static void ConsoleInput()
        {
            try
            {
                Console.Write("Input ChronoTrack Marathon Id: ");
                marathonId = Console.ReadLine();
               
                Console.Write("1. Localhost Connection\n" +
                              "2. Specific IP Connection\n" +
                              "3. Multicast Connection\n" +
                              "Input the type of network connection you would like: ");
                networkChoice = Int32.Parse(Console.ReadLine() ?? string.Empty) - 1;

                switch (networkChoice)
                {
                    case 0:
                        break;
                    case 1:
                        Console.Write("Input the IP address for UDP connection: ");
                        address = Console.ReadLine();
                        Console.Write("Input the port for the UDP connection: ");
                        port = int.Parse(Console.ReadLine() ?? string.Empty);
                        break;
                    case 2:
                        Console.Write("Input the Multicast address: ");
                        address = Console.ReadLine();
                        Console.Write("Input the Multicast port: ");
                        port = int.Parse(Console.ReadLine() ?? string.Empty);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("ERROR: Incorrect Network Choice. Try inputting an integer of the option you want.");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /**
         * @Task Setup - Getter program to collect global lists of runners, races, and intervals
         *               then map unique points and racers to individual race lists
         * @OUTPUT: Edits global variable lists
         */
        private static async Task Setup()
        {
            try
            {
                SetUrls(); //Set URLs after user input
                SetupNetwork(); //Setup networks after user input
                await GetRunners(); //Get and update global runner list
                await GetIntervals(); //Get and update global interval list
                await GetRaces(); //Get and update global race list
                MapRaceIntervals(); //Map intervals to each race they belong to
                MapRaceEntries(); //Map racers to each race they belong to
                GetIntervalCoordinates(); //Map intervals to their respective coordinates    
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Setup complete...\nBeginning API Loop...");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /*
         * @void SetupNetwork - Program utilizing the networkChoice variable to setup the appropriate network config
         * @OUTPUT - appropriate socket created
         */
        private static void SetupNetwork()
        {
            switch (networkChoice)
            {
                case 0: //Localhost setup
                    try
                    {
                        sender = new UdpSocket();
                        sender.Client("127.0.0.1", 8080);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("UDP Sockets Created...");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                case 1: //Specific UDP socket setup
                    try
                    {
                        sender = new UdpSocket();
                        sender.Client(address, port);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"UDP Sockets Created for {address}...");
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                case 2: //Multicast setup
                    try
                    {
                        MulticastSender.McastAddress = IPAddress.Parse(address);
                        MulticastSender.McastPort = port;
                        MulticastSender.JoinMulticastGroup(); //Creates and binds to multicast socket to send messages
                        Console.WriteLine(
                            $"Multicast IP Address: {MulticastSender.McastAddress}\nMulticast Port: {MulticastSender.McastPort}\nMulticast Setup Complete...");
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
            }
        }

        /**
         * @Task: Loop - Infinitely looping method to update interval objects and calculate completion percentage
         */
        private static void Loop()
        {
            //Parallel For each loop for each race in the list
            Parallel.ForEach(raceList, race =>
            {
                //Nested parallel for each loop for each interval object in each race
                Console.WriteLine("Updating race: " + race.race_name + "...");
                Parallel.ForEach(race.raceIntervals, async interval =>
                {
                    Console.WriteLine($"Updating interval: {interval.interval_id}");
                    //Collect updated interval results and save as a string
                    var updatedIntervalResults = await HttpCalls.IntervalHttpCall(interval.interval_id);
                    //Parse JSON string object and save the completions in the interval object
                    interval.completes = interval.ParseIntervalResults(updatedIntervalResults);

                    if (interval.racesBelongTo == 1)
                    {
                        //Calculate percentage by dividing the number of completions by the total number of runners
                        interval.percentage = interval.completes / race.raceEntries.Count;
                    }
                    else //If the race belongs to more than one race
                    {
                        //LINQ statement creating a double variable containing the total number of runners for each race
                        //containing the same @race_id as the interval
                        var intervalRacers =
                            raceList.Where(race1 => race1.raceIntervals.Contains(interval)).Aggregate(new double(),
                                (current, race1) => current + race1.raceEntries.Count);
                        //Calculate percentage by dividing the number of total completions by the total number of runners
                        //for each involved race that interval belongs to
                        interval.percentage = interval.completes / intervalRacers;
                    }
                });
            });

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("API Loop Complete... Broadcasting Messages");
            Console.ForegroundColor = ConsoleColor.White;
            //Create a temporary list of intervals to keep track of which messages have already been sent
            var tempIntervalList = new List<interval>();
            //For each loop with a LINQ qualifier to send a message for each interval out of each indiviual
            //race's interval lists if it doesn't exist within the temporary list
            foreach (var interval in raceList.SelectMany(race =>
                race.raceIntervals.Where(interval => !tempIntervalList.Contains(interval))))
            {
                switch (networkChoice)
                {
                    case 0:
                        //Send the message
                        sender.Send(MessageBuilder(interval));
                        break;
                    case 1:
                        //Send the message
                        sender.Send(MessageBuilder(interval));
                        break;
                    case 2:
                        //Send the message
                        MulticastSender.BroadcastMessage(MessageBuilder(interval));
                        break;
                }
               
                //Add it to the list so as not to send it again
                tempIntervalList.Add(interval);
                //Sleep for 2 seconds in order not to overload listener program with an influx of messages
                Thread.Sleep(2000);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("API messages sent. Will update in 30 seconds" +
                              "\n--------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.White;
            //Sleep for 30 seconds (CHANGE FOR MARATHON)
            Thread.Sleep(30000);
        }

        /**
         * @String: Message Builder - Method to parse interval objects and their results as a string to send via multicast
         * INPUT: interval interval - the interval object to be parsed and sent
         * OUTPUT: string message - the string containing the built message to be sent via multicast
         */
        private static string MessageBuilder(interval interval)
        {
            //Static variables for COT messages
            const string eventType = "a-f-G-U-C";
            const string linkType = "b-m-p-s-m";
            const string whitePush = "-1";
            const string redPush = "-65536";
            const string yellowPush = "-256";
            const string greenPush = "-16711936";
            const string bluePush = "-16776961";
            const string bluePath = "COT_MAPPING_SPOTMAP/b-m-p-s-m/-16776961";
            const string yellowPath = "COT_MAPPING_SPOTMAP/b-m-p-s-m/-256";
            const string redPath = "COT_MAPPING_SPOTMAP/b-m-p-s-m/-65536";
            const string greenPath = "COT_MAPPING_SPOTMAP/b-m-p-s-m/-16711936";
            const string whitePath = "COT_MAPPING_SPOTMAP/b-m-p-s-m/-1";
            //String variables for remarks (the percentage) and the message (the full string to be returned)
            string remarks;
            string message;
            //ID correlates to the interval's unique ID
            var id = interval.interval_id;

            switch (interval.percentage * 100) //Multiply the percentage by 100
            {
                //If less than 1/4th of runners have crossed the point, give a red point on TAK
                case <= 25.0:
                    remarks = interval.percentage.ToString("0.00%");
                    message = "lat: " + interval.location.Latitude + " lon: " + interval.location.Longitude
                              + " uid: " + id + " event_type: " + eventType + " remarks: " + remarks + " link_type: "
                              + linkType + " argb: " + redPush  + " iconsetpath: " + redPath;
                    break;
                //If more than 1/4th but less than 1/2, push a blue point
                case <= 50.0 and > 25.0:
                    remarks = interval.percentage.ToString("0.00%");
                    message = "lat: " + interval.location.Latitude + " lon: " + interval.location.Longitude
                              + " uid: " + id + " event_type: " + eventType + " remarks: " + remarks + " link_type: "
                              + linkType + " argb: " + bluePush  + " iconsetpath: " + bluePath;
                    break;
                //If more than 1/2 but less than 3/4, push a yellow point
                case <= 75.0 and > 50.0:
                    remarks = interval.percentage.ToString("0.00%");
                    message = "lat: " + interval.location.Latitude + " lon: " + interval.location.Longitude
                              + " uid: " + id + " event_type: " + eventType + " remarks: " + remarks + " link_type: "
                              + linkType + " argb: " + yellowPush  + " iconsetpath: " + yellowPath;
                    break;
                //If more than 3/4ths, push a green point
                case > 75.0:
                    remarks = interval.percentage.ToString("0.00%");
                    message = "lat: " + interval.location.Latitude + " lon: " + interval.location.Longitude
                              + " uid: " + id + " event_type: " + eventType + " remarks: " + remarks + " link_type: "
                              + linkType + " argb: " + greenPush + " iconsetpath: " + greenPath;
                    break;
                //Default in case of error
                default:
                    remarks = interval.percentage.ToString("0.00%");
                    message = "lat: " + interval.location.Latitude + " lon: " + interval.location.Longitude
                              + " uid: " + id + " event_type: " + eventType + " remarks: " + remarks + " link_type: "
                              + linkType + " argb: " + whitePush  + " iconsetpath: " + whitePath;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("ERROR: Percentage incorrect. Pushing white pushpin as default.");
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            //Return the message based on switch statement case
            return message;
        }

        /**
         * Task: GetRunners - method to fetch JSON object of all event runners and parse into global list
         */
        private static async Task GetRunners()
        {
            try
            {
                //Initialize global list
                racerList = new List<entry>();
                //Fetch JSON from CT API website
                var responseBody = await HttpCalls.HttpCaller(PeopleUrl);
                //Pass to runner parser function
                racerList = entry.ParseRunners(responseBody);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Runners: OK. Total Runners: {racerList.Count}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Runners unable to be parsed...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /**
         * Task: GetIntervals - method to fetch JSON object of all event intervals and parse into global list
         */
        private static async Task GetIntervals()
        {
            try
            {
                //Initialize global list
                intervalList = new List<interval>();
                //Fetch JSON object from CT API
                var responseBody = await HttpCalls.HttpCaller(EventIntervalUrl);
                //Pass to interval parser and set global list to return of function
                intervalList = interval.GetIntervalIds(responseBody);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Intervals: OK. Total Intervals: {intervalList.Count}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Intervals unable to be parsed...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /**
         * Task: GetRaces - method to fetch JSON object of all event races and parse into global list
         */
        private static async Task GetRaces()
        {
            try
            {
                //Initialize global list
                raceList = new List<race>();
                //Fetch JSON object from CT API
                var responseBody = await HttpCalls.HttpCaller(RaceUrl);
                //Pass to race parser and set global list as return of the function
                raceList = race.ParseRaces(responseBody);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Races: OK. Total Races: {raceList.Count}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Races unable to be parsed...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /**
         * void MapRaceIntervals - Method to map event intervals to each race they specifically belong to
         */
        private static void MapRaceIntervals()
        {
            try
            {
                foreach (var race in raceList) //For each race in the event
                {
                    //For each loop with LINQ qualifier to select intervals where their owner race ID matches the race ID
                    foreach (var interval in intervalList.Where(interval => interval.race_id.Equals(race.race_id)))
                    {
                        //Add it to the internal race interval list
                        race.raceIntervals.Add(interval);
                        //Increase interval races for percentage calculation purposes
                        interval.racesBelongTo++;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Interval {interval.interval_id} mapped to race {race.race_id}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Intervals successfully mapped to races...");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Intervals unable to be mapped to races...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        /**
         * void MapRaceEntries - Method to map event entries (runners) to each race they specifically belong to
         */
        private static void MapRaceEntries()
        {
            try
            {
                foreach (var race in raceList) //For each race in the event
                {
                    //For each loop with a LINQ qualifier to choose entries where their owner race ID matches the iteration race ID
                    foreach (var entry in racerList.Where(entry => entry.race_id.Equals(race.race_id)))
                    {
                        //Add to the internal race entry list
                        race.raceEntries.Add(entry);
                    }
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Runners successfully mapped to races...");
                Console.ForegroundColor = ConsoleColor.White;
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Runners unable to be mapped to races...");
                Console.WriteLine(e);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
        /*
         * void GetIntervalCoordinates - Method to map the interval ID to the locations they will be at in
         * the 2021 AFM
         */

        private static void GetIntervalCoordinates()
        {
            //Create the dictionary to lookup interval coordinates based on unique ID
            ids = new Dictionary<string, Coordinate>()
            {
                {"111111", new Coordinate(39.7820035, -84.0545872)},
                {"111112", new Coordinate(39.7817053, -84.0541241)},
                {"111113", new Coordinate(39.7866823, -84.1033898)},
                {"111114", new Coordinate(39.7836481, -84.0841791)}, //Hobson way
                {"111115", new Coordinate(39.7850337, -84.1071719)},
                {"111116", new Coordinate(39.7852471, -84.1071008)},
                {"111122", new Coordinate(39.8006421, -84.0740441)}, //Hebble Creek
                {"111117", new Coordinate(39.7885716, -84.0480874)},
                {"111123", new Coordinate(39.8260493, -84.0615116)}, //Scout camp
                {"111118", new Coordinate(39.8414081, -84.0353504)},
                {"111124", new Coordinate(39.8103749, -84.0320672)}, //Schuster drive
                {"111119", new Coordinate(39.8214304, -84.0207584)},
                {"111125", new Coordinate(39.8074305, -84.0375252)}, //Estabrook drive
                {"111126", new Coordinate(39.8100594, -84.0401771)}, //Metzger drive
            };

            //For each interval in the racelist, map the coordinates to their respective locations
            foreach (var interval in raceList.SelectMany(race => race.raceIntervals))
            {
                try
                {
                    interval.location = ids[interval.interval_id];
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(
                        $"Mapped interval {interval.interval_id} to location {interval.location.Latitude}, {interval.location.Longitude}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch
                {
                    interval.location = new Coordinate(39.9897842121979, -83.0304294861043);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"ERROR: Interval {interval.interval_id} unable to be mapped to location");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }
}
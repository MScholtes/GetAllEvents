// Markus Scholtes, 2019
// Console program to query all events from all event logs (there are about 1200 in Windows 10 !)
// Output is sorted by time
// Output can be to file or console in text or csv format
// A remote computer can be accessed

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;

// set executable properties
using System.Reflection;
[assembly:AssemblyTitle("Retrieve events from all event logs")]
[assembly:AssemblyDescription("Tool to retrieve all events from all event logs")]
[assembly:AssemblyConfiguration("")]
[assembly:AssemblyCompany("MS")]
[assembly:AssemblyProduct("GetAllEvents")]
[assembly:AssemblyCopyright("© Markus Scholtes 2019")]
[assembly:AssemblyTrademark("")]
[assembly:AssemblyCulture("")]
[assembly:AssemblyVersion("1.0.0.0")]
[assembly:AssemblyFileVersion("1.0.0.0")]


class GetAllEvents
{
	public static int Main(string[] arguments)
	{
		// initiate parameter parsing
		ParseParameters parameter = new ParseParameters();
		// default parameter is allowed
		parameter.EnableDefaultParameter();

		// parse command line
		if (parameter.Scan(arguments))
		{ // error reading command line
			Console.Error.WriteLine(parameter.errorText);
			return -1;
		}

		// check if there is an unknown parameter
		if (!parameter.CheckForUnknown(new string[] { "?", "h", "help", "l", "log", "logname", "c", "computer", "computername", "s", "start", "starttime", "e", "end", "endtime", "level", "csv", "f", "file", "filename", "q", "quiet" }))
		{ // error reading command line
			Console.Error.WriteLine("Unknown parameter error.");
			return -1;
		}

		if (parameter.Exist("?") || parameter.Exist("h") || parameter.Exist("help"))
		{ // help wanted
			Console.WriteLine("{0}\t\t\t\t\tMarkus Scholtes, 2019\n", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("Program to determine the events of all event logs ordered by time.\n");
			Console.WriteLine("{0} [[-logname:]<LOGNAMES>] [-level:<LEVEL>]", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("    [-starttime:<STARTTIME>] [-endtime:<ENDTIME>] [-computername:<COMPUTER>]");
			Console.WriteLine("    [-filename:<FILENAME>] [-csv] [-quiet] [-?|-help]");
			Console.WriteLine("\nParameters:");
			Console.WriteLine("-logname:<LOGNAMES> comma separated list of event log names. Queries all event");
			Console.WriteLine("    logs if omitted (can be abbreviated as -log or -l or can be omitted).");
			Console.WriteLine("-level:<LEVEL> queries up to level <LEVEL>. Queries all events if omitted.");
			Console.WriteLine("    Level: Critical - 1, Error - 2, Warning - 3, Informational - 4, Verbose - 5");
			Console.WriteLine("-starttime:<STARTTIME> start time of events to query (can be abbreviated as");
			Console.WriteLine("    -start or -s). Default is end time minus one hour.");
			Console.WriteLine("-endtime:<ENDTIME> end time of events to query (can be abbreviated as -end or");
			Console.WriteLine("    -e). Default is now.");
			Console.WriteLine("-computername:<COMPUTER> name of computer to query (can be abbreviated as");
			Console.WriteLine("    -computer or -c). Default is the local system.");
			Console.WriteLine("-filename:<FILENAME> name of the file in which the results are output (can be");
			Console.WriteLine("    abbreviated as -file or -f). Default is output to the console.");
			Console.WriteLine("-csv output format \"comma separated\" instead of output format text.");
			Console.WriteLine("-quiet shows only error messages and results (can be abbreviated as -q).");
			Console.WriteLine("-? or -help shows this help (can be abbreviated as -h).");
			Console.WriteLine("\nExamples:");
			Console.WriteLine("{0} -start:10:00 -end:11:00", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("{0} System,Setup,Application -Computer=REMOTESYSTEM", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("{0} /level:2 /q /CSV /file:OnlyErrors.csv", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("{0} \"/starttime:2019/11/29 10:00\" \"/endtime:2019/11/29 11:00\"", System.AppDomain.CurrentDomain.FriendlyName);
			Console.WriteLine("{0} \"/s=2019/12/08 10:09:49.450\" \"/e=2019/12/08 10:09:49.850\"", System.AppDomain.CurrentDomain.FriendlyName);
			return 0;
		}

		bool bQuiet = false; // no status messages?
		if (parameter.Exist("q") || parameter.Exist("quiet")) bQuiet = true;

		// read end time for log query
		DateTime endTime;
		// read parameter /e, /end or /endtime
		string endString = parameter.ValueOrDefault("e", parameter.ValueOrDefault("end", parameter.Value("endtime")));
		if (endString != "")
		{ // parameter set, convert to timestamp
			try {
				endTime = DateTime.Parse(endString);
			}
			catch
			{ // cannot convert string to timestamp
				Console.Error.WriteLine("Error: unknown time format");
				return -1;
			}
		}
		else
			// parameter not specified, use default (now)
			endTime = DateTime.Now;

		// read start time for log query
		DateTime startTime;
		// read parameter /s, /start or /starttime
		string startString = parameter.ValueOrDefault("s", parameter.ValueOrDefault("start", parameter.Value("starttime")));
		if (startString != "")
		{ // parameter set, convert to timestamp
			try {
				startTime = DateTime.Parse(startString);
			}
			catch
			{ // cannot convert string to timestamp
				Console.Error.WriteLine("Error: unknown time format");
				return -1;
			}
		}
		else
			// parameter not specified, use default (end time minus one hour)
			startTime = endTime.AddHours(-1.0);

		if (endTime <= startTime)
		{ // end time has to be later than start time
			Console.Error.WriteLine("Error: end time has to be later than start time");
			return -1;
		}

		// get information level, 0 = default = return all events
		// Level: LogAlways - 0, Critical - 1, Error - 2, Warning - 3, Informational - 4, Verbose - 5
		// there are different levels for auditing logs!
		string informationLevel = parameter.Value("level");
		int maxLevel = 0;
		if (informationLevel != "")
		{ // parameter level was given
			if (Int32.TryParse(informationLevel, out maxLevel))
			{ // filter for information level
				if ((maxLevel < 0) || (maxLevel > 5))
					// mark as wrong information level
					maxLevel = -1;
			}
			else
				// mark as wrong information level
				maxLevel = -1;
			if (maxLevel < 0)
			{ // wrong information level -> error
				Console.Error.WriteLine("Error: unknown information level");
				Console.Error.WriteLine("The following values are allowed: up to Critical - 1, up to Error - 2, up to Warning - 3, up to Informational - 4, up to Verbose - 5");
				return -1;
			}
		}

		// connect to remote computer in parameter /c, /computer or /computername or to "localhost"
		EventLogSession session = new EventLogSession(parameter.ValueOrDefault("c", parameter.ValueOrDefault("computer", parameter.ValueOrDefault("computername", "localhost"))));

		int outputMode = 1; // 1 - Text, 2 - CSV
		if (parameter.Exist("csv")) outputMode = 2;

		// get filename. If no filename is given output is written to console
		string fileName = parameter.ValueOrDefault("f", parameter.ValueOrDefault("file", parameter.Value("filename")));

		List<string> logNames;
		// read parameter /l or /log or default parameter for log name (or comma separated list of names given)
		if (parameter.ValueOrDefault("l", parameter.ValueOrDefault("log", parameter.ValueOrDefault("logname", parameter.DefaultParameter()))) != "")
		{ // yes, only read those logs
			logNames = new List<string>(parameter.ValueOrDefault("l", parameter.ValueOrDefault("log", parameter.DefaultParameter())).Split(new char[] {',',';'}).Where(val => val.Trim() != "").Select(val => val.Trim()).ToArray());
		}
		else
		{ // no parameter for log, read all logs
			try { // retrieve all log names
				logNames = new List<string>(session.GetLogNames());
			}
			catch (Exception e)
			{ // cannot retrieve log names
				Console.Error.WriteLine("Error connecting to event log: " + e.Message);
				return -2;
			}
		}

		// sort log names now to save work later
		logNames.Sort();

		int logCount = 0;
		List<EventEntry> eventList = new List<EventEntry>();
		foreach (string name in logNames)
		{ // query entries for all logs
			if (GetEntries(ref eventList, name, session, startTime, endTime, maxLevel, outputMode, bQuiet)) logCount++;
		}

		if (eventList.Count > 0)
		{ // only write events if there are any
			if (fileName == "")
			{	// output to console
				if (outputMode == 1)
					Console.WriteLine("time created\tlog\tid\tsource\tlevel\tdescription");
				else
					Console.WriteLine("\"time created\";\"log\";\"id\";\"source\";\"level\";\"description\"");

				foreach (EventEntry entry in eventList.OrderBy(a => a.timeCreated))
				{ // output events sorted by time
					Console.WriteLine(entry.message);
				}
			}
			else
			{	// output to file
				try {
					// write header line only to new files
					bool typeHeader = true;
					if (File.Exists(fileName)) typeHeader = false;

					FileStream fs = new FileStream(fileName, FileMode.Append);
					using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
					{
						if (typeHeader)
						{ // output the header line
							if (outputMode == 1)
								sw.WriteLine("time created\tlog\tid\tsource\tlevel\tdescription");
							else
								sw.WriteLine("\"time created\";\"log\";\"id\";\"source\";\"level\";\"description\"");
						}

						foreach (EventEntry entry in eventList.OrderBy(a => a.timeCreated))
						{ // output events sorted by time
							sw.WriteLine(entry.message);
						}
					}
					if (fs != null) fs.Dispose();
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Error writing to file \"" + fileName + "\": " + e.Message);
					return 1;
				}
			}
		}

		if (!bQuiet) Console.WriteLine("Successfully processed " + eventList.Count + " events from " + logCount + " logs, access errors with " + (int)(logNames.Count - logCount) + " logs.");
		return 0;
	}

	#region reading event log
	public class EventEntry
	{
		public DateTime timeCreated { get; set; }
		public string message { get; set; }

		public EventEntry(DateTime timeStamp, string messageText)
		{
				timeCreated = timeStamp;
				message = messageText;
		}
	}

	private static bool GetEntries(ref List<EventEntry> eventList, string logName, EventLogSession session, DateTime startTime, DateTime endTime, int maxLevel, int outputMode, bool bQuiet)
	{
		string eventQuery;
		if (maxLevel == 0)
			// query for all information levels
			eventQuery = string.Format("*[System/TimeCreated/@SystemTime > '{0}'] and *[System/TimeCreated/@SystemTime <= '{1}']", startTime.ToUniversalTime().ToString("o"), endTime.ToUniversalTime().ToString("o"));
		else
			// level: LogAlways - 0, Critical - 1, Error - 2, Warning - 3, Informational - 4, Verbose - 5
			// there are different levels for auditing logs!
			eventQuery = string.Format("*[System/TimeCreated/@SystemTime > '{0}'] and *[System/TimeCreated/@SystemTime <= '{1}'] and *[System/Level <= {2}]", startTime.ToUniversalTime().ToString("o"), endTime.ToUniversalTime().ToString("o"), maxLevel.ToString());

		// define event log query
		EventLogQuery eventLogQuery = new EventLogQuery(logName, PathType.LogName, eventQuery);
		eventLogQuery.Session = session;

		try
		{ // start query
			EventLogReader eventLogReader = new EventLogReader(eventLogQuery);

			int count = 0;
			for (EventRecord eventRecord = eventLogReader.ReadEvent(); eventRecord != null; eventRecord = eventLogReader.ReadEvent())
			{ // enumerate all found events
				count++;
				// read Event details
				DateTime timeCreated = DateTime.Now;
				if (eventRecord.TimeCreated.HasValue) timeCreated = eventRecord.TimeCreated.Value;

				string eventLevel;
				try {
					eventLevel = eventRecord.LevelDisplayName;
					if (eventRecord.Level == 0) eventLevel = "LogAlways";
				}
				catch {
					eventLevel = "Unknown";
				}

				string eventLine;
				if (outputMode == 1)
					eventLine = timeCreated.ToString() + "\t" + logName + "\t" + eventRecord.Id.ToString() + "\t" + eventRecord.ProviderName + "\t" + eventLevel + "\t";
				else
					eventLine = "\"" + timeCreated.ToString() +  "\";\"" + logName + "\";" + eventRecord.Id.ToString() + ";\"" + eventRecord.ProviderName + "\";\"" + eventLevel + "\";\"";

				try
				{
					if (!String.IsNullOrEmpty(eventRecord.FormatDescription()))
					{
						if (outputMode == 1)
							eventList.Add(new EventEntry(timeCreated, eventLine + eventRecord.FormatDescription().Replace("\n", "\n\t")));
						else
							eventList.Add(new EventEntry(timeCreated, eventLine + eventRecord.FormatDescription().Replace("\"", "\"\"") + "\""));
					}
					else
					{ // description not available, try to interpret raw data
						string rawDescription = "";
						foreach (EventProperty eventProperty in eventRecord.Properties)
						{
							rawDescription += eventProperty.Value.ToString();
						}
						if (outputMode == 1)
							eventList.Add(new EventEntry(timeCreated, eventLine + rawDescription.Replace("\n", "\n\t")));
						else
							eventList.Add(new EventEntry(timeCreated, eventLine + rawDescription.Replace("\"", "\"\"") + "\""));
					}
				}
				catch (Exception e)
				{
					if (outputMode == 1)
						eventList.Add(new EventEntry(timeCreated, eventLine + "### Error reading the event log entry: " + e.Message.Replace("\n", "\n\t")));
					else
						eventList.Add(new EventEntry(timeCreated, eventLine + "### Error reading the event log entry: " + e.Message.Replace("\"", "\"\"") + "\""));
				}
			}

			if (!bQuiet) Console.WriteLine("Processed event log \"" + logName + "\": " + count + " entries");
			return true;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Error opening the event log \"" + logName + "\": " + e.Message);
			return false;
		}
	}
	#endregion

	#region parameter parsing
	// class to interpret command line parameters
	public class ParseParameters
	{
		// introducing characters for parameters
		private HashSet<char> introducerChar = new HashSet<char>();
		// with a separator character the value of a parameter starts
		private HashSet<char> separatorChar = new HashSet<char>();
		// hash table for parameter with values
		private System.Collections.Hashtable parameterList = new System.Collections.Hashtable();

		// is default parameter allowed (parameter without introducer)?
		private bool defaultParameterAllowed = false;

		// default parameter (parameter without introducer)
		private string defaultParameter;

		// error text
		public string errorText = "";


		public ParseParameters() : this(new char[] { '-', '/' }, new char[] { '=', ':' })
		{ // constructor with default separator characters
		}

		public ParseParameters(char[] introducerList) : this(introducerList, new char[] { '=', ':' })
		{ // contructor with introducer characters
		}

		public ParseParameters(char[] introducerList, char[] separatorList)
		{ // contructor with introducer and separator characters

			foreach (char introducer in introducerList)
				introducerChar.Add(introducer);

			foreach (char separator in separatorList)
				separatorChar.Add(separator);
		}

		public bool Scan(string[] arguments)
		{ // initial scan of parameters to hash table
			bool parameterError = false;

			foreach (string argument in arguments)
			{ // check all arguments
				if ((introducerChar.Contains(argument[0])) || (introducerChar.Contains('\0')))
				{ // parameter introducing char at first place found
					bool foundSeparator = false;
					int searchPosition = 1;
					// no introducer char required?
					if (introducerChar.Contains('\0')) searchPosition = 0;
					for (int i = searchPosition; (i < argument.Length) && (!foundSeparator); i++)
					{ // search for separating char
						if (separatorChar.Contains(argument[i]))
						{ // found separating char
							foundSeparator = true;
							if (i == searchPosition)
							{ // separating char at second place is an error
								parameterError = true;
								errorText = "Error in parameter " + argument;
							}
							else
							{ // parameter found, now check if it already exists
								if (!parameterList.Contains(argument.Substring(searchPosition, i - searchPosition).ToUpper()))
								{ // no, then add to hash table
									parameterList.Add(argument.Substring(searchPosition, i - searchPosition).ToUpper(), argument.Substring(i + 1));
								}
								else
								{ // yes, this is an error
									parameterError = true;
									errorText = "Multiple occurrance of parameter " + argument.Substring(searchPosition, i - searchPosition).ToUpper();
								}
							}
						}
					}

					if (!foundSeparator)
					{ // no separating char found, do we have a switch?
						if (argument.Length > searchPosition)
						{ // string for switch name found, check if it already exists
							if (!parameterList.Contains(argument.Substring(searchPosition).ToUpper()))
							{ // no, then add to hash table
								parameterList.Add(argument.Substring(searchPosition).ToUpper(), "");
							}
							else
							{ // yes, this is an error
								parameterError = true;
								errorText = "Multiple occurrance of parameter " + argument.Substring(searchPosition).ToUpper();
							}
						}
						else
						{ // no name found: error
							parameterError = true;
							errorText = "Error in parameter " + argument;
						}
					}
				}
				else
				{ // no introducing character found
					if (defaultParameterAllowed)
					{ // store value as default parameter
						if (defaultParameter == "")
							defaultParameter = argument;
						else
						{ // error: default parameter already exist
							parameterError = true;
							errorText = "Multiple occurrance of the default parameter";
						}
					}
					else
					{ // unknown error
						parameterError = true;
						errorText = "Error in parameter " + argument;
					}
				}
			}
			return parameterError;
		}

		public void EnableDefaultParameter()
		{ // enable default parameter
			defaultParameter = "";
			defaultParameterAllowed = true;
		}

		public string DefaultParameter()
		{ // get default parameter
			if (defaultParameterAllowed)
				return defaultParameter;
			else
				return "";
		}

		public int Count()
		{ // return count of paramaters in hash table
			return parameterList.Count;
		}

		public System.Collections.IDictionaryEnumerator Enumerator()
		{ // get enumerator of parameters' hash table
			return parameterList.GetEnumerator();
		}

		public string Value(string key)
		{ // retrieve value to key, empty string if it does not exist
			string value = "";

			if (parameterList.ContainsKey(key.ToUpper()))
			{ // retrieve value to key
				value = parameterList[key.ToUpper()].ToString();
			}
			return value;
		}

		public string ValueOrDefault(string key, string defaultvalue)
		{ // retrieve value to key, defaultvalue if key does not exist
			string value;

			if (parameterList.ContainsKey(key.ToUpper()))
			{ // retrieve value to key
				value = parameterList[key.ToUpper()].ToString();
			}
			else
			{ // key does not exist, return defaultvalue
				value = defaultvalue;
			}
			return value;
		}

		public bool Exist(string key)
		{ // check if key exists

			if (parameterList.ContainsKey(key.ToUpper()))
			{ // key exists
				return true;
			}
			else
			{ // key does not exist
				return false;
			}
		}

		public void Remove(string key)
		{ // remove key (if it exist)
			if (parameterList.ContainsKey(key.ToUpper()))
			{ // remove key
				parameterList.Remove(key.ToUpper());
			}
		}

		public bool CheckForUnknown(string[] keyList)
		{ // checks for unknown parameter in checking if there are parameters that are not in keyList
			foreach (object parameter in parameterList.Keys)
			{ // enumerate all parameters
				bool Check = false;

				foreach (string key in keyList)
				{ // test parameter against all items of keyList
					if (parameter.ToString().Equals(key.ToUpper()))
						Check = true; // parameter found
				}

				// parameter not found, return "unknown parameter"
				if (!Check) return false;
			}
			return true; // parameter "all parameters found"
		}
	}
	#endregion
}

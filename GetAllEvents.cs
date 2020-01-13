// Markus Scholtes, 2019 + 2020
// Console program to query all events from all event logs (there are about 1200 in Windows 10 !)
// output is sorted by time
// output can be to file and to console in text or csv format or graphically to a gridview
// a remote computer can be accessed

using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml;

// set executable properties
using System.Reflection;
[assembly:AssemblyTitle("Retrieve events from all event logs")]
[assembly:AssemblyDescription("Tool to retrieve all events from all event logs")]
[assembly:AssemblyConfiguration("")]
[assembly:AssemblyCompany("MS")]
[assembly:AssemblyProduct("GetAllEvents")]
[assembly:AssemblyCopyright("© Markus Scholtes 2020")]
[assembly:AssemblyTrademark("")]
[assembly:AssemblyCulture("")]
[assembly:AssemblyVersion("1.0.0.2")]
[assembly:AssemblyFileVersion("1.0.0.2")]

namespace NameSpaceGetAllEvents
{
	class GetAllEvents
	{
		// WPF requires STA model, since C# default to MTA threading, the following directive is mandatory
		[STAThread]
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
			if (!parameter.CheckForUnknown(new string[] { "?", "h", "help", "l", "log", "logname", "c", "computer", "computername", "s", "start", "starttime", "e", "end", "endtime", "level", "csv", "g", "grid", "f", "file", "filename", "q", "quiet", "domainname", "domain", "d", "username", "user", "u", "password", "pass", "p" }))
			{ // error reading command line
				Console.Error.WriteLine("Unknown parameter error.");
				return -1;
			}

			if (parameter.Exist("?") || parameter.Exist("h") || parameter.Exist("help"))
			{ // help wanted
				Console.WriteLine("{0}\t\t\t\t\tMarkus Scholtes, 2020\n", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("Console program to determine the events of all event logs ordered by time.\n");
				Console.WriteLine("{0} [[-logname:]<LOGNAMES>] [-level:<LEVEL>]", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("    [-starttime:<STARTTIME>] [-endtime:<ENDTIME>] [-computername:<COMPUTER>]");
				Console.WriteLine("    [-filename:<FILENAME>] [-csv] [-grid] [-quiet] [-?|-help]");
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
				Console.WriteLine("-domainname:<DOMAIN> name of windows domain to logon (can be abbreviated as");
				Console.WriteLine("    -domain or -d). Default is to pass through current credentials.");
				Console.WriteLine("-username:<USER> name of windows user to logon (can be abbreviated as -user or");
				Console.WriteLine("    -u). Default is to pass through current credentials.");
				Console.WriteLine("-password:<PASSWORD> password of windows user to logon (can be abbreviated as");
				Console.WriteLine("    -pass or -p). Default is to pass through current credentials.");
				Console.WriteLine("-filename:<FILENAME> name of the file in which the results are output (can be");
				Console.WriteLine("    abbreviated as -file or -f). Default is output to the console.");
				Console.WriteLine("-csv output format \"comma separated\" instead of output format text.");
				Console.WriteLine("-grid output to GridView instead of console (can be abbreviated as -g).");
				Console.WriteLine("-quiet shows only error messages and results (can be abbreviated as -q).");
				Console.WriteLine("-? or -help shows this help (can be abbreviated as -h).");
				Console.WriteLine("\nExamples:");
				Console.WriteLine("{0} -start:10:00 -end:11:00", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} -start:10:00 -end:11:00 /GRID", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} System,Setup,Application -Computer=REMOTESYSTEM", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} /logname=Application /level:2 /q /CSV /file:OnlyErrors.csv", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} \"/starttime:2019/11/29 10:00\" \"/endtime:2019/11/29 11:00\"", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} \"/s=2019/12/08 10:09:49.450\" \"/e=2019/12/08 10:09:49.850\"", System.AppDomain.CurrentDomain.FriendlyName);
				Console.WriteLine("{0} /log=Security -Computer=REMOTE /D:DOM /U:Admin /P=NoP@ss", System.AppDomain.CurrentDomain.FriendlyName);
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

			EventLogSession session;
			string userName = parameter.ValueOrDefault("u", parameter.ValueOrDefault("user", parameter.Value("username")));
			if (userName == "")
				// connect to remote computer in parameter /c, /computer or /computername or to "localhost" with existent credentials
				session = new EventLogSession(parameter.ValueOrDefault("c", parameter.ValueOrDefault("computer", parameter.ValueOrDefault("computername", "localhost"))));
			else
			{
				string domainName = parameter.ValueOrDefault("d", parameter.ValueOrDefault("domain", parameter.Value("domainname")));
				string passWord = parameter.ValueOrDefault("p", parameter.ValueOrDefault("pass", parameter.Value("password")));

				// connect to remote computer in parameter /c, /computer or /computername or to "localhost" with credentials in parameters
				session = new EventLogSession(parameter.ValueOrDefault("c", parameter.ValueOrDefault("computer", parameter.ValueOrDefault("computername", "localhost"))), domainName, userName, new System.Net.NetworkCredential("", passWord).SecurePassword, SessionAuthentication.Default);
			}

			int outputMode = 1; // 1 - Text, 2 - CSV, 3 - Grid
			if (parameter.Exist("csv")) outputMode = 2;
			if (parameter.Exist("grid") || parameter.Exist("g")) outputMode = 3;

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
				if (GetEntries(ref eventList, name, session, startTime, endTime, maxLevel, bQuiet)) logCount++;
			}

			if (eventList.Count > 0)
			{ // only write events if there are any
				if (fileName == "")
				{ // no file output
					switch (outputMode)
					{
						case 1: // text output to console
							// write header line
							Console.WriteLine("time created\tlog\tid\tsource\tlevel\tdescription");
							foreach (EventEntry entry in eventList.OrderBy(a => a.timeCreated))
							{ // output events sorted by time
								Console.WriteLine(entry.timeCreated.ToString() + "\t" + entry.logName + "\t" + entry.eventId.ToString() + "\t" + entry.sourceName + "\t" + entry.eventLevel + "\t" + entry.description.Replace("\n", "\n\t"));
							}
							break;

						case 2: // csv output to console
							// write header line
							Console.WriteLine("\"time created\";\"log\";\"id\";\"source\";\"level\";\"description\"");
							foreach (EventEntry entry in eventList.OrderBy(a => a.timeCreated))
							{ // output events sorted by time
								Console.WriteLine("\"" + entry.timeCreated.ToString() + "\";\"" + entry.logName + "\";" + entry.eventId.ToString() + ";\"" + entry.sourceName + "\";\"" + entry.eventLevel + "\";\"" + entry.description.Replace("\"", "\"\"") + "\"");
							}
							break;

						case 3:	// output to gridview
							ShowGridView(eventList.OrderBy(a => a.timeCreated).ToList());
							break;
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
								if (outputMode == 1)
									sw.WriteLine(entry.timeCreated.ToString() + "\t" + entry.logName + "\t" + entry.eventId.ToString() + "\t" + entry.sourceName + "\t" + entry.eventLevel + "\t" + entry.description.Replace("\n", "\n\t"));
								else
									sw.WriteLine("\"" + entry.timeCreated.ToString() + "\";\"" + entry.logName + "\";" + entry.eventId.ToString() + ";\"" + entry.sourceName + "\";\"" + entry.eventLevel + "\";\"" + entry.description.Replace("\"", "\"\"") + "\"");
							}
						}
						if (fs != null) fs.Dispose();
					}
					catch (Exception e)
					{ // error on writing to file
						Console.Error.WriteLine("Error writing to file \"" + fileName + "\": " + e.Message);
						return 1;
					}
				}
			}

			if (!bQuiet) Console.WriteLine("Successfully processed " + eventList.Count + " events from " + logCount + " logs, access errors with " + (int)(logNames.Count - logCount) + " logs.");
			return 0;
		}

		private static void ShowGridView(List<EventEntry> eventList)
		{
			// use local setting for display of time and date in grid
			FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));
      
			// XAML string defining the window controls
			string strXAML = @"
<local:CustomWindow
	xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
	xmlns:local=""clr-namespace:***NAMESPACE***;assembly=***ASSEMBLY***""
	xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
	x:Name=""Window"" Title=""GetAllEvents"" WindowStartupLocation=""CenterScreen""
	Width=""1280"" Height=""640"" ShowInTaskbar=""True"">
	 <Grid>
		<DataGrid Name=""dgList"" AutoGenerateColumns=""False"" IsReadOnly=""True"" SelectionMode=""Extended"" SelectionUnit=""FullRow"">
			<DataGrid.Columns>
				<DataGridTextColumn Header=""Time created"" Binding=""{Binding timeCreated}"" />
				<DataGridTextColumn Header=""Log name"" Binding=""{Binding logName}"" />
				<DataGridTextColumn Header=""Event Id"" Binding=""{Binding eventId}"" />
				<DataGridTextColumn Header=""Source"" Binding=""{Binding sourceName}"" />
				<DataGridTextColumn Header=""Level"" Binding=""{Binding eventLevel}"" />
				<DataGridTextColumn Header=""Description"" Binding=""{Binding description}"" />
			</DataGrid.Columns>
      <DataGrid.CommandBindings>
      	<CommandBinding Command=""{x:Static ApplicationCommands.Copy}"" CanExecute=""IsCopyPossible"" Executed=""CopyDataRows""/>
      </DataGrid.CommandBindings>
      <DataGrid.ContextMenu>
      	<ContextMenu>
        	<MenuItem Command=""{x:Static ApplicationCommands.Copy}"" Header=""Copy""/>
        </ContextMenu>
      </DataGrid.ContextMenu>			
		</DataGrid>
	</Grid>
</local:CustomWindow>";

			// generate WPF object tree
			CustomWindow objWindow;
			DataGrid dgList;
			try
			{	// assign XAML root object
				objWindow = CustomWindow.LoadWindowFromXaml(strXAML.Replace("***ASSEMBLY***", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name).Replace("***NAMESPACE***", System.Reflection.Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace));
				dgList = (DataGrid)objWindow.FindName("dgList");
				dgList.ItemsSource = eventList;
			}
			catch (Exception ex)
			{ // on error in XAML definition XamlReader sometimes generates an exception
				MessageBox.Show("Error creating the window objects from XAML description\r\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// and show window
			objWindow.ShowDialog();
		}

		#region reading event log
		public class EventEntry
		{ // record for list of event entries
			public DateTime timeCreated { get; set; }
			public string logName { get; set; }
			public int eventId { get; set; }
			public string sourceName { get; set; }
			public string eventLevel { get; set; }
			public string description { get; set; }

			public EventEntry(DateTime timeStamp, string log, int id, string source, string level, string message)
			{
					timeCreated = timeStamp;
					logName = log;
					eventId = id;
					sourceName = source;
					eventLevel = level;
					description = message;
			}
		}

		private static bool GetEntries(ref List<EventEntry> eventList, string logName, EventLogSession session, DateTime startTime, DateTime endTime, int maxLevel, bool bQuiet)
		{ // read all event log entries matching in EventEntry list
			
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

					try
					{
						if (!String.IsNullOrEmpty(eventRecord.FormatDescription()))
						{ // create log entry in list
							eventList.Add(new EventEntry(timeCreated, logName, eventRecord.Id, eventRecord.ProviderName, eventLevel, eventRecord.FormatDescription()));
						}
						else
						{ // description not available, try to interpret raw data
							string rawDescription = "";
							foreach (EventProperty eventProperty in eventRecord.Properties)
							{
								rawDescription += eventProperty.Value.ToString();
							}
							// create log entry in list
							eventList.Add(new EventEntry(timeCreated, logName, eventRecord.Id, eventRecord.ProviderName, eventLevel, rawDescription));
						}
					}
					catch (Exception e)
					{ // create log entry in list with error message
						eventList.Add(new EventEntry(timeCreated, logName, eventRecord.Id, eventRecord.ProviderName, eventLevel, "### Error reading the event log entry: " + e.Message));
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

	#region WPF application
	public class CustomWindow : Window
	{
		// create window object out of XAML string
		public static CustomWindow LoadWindowFromXaml(string xamlString)
		{ // Get the XAML content from a string.
			// prepare XML document
			XmlDocument XAML = new XmlDocument();
			// read XAML string
			XAML.LoadXml(xamlString);
			// and convert to XML
			XmlNodeReader XMLReader = new XmlNodeReader(XAML);
			// generate WPF object tree
			CustomWindow objWindow = (CustomWindow)XamlReader.Load(XMLReader);

			// return CustomWindow object
			return objWindow;
		}

		// helper function that "climbs up" the parent object chain from a window object until the root window object is reached
		private FrameworkElement FindParentWindow(object sender)
		{
			FrameworkElement GUIControl = (FrameworkElement)sender;
			while ((GUIControl.Parent != null) && (GUIControl.GetType() != typeof(CustomWindow)))
			{
				GUIControl = (FrameworkElement)GUIControl.Parent;
			}

			if (GUIControl.GetType() == typeof(CustomWindow))
				return GUIControl;
			else
				return null;
		}

    private void CopyDataRows(object sender, ExecutedRoutedEventArgs e)
    { // copy marked lines to clipboard
    	string toClipBoard = "";
    	// read marked lines and convert to string
    	foreach (GetAllEvents.EventEntry item in ((DataGrid)sender).SelectedItems)
 				toClipBoard += item.timeCreated.ToString() + "\t" + item.logName + "\t" + item.eventId.ToString() + "\t" + item.sourceName + "\t" + item.eventLevel + "\t" + item.description.Replace("\n", "\n\t") + "\n";

    	Clipboard.SetDataObject(toClipBoard);
    }

    private void IsCopyPossible(object sender, CanExecuteRoutedEventArgs e)
    { // only when lines are marked
			e.CanExecute = ((DataGrid)sender).SelectedItems.Count > 0;
    }
	} // end of CustomWindow
	#endregion
}
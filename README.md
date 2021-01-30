# GetAllEvents
**Version 1.0.0.2, 2020-01-12 by Markus Scholtes**

C# command line tool to query the events of all event logs ordered by time and export to text or csv file.

**New in V1.0.0.2:** Display in graphical (WPF) grid view.

****

## Download:
Download binaries from the Script Center web page
[GetAllEvents: Query all events from all event logs](https://github.com/MScholtes/TechNet-Gallery) or compile yourself...

## Generate:
Compile with Compile.bat (no visual studio needed, but .Net 4.0).

**GetAllEventsCLI.cs** is a version without graphical (WPF) support because on systems without **Desktop Experience** installed the *"WPF version"* crashes.

There is a .Net 3.5 version in folder Net3.5 that lacks grid view and credential support (obviously needs .Net 3.5 to compile and run).

## Description:
C# command line tool to query the events of all event logs ordered by time in text or csv format.

A remote computer can be accessed.

## Parameters:
(/ can be used instead of the leading -, = can be used instead of the value introducing :)

**-logname:\<LOGNAMES\>** comma separated list of event log names.<br />Queries all event logs if omitted (can be abbreviated as -log or -l or can be omitted).

**-level:\<LEVEL\>** queries up to level \<LEVEL\>. Queries all events if omitted.<br />Level: Critical - 1, Error - 2, Warning - 3, Informational - 4, Verbose - 5

**-starttime:\<STARTTIME\>** start time of events to query (can be abbreviated as -start or -s).<br />Default is end time minus one hour.

**-endtime:\<ENDTIME\>** end time of events to query (can be abbreviated as -end or -e).<br />Default is now.

**-computername:\<COMPUTER\>** name of computer to query (can be abbreviated as -computer or -c).<br />Default is the local system.

**-domainname:\<DOMAIN\>** name of windows domain to logon (can be abbreviated as -domain or -d). Default is to pass through current credentials.

**-username:\<USER\>** name of windows user to logon (can be abbreviated as -user or -u). Default is to pass through current credentials.

**-password:\<PASSWORD\>** password of windows user to logon (can be abbreviated as -pass or -p). Default is to pass through current credentials.

**-filename:\<FILENAME\>** name of the file in which the results are output (can be abbreviated as -file or -f).<br />Default is output to the console.

**-csv** output format "comma separated" instead of output format text.

**-grid** output to GridView instead of console (can be abbreviated as -g).

**-quiet** shows only error messages and results (can be abbreviated as -q).

**-?** or **-help** shows help (can be abbreviated as -h).

## Examples:
```
GetAllEvents.exe -start:10:00 -end:11:00
GetAllEvents.exe -start:10:00 -end:11:00 /Grid
GetAllEvents.exe System,Setup,Application -Computer=REMOTESYSTEM
GetAllEvents.exe /logname=Application /level:2 /q /CSV /file:OnlyErrors.csv
GetAllEvents.exe "/starttime:2019/11/29 10:00" "/endtime:2019/11/29 11:00"
GetAllEvents.exe "/s=2019/12/08 10:09:49.450" "/e=2019/12/08 10:09:49.850"
GetAllEvents.exe /log=Security -Computer=REMOTE /D:DOM /U:Admin /P=NoP@ss
```

## History:
### 1.0.0.2 / 2020-01-12
Display of results in Grid view (WPF)

### 1.0.0.1 / 2019-12-12
Remote system access with credentials

### 1.0.0.0 / 2019-11-29
Initial release

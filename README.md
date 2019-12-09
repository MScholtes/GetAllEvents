# GetAllEvents
V1.0.0.0, 2019-12-08

C# command line tool to query the events of all event logs ordered by time and export to text or csv file.

****

## Generate:
Compile with Compile.bat (no visual studio needed, but .Net 4.0).

.Net 3.5 version in folder Net3.5 (obviously needs .Net 3.5 to compile and run).

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

**-filename:\<FILENAME\>** name of the file in which the results are output (can be abbreviated as -file or -f).<br />Default is output to the console.

**-csv** output format "comma separated" instead of output format text.

**-quiet** shows only error messages and results (can be abbreviated as -q).

**-?** or **-help** shows help (can be abbreviated as -h).

## Examples:
```
GetAllEvents.exe -start:10:00 -end:11:00
GetAllEvents.exe System,Setup,Application -Computer=REMOTESYSTEM
GetAllEvents.exe /level:2 /q /CSV /file:OnlyErrors.csv
GetAllEvents.exe "/starttime:2019/11/29 10:00" "/endtime:2019/11/29 11:00"
GetAllEvents.exe "/s=2019/12/08 10:09:49.450" "/e=2019/12/08 10:09:49.850"
```

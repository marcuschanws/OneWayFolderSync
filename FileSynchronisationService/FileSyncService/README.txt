================================================
File Synchronisation Service Console Application
================================================

Tool Overview:
--------------
This tool synchronises the contents of a source folder with a destination folder at specified time intervals. The destination folder will be a full, identical copy of the source folder after the periodic synchronisation is executed.

Usage:
------
To run the application, you need to provide the following input parameters (in any order):
1. Source folder file path
2. Destination folder file path
3. Log folder file path
4. Time interval for synchronisation (in seconds)

If any of the above input parameters are omitted, the ones from App.config will be used.

Example Commands: 
-----------------
	source="C:\Path\To\SourceFolder" destination="C:\Path\To\DestinationFolder" log="C:\Path\To\LogFolder" interval=60

Should you wish to omit only the destination folder:
	source="C:\Path\To\SourceFolder" log="C:\Path\To\LogFolder" interval=60

Should you wish to omit the time interval:
	source="C:\Path\To\SourceFolder" destination="C:\Path\To\DestinationFolder" log="C:\Path\To\LogFolder"

Should you wish to omit all input parameters and the values provided in the configuration file will be used. If invalid values are found, the tool will ask you to re-enter the values.

Functionality:
--------------
- The tool will start by checking the contents of the source folder.
- It will copy any new or updated files from the source folder to the destination folder.
- It will delete any files in the destination folder that do not exist in the source folder.
- The tool will repeat this process at the specified time interval (in seconds).
- File creation/copying/removal operations will be logged to a file in the specified log folder and to the console output

Notes:
------
- Ensure that the destination folder is not being used by another process during synchronisation to avoid conflicts.
- If a large amount of data needs to be synchronised, it is advisable to set a longer interval to prevent write permission conflict errors.
- This tool requires read and write permissions for both the source and destination folders.
- If an error occurs during synchronisation, the tool will log the error and attempt to continue with the next scheduled synchronisation.

================================================
To load the installer .vdproj you need to get "Microsoft Visual Studio 2013 Installer Projects" from nuget, or from this link
https://visualstudiogallery.msdn.microsoft.com/9abe329c-9bba-44a1-be59-0fbf6151054d

How to get find and install the Awesomium redistributable .msm file.
1.  Run the Awesomium setup
2.  Select Modify.
3.  Select Custom installation.
4.  Check Redistribution modules.
5.  The file will be located in [AwesomiumInstallDir]\Awesomium SDK\1.7.5.1\wrappers\Awesomium.NET\Redistribute\AWENET0175F.msm
Note: The default installation directory is C:\Program Files(x86)\Awesomium Technologies LLC

When you build OSBIDE.Installer in Debug, you will find "OSBLEDependencies.msi" in 
[development folder]\OSBIDE.Installer\Debug\OSBLEDependencies.msi
This installer will work regardless of it being Debug or Release, no constants change here
If the build is done in release instead of debug, then the installer will be located in the same directory except
instead of the Debug folder it will be in the Release folder.

When you build OSBIDE.Plugins.VS2013 in debug mode you will find "OSBLE_VisualStudio_Extension.vsix" in this folder:
[development folder]\OSBLEPlus.VSPlugin\OSBIDE.Plugins.VS2013\bin\debug\OSBLE_VisualStudio_Extension.vsix
This version of the plugin installer will work with constants of localhost:8088 and localhost:8087

For a version that uses https://plus.osble.org you want to change the dropdown next to "Start" (to run debugging)
from Debug to Release.  
The .vsix installer will be closed in the same directory above except instead of Debug it will be in the Release folder.

The two files above will be needed for future students to run the extension.

Install OSBLEDependencies first (if you do not have awesomium installed), then run the .vsix installer second.

Double clicking the .vsix file will install the extension into visual studio.
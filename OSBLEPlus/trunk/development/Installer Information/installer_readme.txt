To load the installer .vdproj you need to get "Microsoft Visual Studio 2013 Installer Projects" from nuget, or from this link
https://visualstudiogallery.msdn.microsoft.com/9abe329c-9bba-44a1-be59-0fbf6151054d

How to get find and install the Awesomium redistributable .msm file.
1.  Run the Awesomium setup
2.  Select Modify.
3.  Select Custom installation.
4.  Check Redistribution modules.
5.  The file will be located in [AwesomiumInstallDir]\Awesomium SDK\1.7.5.1\wrappers\Awesomium.NET\Redistribute\AWENET0175F.msm
Note: The default installation directory is C:\Program Files(x86)\Awesomium Technologies LLC

When you build OSBIDE.Installer, you will find "OSBLEDependencies.msi" in 
[OSBLEPlus.VSPlugin project folder]\OSBLEPlus.VSPlugin\OSBIDE.Installer\Debug\OSBLEDependencies.msi

When you build OSBIDE.Plugins.VS2013 you will find "OSBLE_VisualStudio_Extension.vsix"
[OSBLEPlus.VSPlugin project folder]\OSBIDE.Plugins.VS2013\bin\debug\OSBLE_VisualStudio_Extension.vsix

The two files above will be needed for future students to run the extension.

Double clicking the .vsix file will install the extension into visual studio.  If you have not installed Awesomium or OSBLEDependecies
the extension will not have all the activity feed options available.
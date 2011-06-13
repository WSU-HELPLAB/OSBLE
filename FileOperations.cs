using System;
using System.Collections.ObjectModel;
using System.IO;
using FileUploader.OsbleServices;

namespace FileUploader
{
    public static class FileOperations
    {
        /// <summary>
        /// Constructs a DirectoryListing of the supplied filesystem path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static DirectoryListing BuildLocalDirectoryListing(string path, bool includeParentLink = true, bool performRecursiveCall = false)
        {
            DirectoryListing listing = new DirectoryListing();
            listing.Directories = new ObservableCollection<DirectoryListing>();
            listing.Files = new ObservableCollection<FileListing>();
            listing.Name = path.Substring(path.LastIndexOf('\\') + 1);
            listing.AbsolutePath = path;

            //Add a parent directory "..." at the top of every directory listing if requested
            if (includeParentLink)
            {
                listing.Directories.Add(new ParentDirectoryListing() { Name = "..." });
            }

            //apparently, some folders are restricted.  Use a try block to catch
            //read exceptions
            try
            {
                //add files first
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    FileListing fileListing = new FileListing();
                    fileListing.LastModified = File.GetLastWriteTime(file);
                    fileListing.Name = System.IO.Path.GetFileName(file);
                    fileListing.AbsolutePath = file;
                    listing.Files.Add(fileListing);
                }

                //add other directories
                foreach (string folder in Directory.EnumerateDirectories(path))
                {
                    
                    //often, we don't want to go recursive
                    if (!performRecursiveCall)
                    {
                        DirectoryListing dList = new DirectoryListing();
                        dList.Files = new ObservableCollection<FileListing>();
                        dList.Directories = new ObservableCollection<DirectoryListing>();
                        dList.LastModified = Directory.GetLastWriteTime(folder);
                        dList.Name = folder.Substring(folder.LastIndexOf('\\') + 1);
                        listing.Directories.Add(dList);
                    }
                    else
                    {
                        DirectoryListing dList = BuildLocalDirectoryListing(folder, includeParentLink, performRecursiveCall);
                        listing.Directories.Add(dList);
                    }
                }
            }
            catch (Exception ex)
            {
                //something went wrong, oh well (for now)
            }
            return listing;
        }
    }
}

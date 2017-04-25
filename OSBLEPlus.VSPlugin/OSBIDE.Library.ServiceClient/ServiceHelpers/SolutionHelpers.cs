using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnvDTE;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public static class SolutionHelpers
    {
        public static List<CodeDocument> GetSolutionFiles(Solution solution)
        {
            var files = new List<CodeDocument>();
            return solution.Projects
                           .Cast<Project>()
                           .Aggregate(files, (current, project) =>
                                              current
                                             .Union(GetProjectFiles(project))
                                             .ToList());
        }

        public static List<CodeDocument> GetProjectFiles(Project project)
        {
            return GetProjectItemFiles(project.ProjectItems);
        }

        //AC Note: since we're currently using C/C++, just keep those files
        private static readonly string[] AllowedExtensions = { ".c", ".cpp", ".h", ".cs" };
        private static List<CodeDocument> GetProjectItemFiles(IEnumerable items)
        {
            var files = new List<CodeDocument>();
            foreach (ProjectItem item in items)
            {
                if (item.SubProject != null)
                {
                    files = files.Union(GetProjectItemFiles(item.ProjectItems)).ToList();
                }
                else if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    files = files.Union(GetProjectItemFiles(item.ProjectItems)).ToList();
                }
                else
                {
                    var fileName = item.Name;
                    var extension = Path.GetExtension(fileName);
                    if (!AllowedExtensions.Contains(extension)) continue;

                    //AC Note: This will not save an unopened file.  Is this desired behavior?
                    if (item.Document != null)
                    {
                        files.Add(DocumentFactory.FromDteDocument(item.Document));
                    }
                }
            }
            return files;
        }
    }
}

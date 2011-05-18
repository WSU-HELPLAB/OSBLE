using System;
using System.Reflection;

namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class LanguageFactory
    {
        /// <summary>
        /// This goes throw the enum Language and finds the Language that has the
        /// attribute FileExtensionAttribute that has the value that matches FileExtension
        /// this returns a class that inhierts from ILanguage which is assoicated with
        /// the fileExtension that was passed in or the NUllLanguage if no class such class
        /// exists
        /// </summary>
        /// <param name="fileExtension">a known file extension</param>
        /// <returns>A Language Class assoicated with the given string or NullLanguage if no such
        /// class exists</returns>
        public static ILanguage LanguageFromFileExtension(string fileExtension)
        {
            //an index Languages start explicitly defined as 0
            int i = 0;

            //we set language to i (0) so the first enum in Languages
            Languages language = (Languages)i;

            //we then enter this loop which exists when i gets bigger than the number of elements
            //in the enum Languages
            while (Enum.IsDefined(typeof(Languages), i))
            {
                Type type = language.GetType();

                FieldInfo fi = type.GetField(language.ToString());

                //we get the attriutes of the selected language
                FileExtensionAttribute[] attrs = (fi.GetCustomAttributes(typeof(FileExtensionAttribute), false) as FileExtensionAttribute[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is FileExtensionAttribute)
                {
                    //we get the first attribues value which should be the fileExtension
                    foreach (string s in attrs[0].Value)
                    {
                        if (fileExtension == s)
                        {
                            //if it makes the fileExtension passed in create an ILanguage using the Language Selector
                            return LanguageSelector(language);
                        }
                    }
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirment
                    throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
                }

                //didn't find it so try the next one
                i++;
                language = (Languages)i;
            }

            //we did not find the language so return NullLanguage
            return new NullLanguage();
        }

        /// <summary>
        /// Pass in a entry of the enum Language and it returns an instance of the class assoicated with that entry.
        /// </summary>
        /// <param name="lang">The languages which you want a class off</param>
        /// <returns>An instace of Ilanguage of the type requested</returns>
        public static ILanguage LanguageSelector(Languages lang)
        {
            ILanguage Ilang;
            if (lang == Languages.C)
            {
                Ilang = new CLanguage();
            }
            else if (lang == Languages.CPlusPlus)
            {
                Ilang = new CPlusPlusLanguage();
            }
            else if (lang == Languages.CSharp)
            {
                Ilang = new CSharpLanguage();
            }
            else if (lang == Languages.Java)
            {
                Ilang = new JavaLanguage();
            }
            else if (lang == Languages.Python)
            {
                Ilang = new PythonLanguage();
            }

                /*for new languages add the following here
                 *
                 * else if(lang == Languages.NewLangaugeName)
                 * {
                 *      Ilang = new NewLangaugeClass();
                 * }
                 *
                 * where NewLangaugeName has been added to the enum Langauges
                 * and where NewLangaugeClass is a new class that inheirts from ILanguage
                 */

            else
            {
                Ilang = new NullLanguage();
            }
            return Ilang;
        }
    }
}
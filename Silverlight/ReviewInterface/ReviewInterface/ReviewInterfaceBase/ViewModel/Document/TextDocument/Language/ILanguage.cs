namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    /// <summary>
    /// All supported languages.
    /// Note: When adding a new language you need to add the name here, with a Attribue
    /// FileExtensionAttribute that has the assosiated file type. You also need to add
    /// a class file that inhierts from ILanguage, and add an entry to LanguageFactorty.LanguageSelector function
    /// </summary>
    public enum Languages
    {
        [FileExtensionAttribute(new string[] { ".c", ".h" })]
        C = 0,

        [FileExtensionAttribute(new string[] { ".cpp", ".h" })]
        CPlusPlus,

        [FileExtensionAttribute(new string[] { ".cp" })]
        CSharp,

        [FileExtensionAttribute(new string[] { ".py" })]
        Python,

        [FileExtensionAttribute(new string[] { ".java" })]
        Java
    };

    /// <summary>
    /// This stores a string (FileExtension) and is used to decorate the Enum entries in Languages with a FileExtension
    /// </summary>
    public class FileExtensionAttribute : System.Attribute
    {
        private string[] _fileExensions;

        public FileExtensionAttribute(string[] fileExensions)
        {
            _fileExensions = fileExensions;
        }

        public string[] Value
        {
            get { return _fileExensions; }
        }
    }

    /// <summary>
    /// All LanguageClasses must inherit from ILanguage
    /// Note: When adding a new language you need to add the name to the enum Languages, with a Attribute
    /// FileExtensionAttribute that has the associated file type. You also need to add
    /// a class file that inherits from this class, and add an entry to LanguageFactorty.LanguageSelector function
    /// </summary>
    public interface ILanguage
    {
        string[] WhiteSpaceOnlyBefore
        {
            get;
        }

        string[] KeyWords
        {
            get;
        }

        string[] InlineComments
        {
            get;
        }

        string StartMultiLineComment
        {
            get;
        }

        string EndMultiLineComment
        {
            get;
        }
    }
}
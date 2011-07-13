namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class NullLanguage : ILanguage
    {
        public string[] WhiteSpaceOnlyBefore
        {
            get { return new string[] { "" }; }
        }

        public string[] KeyWords
        {
            get { return new string[] { "" }; }
        }

        public string[] InlineComments
        {
            get { return new string[] { "" }; }
        }

        public string StartMultiLineComment
        {
            get { return new string('a', 0); }
        }

        public string EndMultiLineComment
        {
            get { return new string('a', 0); }
        }
    }
}
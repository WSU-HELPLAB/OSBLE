namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class PythonLanguage : ILanguage
    {
        private string[] pounds = new string[]
        {
        };

        public string[] WhiteSpaceOnlyBefore
        {
            get { return pounds; }
        }

        private string[] keyWords = new string[]
        {
            "and",
            "del",
            "from",
            "not",
            "while",
            "as",
            "elif",
            "global",
            "or",
            "with",
            "assert",
            "else",
            "if",
            "pass",
            "yield",
            "break",
            "except",
            "import",
            "print",
            "class",
            "exec",
            "in",
            "raise",
            "continue",
            "finally",
            "is",
            "return",
            "def",
            "for",
            "lambda",
            "try"
        };

        public string[] KeyWords
        {
            get { return keyWords; }
        }

        private string[] inlineComments = new string[] { "#" };

        public string[] InlineComments
        {
            get { return inlineComments; }
        }

        private string startMultiLineComment = null;

        public string StartMultiLineComment
        {
            get { return startMultiLineComment; }
        }

        private string endMultiLineComment = null;

        public string EndMultiLineComment
        {
            get { return endMultiLineComment; }
        }
    }
}
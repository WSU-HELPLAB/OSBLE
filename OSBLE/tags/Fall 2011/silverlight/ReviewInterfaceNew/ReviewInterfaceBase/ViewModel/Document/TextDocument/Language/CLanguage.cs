namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class CLanguage : ILanguage
    {
        private string[] poundsDef = new string[]
        {
            "#include",
            "#define",
            "#ifndef",
            "#endif",
        };

        public string[] WhiteSpaceOnlyBefore
        {
            get { return poundsDef; }
        }

        private string[] keyWords = new string[]
        {
            "auto",
            "break",
            "case",
            "char",
            "const",
            "continue",
            "default",
            "do",
            "double",
            "else",
            "enum",
            "extern",
            "float",
            "for",
            "goto",
            "if",
            "int",
            "long",
            "register",
            "return",
            "short",
            "signed",
            "sizeof",
            "static",
            "struct",
            "switch",
            "typedef",
            "union",
            "unsigned",
            "void",
            "volatile",
            "while"
        };

        public string[] KeyWords
        {
            get { return keyWords; }
        }

        private string[] inlineComments = new string[] { "//" };

        public string[] InlineComments
        {
            get { return inlineComments; }
        }

        private string startMultiLineComment = "/*";

        public string StartMultiLineComment
        {
            get { return startMultiLineComment; }
        }

        private string endMultiLineComment = "*/";

        public string EndMultiLineComment
        {
            get { return endMultiLineComment; }
        }
    }
}
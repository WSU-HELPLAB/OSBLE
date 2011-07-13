namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class CSharpLanguage : ILanguage
    {
        private string[] pounds = new string[]
        {
            "#include",
            "#define",
            "#if",
            "#else",
            "#end",
        };

        public string[] WhiteSpaceOnlyBefore
        {
            get { return pounds; }
        }

        private string[] keyWords = new string[]
        {
            "abstract",
            "as",
            "base",
            "bool",
            "break",
            "byte",
            "case",
            "catch",
            "char",
            "checked",
            "class",
            "const",
            "continue",
            "decimal",
            "default",
            "delegate",
            "do",
            "double",
            "else",
            "enum",
            "event",
            "explicit",
            "extern",
            "false",
            "finally",
            "fixed",
            "float",
            "for",
            "foreach",
            "goto",
            "if",
            "implicit",
            "in",
            "int",
            "interface",
            "internal",
            "is",
            "lock",
            "long",
            "namespace",
            "new",
            "null",
            "object",
            "operator",
            "out",
            "override",
            "params",
            "private",
            "protected",
            "public",
            "readonly",
            "ref",
            "return",
            "sbyte",
            "sealed",
            "short",
            "sizeof",
            "stackalloc",
            "static",
            "string",
            "struct",
            "switch",
            "this",
            "throw",
            "true",
            "try",
            "typeof",
            "uint",
            "ulong",
            "unchecked",
            "unsafe",
            "ushort",
            "using",
            "virtual",
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
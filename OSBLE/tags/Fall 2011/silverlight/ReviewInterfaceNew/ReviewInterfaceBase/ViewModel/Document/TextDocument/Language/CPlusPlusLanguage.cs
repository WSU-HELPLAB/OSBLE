namespace ReviewInterfaceBase.ViewModel.Document.TextFileDocument.Language
{
    public class CPlusPlusLanguage : ILanguage
    {
        private string[] pounds = new string[]
        {
            "#include",
            "#define",
            "#ifndef",
            "#end",
        };

        public string[] WhiteSpaceOnlyBefore
        {
            get { return pounds; }
        }

        private string[] keyWords = new string[]
        {
            "and",
            "default",
            "noexcept",
            "template",
            "and_eq",
            "delete",
            "not",
            "this",
            "alignof",
            "double",
            "not_eq",
            "thread_local",
            "asm",
            "dynamic_cast",
            "nullptr",
            "throw",
            "auto",
            "else",
            "operator",
            "true",
            "bitand",
            "enum	",
            "or	",
            "try",
            "bitor",
            "explicittodo",
            "or_eq",
            "typedef",
            "bool",
            "export",
            "private",
            "typeid",
            "break",
            "externtodo",
            "protected",
            "typename",
            "case",
            "false",
            "public",
            "union",
            "catch",
            "float",
            "register",
            "using",
            "char",
            "for",
            "reinterpret_cast",
            "unsigned",
            "char16_t",
            "friend",
            "return",
            "void",
            "char32_t",
            "goto",
            "short",
            "wchar_t",
            "class",
            "if",
            "signed",
            "virtual",
            "compl",
            "inline",
            "sizeof",
            "volatile",
            "const",
            "int	",
            "static",
            "while",
            "constexpr",
            "long",
            "static_assert",
            "xor",
            "const_cast",
            "mutable",
            "static_cast",
            "xor_eq",
            "continue",
            "namespace",
            "struct	",
            "decltype",
            "new	",
            "switch "
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
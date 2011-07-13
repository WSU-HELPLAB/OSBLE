namespace ReviewInterfaceBase.ViewModel.FindWindow
{
    public enum SearchIn
    {
        [DisplyStringAttribute("Current Document Only")]
        CurrentDocumentOnly,

        [DisplyStringAttribute("Current Comments Only")]
        CurrentCommentsOnly,

        [DisplyStringAttribute("Current Document and Comments")]
        CurrentDocumentAndComments,

        [DisplyStringAttribute("All Documents")]
        AllDocuments,

        [DisplyStringAttribute("All Comments")]
        AllComments,

        [DisplyStringAttribute("All Documents and Comments")]
        AllDocumentsAndComments
    }

    /// <summary>
    /// This stores a string and is used to decorate the Enum entries in SearchIn with a DisplyStringAttribute
    /// </summary>
    public class DisplyStringAttribute : System.Attribute
    {
        private string _displayString;

        public DisplyStringAttribute(string displayString)
        {
            _displayString = displayString;
        }

        public string Value
        {
            get { return _displayString; }
        }
    }
}
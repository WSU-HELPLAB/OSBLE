namespace OSBLEPlus.Logic.Utility.Lookups
{
    public class NameValuePair
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public NameValuePair() { } // NOTE!! This is required by Dapper ORM
    }
}

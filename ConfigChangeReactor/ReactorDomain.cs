namespace ConfigChangeReactor
{
    public static class ReactorDomain
    {
        public delegate void ChangeHandler(Dictionary<string, string> cnfg);
        static ChangeHandler? Change;
        public static void Subscribe(ChangeHandler ch)
        {
            Change += ch;
        }
        public static void Unsubscribe(ChangeHandler ch)
        {
            Change -= ch;
        }
        public static void InvokeConfigChange(Dictionary<string, string> cnfg)
        {
            Change?.Invoke(cnfg);
        }
    }
}

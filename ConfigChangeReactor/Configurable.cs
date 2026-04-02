namespace ConfigChangeReactor
{
    public class Configurable
    {
        public virtual void ChangeHandler(Dictionary<string, string> cnfg) { }
        public virtual Task Dispose()
        {
            ReactorDomain.Unsubscribe(ChangeHandler);
            return Task.CompletedTask;
        }
    }
}

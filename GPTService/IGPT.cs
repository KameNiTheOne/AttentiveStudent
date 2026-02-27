namespace GPTService
{
    public interface IGPT
    {
        public Task<string> Query(string query, CancellationToken ct);
    }
}

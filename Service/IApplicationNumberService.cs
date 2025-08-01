namespace PerizinanPeternakan.Service
{
    public interface IApplicationNumberService
    {
        Task<string> GenerateApplicationNumberAsync();

        bool IsValidApplicationNumberFormat(string applicationNumber);

        Task<int> GetNextSequenceNumberAsync();

        Task<bool> ApplicationNumberExistsAsync(string applicationNumber);
    }
}

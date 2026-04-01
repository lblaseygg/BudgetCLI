using System.Text.Json;
using System.Text.Json.Serialization;
using BudgetCLI.Models;

namespace BudgetCLI.Services;

public class TransactionFileService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public TransactionFileService(string filePath)
    {
        _filePath = filePath;
    }

    public string FilePath => _filePath;

    public async Task<List<Transaction>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<Transaction>();

        await using var stream = File.OpenRead(_filePath);
        var transactions = await JsonSerializer.DeserializeAsync<List<Transaction>>(stream, _jsonOptions);

        return transactions ?? new List<Transaction>();
    }

    public async Task AddAsync(Transaction transaction)
    {
        var transactions = await GetAllAsync();
        transactions.Add(transaction);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, transactions, _jsonOptions);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var transactions = await GetAllAsync();
        var transactionToRemove = transactions.FirstOrDefault(t => t.Id == id);

        if (transactionToRemove is null)
            return false;

        transactions.Remove(transactionToRemove);

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, transactions, _jsonOptions);

        return true;
    }
}

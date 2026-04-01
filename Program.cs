using BudgetCLI.Models;
using BudgetCLI.Services;
using Spectre.Console;
using System.CommandLine;

var dataFilePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "transactions.json");
var transactionService = new TransactionFileService(dataFilePath);

var descriptionOption = new Option<string>("--description", "-d")
{
    Description = "What the transaction was for",
    Required = true
};

var amountOption = new Option<decimal>("--amount", "-a")
{
    Description = "Transaction amount",
    Required = true
};

var typeOption = new Option<string>("--type", "-t")
{
    Description = "income or expense",
    Required = true
};
typeOption.AcceptOnlyFromAmong("income", "expense");

var addCommand = new Command("add", "Add a transaction");
addCommand.Options.Add(descriptionOption);
addCommand.Options.Add(amountOption);
addCommand.Options.Add(typeOption);

addCommand.SetAction(async parseResult =>
{
    var description = parseResult.GetRequiredValue(descriptionOption);
    var amount = parseResult.GetRequiredValue(amountOption);
    var typeText = parseResult.GetRequiredValue(typeOption);

    if (amount <= 0)
    {
        AnsiConsole.MarkupLine("[red]Amount must be greater than 0.[/]");
        return 1;
    }

    var transaction = new Transaction
    {
        Description = description,
        Amount = amount,
        Type = Enum.Parse<TransactionType>(typeText, true)
    };

    await transactionService.AddAsync(transaction);

    AnsiConsole.MarkupLine("[green]Transaction saved.[/]");
    AnsiConsole.WriteLine($"Id: {transaction.Id}");
    AnsiConsole.WriteLine($"File: {transactionService.FilePath}");

    return 0;
});

var rootCommand = new RootCommand("Simple budget tracker");
rootCommand.Subcommands.Add(addCommand);

return await rootCommand.Parse(args).InvokeAsync();

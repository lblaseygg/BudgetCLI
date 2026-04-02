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

var categoryFilterOption = new Option<string>("--category", "-c")
{
    Description = "Filter by category"
};

categoryFilterOption.AcceptOnlyFromAmong(
    Enum.GetNames<Category>().Select(name => name.ToLower()).ToArray()
);

var monthFilterOption = new Option<int>("--month", "-m")
{
    Description = "Filter by month (1-12)",
    DefaultValueFactory = _ => 0
};

// command definitions
var addCommand = new Command("add", "Add a transaction");
addCommand.Options.Add(descriptionOption);
addCommand.Options.Add(amountOption);
addCommand.Options.Add(typeOption);

addCommand.SetAction(async parseResult =>
{
    var description = parseResult.GetRequiredValue(descriptionOption);
    var amount = parseResult.GetRequiredValue(amountOption);
    var typeText = parseResult.GetRequiredValue(typeOption);
    var selectedCategory = AnsiConsole.Prompt(
        new SelectionPrompt<Category>()
            .Title("Select a [green]category[/]:")
            .AddChoices(Enum.GetValues<Category>())
    );

    if (amount <= 0)
    {
        AnsiConsole.MarkupLine("[red]Amount must be greater than 0.[/]");
        return 1;
    }

    var transaction = new Transaction
    {
        Description = description,
        Amount = amount,
        Type = Enum.Parse<TransactionType>(typeText, true),
        Category = selectedCategory
    };

    await transactionService.AddAsync(transaction);

    AnsiConsole.MarkupLine("[green]Transaction saved.[/]");
    AnsiConsole.WriteLine($"Id: {transaction.Id}");
    AnsiConsole.WriteLine($"File: {transactionService.FilePath}");

    return 0;
});


var listCommand = new Command("list", "List all transactions");
listCommand.Options.Add(categoryFilterOption);
listCommand.Options.Add(monthFilterOption);

listCommand.SetAction(async parseResult =>
{
    var transactions = await transactionService.GetAllAsync();
    var categoryFilter = parseResult.GetValue(categoryFilterOption);
    var monthFilter = parseResult.GetValue(monthFilterOption);

    if (monthFilter is < 0 or > 12)
    {
        AnsiConsole.MarkupLine("[red]Invalid month filter. Must be between 1 and 12.[/]");
        return 1;
    }

    if (!string.IsNullOrWhiteSpace(categoryFilter))
    {
        var selectedCategory = Enum.Parse<Category>(categoryFilter, true);
        transactions = transactions
            .Where(t => t.Category == selectedCategory)
            .ToList();
    }

    if (monthFilter > 0)
    {
        transactions = transactions
            .Where(t => t.Date.Month == monthFilter)
            .ToList();
    }

    if (transactions.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No transactions found.[/]");
        return 0;
    }
    
    var table = new Table();
    table.Border = TableBorder.Minimal;

    table.AddColumn(new TableColumn("[grey]id[/]"));
    table.AddColumn(new TableColumn("[grey]date[/]"));
    table.AddColumn(new TableColumn("[grey]description[/]"));
    table.AddColumn(new TableColumn("[grey]type[/]"));
    table.AddColumn(new TableColumn("[grey]category[/]"));
    table.AddColumn(new TableColumn("[grey]amount[/]") { Alignment = Justify.Right });

    foreach (var transaction in transactions.OrderByDescending(t => t.Date))
    {
        var amountColor = transaction.Type == TransactionType.Income ? "green" : "red";
        var shortId = transaction.Id.ToString()[..8];
        var typeText = transaction.Type.ToString().ToLower();

        table.AddRow(
            $"[grey]{shortId}[/]",
            $"[grey]{transaction.Date:MM/dd/yy}[/]",
            transaction.Description,
            $"[grey]{typeText}[/]",
            $"[grey]{transaction.Category.ToString().ToLower()}[/]",
            $"[{amountColor}]${transaction.Amount:F2}[/]"
        );
    }

    AnsiConsole.Write(table);
    return 0;
});


var idOption = new Option<Guid>("--id", "-i")
{
    Description = "Transaction ID",
    Required = true
};

var deleteCommand = new Command("delete", "Delete a transaction");
deleteCommand.Options.Add(idOption);

deleteCommand.SetAction(async parseResult =>
{
    var id = parseResult.GetRequiredValue(idOption);
    var deleted = await transactionService.DeleteAsync(id);

    if (!deleted)
    {
        AnsiConsole.MarkupLine("[red]Transaction not found.[/]");
        return 1;
    }
    AnsiConsole.MarkupLine("[green]Transaction deleted.[/]");
    return 0;
});


// command line parsing
var rootCommand = new RootCommand("CLI Budget Tracker");
rootCommand.Subcommands.Add(addCommand);
rootCommand.Subcommands.Add(listCommand);
rootCommand.Subcommands.Add(deleteCommand);
return await rootCommand.Parse(args).InvokeAsync();

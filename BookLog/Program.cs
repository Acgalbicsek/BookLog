using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;



namespace BookLog
{
    public class BookEntry
    {
        public string UserName { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public int Pages { get; set; }
        public DateTime DateFinished { get; set; }  
    }

    public class BookLedger
    {
        public List<BookEntry> Entries { get; set; } = new();
        public int PaidThroughPages { get; set; } = 0;          
        public DateTime? LastPaidDate { get; set; } = null;

        public int TotalPages() => SumPages(Entries);
        public static int SumPages(IEnumerable<BookEntry> items)
        {
            int sum = 0;
            foreach (var e in items) sum += e.Pages;
            return sum;
        }


        public class BookLog
        {
            public int TotalPagesRead { get; set; }
            public int RemainderPages { get; private set; }

            // Example rate per 100 pages
            private const decimal RatePer100Pages = 5.00m;

            public decimal CalculateInvoice()
            {
                // Add remainder from last invoice, if any
                int totalWithRemainder = TotalPagesRead + RemainderPages;

                // Determine full sets of 100 pages
                int billableSets = totalWithRemainder / 100;

                // Determine remainder for next invoice
                RemainderPages = totalWithRemainder % 100;

                // Calculate amount due for full 100-page sets
                decimal amountDue = billableSets * RatePer100Pages;

                // Reset total pages read after cashing out
                TotalPagesRead = 0;

                return amountDue;
            }
        }



        public int UnpaidPages() => Math.Max(0, TotalPages() - PaidThroughPages);

        // $1 per 100 pages (rounded down).
        public decimal AmountOwedDollars()
        {
            int hundreds = UnpaidPages() / 100; 
            return hundreds * 1m;
        }

        public void MarkPaid()
        {
            PaidThroughPages = TotalPages();
            LastPaidDate = DateTime.Now;
        }

        public void ResetAll()
        {
            Entries.Clear();
            PaidThroughPages = 0;
            LastPaidDate = null;
        }
    }


    internal static class Program
    {
        
        
            
         private static readonly string DataFile = Path.Combine(AppContext.BaseDirectory, "booklog.json");
         private static readonly string ExportTxt = Path.Combine(AppContext.BaseDirectory, "BookLog_Export.txt");

            private static BookLedger _ledger = new();
        

        private static void Main()
        {
            Load();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== BOOK LOG ===");
                Console.WriteLine("1) Add entry");
                Console.WriteLine("2) List entries");
                Console.WriteLine("3) Show totals & amount owed");
                Console.WriteLine("4) Export to Notepad (.txt) for printing");
                Console.WriteLine("5) Mark current balance as PAID");
                Console.WriteLine("6) RESET (clear all data)");
                Console.WriteLine("7) Save & Exit");
                Console.Write("Choose: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        AddEntry();
                        break;
                    case "2":
                        ListEntries();
                        break;
                    case "3":
                        ShowTotals();
                        break;
                    case "4":
                        ExportToTxtAndOpen();
                        break;
                    case "5":
                        MarkPaid();
                        break;
                    case "6":
                        ResetAll();
                        break;
                    case "7":
                        Save();
                        Console.WriteLine("Saved. Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Press any key...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        // === Menu actions ===

        private static void AddEntry()
        {
            Console.Clear();
            Console.WriteLine("=== Add Book Entry ===");

            string user = Prompt("Your name");
            string title = Prompt("Book title");
            string author = Prompt("Author");

            int pages = PromptInt("Number of pages (whole number, >= 0)", min: 0);

            DateTime dateFinished = PromptDate(
                "Date finished (YYYY-MM-DD). Leave blank for today",
                allowBlankForToday: true
            );

            var entry = new BookEntry
            {
                UserName = user,
                Title = title,
                Author = author,
                Pages = pages,
                DateFinished = dateFinished
            };

            _ledger.Entries.Add(entry);
            Save();

            Console.WriteLine("Entry added! Press any key...");
            Console.ReadKey();
        }

        private static void ListEntries()
        {
            Console.Clear();
            Console.WriteLine("=== Entries ===");

            if (_ledger.Entries.Count == 0)
            {
                Console.WriteLine("(no entries yet)");
            }
            else
            {
                int i = 1;
                foreach (var e in _ledger.Entries)
                {
                    Console.WriteLine($"{i++}. {e.UserName} | \"{e.Title}\" by {e.Author} | {e.Pages} pages | finished {e.DateFinished:yyyy-MM-dd}");
                }
            }

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }

        private static void ShowTotals()
        {
            Console.Clear();
            Console.WriteLine("=== Totals ===");

            int totalPages = _ledger.TotalPages();
            int unpaidPages = _ledger.UnpaidPages();
            decimal owed = _ledger.AmountOwedDollars();

            Console.WriteLine($"Total pages (all time): {totalPages:N0}");
            Console.WriteLine($"Paid-through pages:     {_ledger.PaidThroughPages:N0}");
            Console.WriteLine($"Unpaid pages:           {unpaidPages:N0}");
            Console.WriteLine($"Amount owed ($1/100p):  ${owed:N0}");

            if (_ledger.LastPaidDate.HasValue)
                Console.WriteLine($"Last paid date:         {_ledger.LastPaidDate:yyyy-MM-dd HH:mm}");

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }

        private static void ExportToTxtAndOpen()
        {
            Console.Clear();
            Console.WriteLine("=== Export ===");

            var sb = new StringBuilder();
            sb.AppendLine("BOOK LOG");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine();

            if (_ledger.Entries.Count == 0)
            {
                sb.AppendLine("(no entries yet)");
            }
            else
            {
                int i = 1;
                foreach (var e in _ledger.Entries)
                {
                    sb.AppendLine($"{i++}. {e.UserName} | \"{e.Title}\" by {e.Author}");
                    sb.AppendLine($"    {e.Pages} pages | finished {e.DateFinished:yyyy-MM-dd}");
                }
            }

            sb.AppendLine();
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Total pages:        {_ledger.TotalPages():N0}");
            sb.AppendLine($"Paid-through pages: {_ledger.PaidThroughPages:N0}");
            sb.AppendLine($"Unpaid pages:       {_ledger.UnpaidPages():N0}");
            sb.AppendLine($"Amount owed:        ${_ledger.AmountOwedDollars():N0}");
            if (_ledger.LastPaidDate.HasValue)
                sb.AppendLine($"Last paid date:     {_ledger.LastPaidDate:yyyy-MM-dd HH:mm}");

            File.WriteAllText(ExportTxt, sb.ToString());

            Console.WriteLine($"Exported to: {ExportTxt}");
            TryOpenNotepad(ExportTxt);

            Console.WriteLine("\nPress any key...");
            Console.ReadKey();
        }

        private static void MarkPaid()
        {
            Console.Clear();
            int unpaidPages = _ledger.UnpaidPages();
            decimal owed = _ledger.AmountOwedDollars();

            Console.WriteLine("=== Mark Paid ===");
            Console.WriteLine($"You currently owe ${owed:N0} for {unpaidPages:N0} unpaid pages.");
            Console.Write("Confirm mark as PAID? (y/n): ");
            var c = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (c == "y" || c == "yes")
            {
                _ledger.MarkPaid();
                Save();
                Console.WriteLine("Marked as PAID.");
            }
            else
            {
                Console.WriteLine("Canceled.");
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        private static void ResetAll()
        {
            Console.Clear();
            Console.WriteLine("=== RESET ===");
            Console.Write("This will erase ALL entries and payment status. Continue? (type RESET): ");
            var text = Console.ReadLine();
            if (text?.Trim().ToUpperInvariant() == "RESET")
            {
                _ledger.ResetAll();
                Save();
                Console.WriteLine("All data cleared.");
            }
            else
            {
                Console.WriteLine("Canceled.");
            }

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        // === Small helpers ===

        private static string Prompt(string label)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                var s = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(s))
                    return s.Trim();
                Console.WriteLine("Please enter a value.");
            }
        }

        private static int PromptInt(string label, int? min = null, int? max = null)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                var s = Console.ReadLine();
                if (int.TryParse(s, out int value))
                {
                    if (min.HasValue && value < min.Value)
                    {
                        Console.WriteLine($"Value must be >= {min.Value}.");
                        continue;
                    }
                    if (max.HasValue && value > max.Value)
                    {
                        Console.WriteLine($"Value must be <= {max.Value}.");
                        continue;
                    }
                    return value;
                }
                Console.WriteLine("Please enter a whole number.");
            }
        }

        private static DateTime PromptDate(string label, bool allowBlankForToday = false)
        {
            while (true)
            {
                Console.Write($"{label}: ");
                var s = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(s) && allowBlankForToday)
                    return DateTime.Today;

                if (DateTime.TryParse(s, out var dt))
                    return dt.Date;

                Console.WriteLine("Please enter a valid date (e.g., 2025-10-16).");
            }
        }

        private static void TryOpenNotepad(string path)
        {
            try
            {
                // Launch Notepad on Windows
                Process.Start(new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{path}\"",
                    UseShellExecute = true
                });
            }
            catch
            {
                // If not available, silently continue.
            }
        }

        // === Persistence ===

        private static void Load()
        {
            try
            {
                if (File.Exists(DataFile))
                {
                    var json = File.ReadAllText(DataFile);
                    var loaded = JsonSerializer.Deserialize<BookLedger>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (loaded != null)
                        _ledger = loaded;
                }
            }
            catch
            {
                // If corrupt, start fresh but keep the old file around.
            }
        }

        private static void Save()
        {
            var json = JsonSerializer.Serialize(_ledger, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(DataFile, json);
        }
    }
}
    


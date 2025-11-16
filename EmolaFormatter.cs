using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class EmolaFormatter
{
    public static string GetAccount(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;
        bool isPayment = message.Contains("Movitel,SA", StringComparison.OrdinalIgnoreCase);
        if(isPayment)
            return "_";
        // Expressão que cobre todos os padrões mencionados
        var match = Regex.Match(
            message,
            @"(?i)(?:Agente\s+com\s+codigo\s+ID|de\s+conta|para\s+conta)[\s,]+(\d+)(?=[\s,]*(?:nome|Nome|nome:|,|\.))",
            RegexOptions.Singleline
        );

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return string.Empty;
    }



    public static string GetName(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;
        bool isPayment = message.Contains("Movitel,SA", StringComparison.OrdinalIgnoreCase);
        if(isPayment)
            return "Movitel";
        // Regex que captura texto entre "nome" (ou "Nome:") e "as"
        var match = Regex.Match(
            message,
            @"(?i)\bnome[: ]+(.+?)\s+as\b", // (?i) = ignore case, .+? = não guloso
            RegexOptions.Singleline
        );

        if (match.Success)
        {
            var name = match.Groups[1].Value.Trim();

            // Remove possíveis caracteres extras no final, tipo ponto ou vírgula
            name = Regex.Replace(name, @"[.,]+$", "").Trim();

            return name;
        }

        return string.Empty;
    }

    public static double GetTaxValue(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return 0;

        var match = Regex.Match(
            message,
            @"Taxa[: ]+([\d.,]+)\s*MT",
            RegexOptions.IgnoreCase
        );

        if (match.Success)
        {
            var value = match.Groups[1].Value.Replace(",", "").Trim();
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double tax))
                return tax;
        }

        return 0; // retorna 0 se não encontrar
    }


    public static long GetTimestamp(string message)
    {
        long timestamp = 0;

        // 1️⃣ Verifica se é pagamento (Movitel)
        bool isPayment = message.Contains("Movitel,SA", StringComparison.OrdinalIgnoreCase);

        // 2️⃣ Expressões diferentes para casos diferentes
        Regex regex;
        string dateStr = null;

        if (isPayment)
        {
            // Exemplo de mensagem: "A 14:32 05/11/2025. Movitel,SA ..."
            regex = new Regex(@"A\s*(?<time>\d{1,2}:\d{2})\s*(?<date>\d{1,2}/\d{1,2}/\d{4})");
            var match = regex.Match(message);
            if (match.Success)
            {
                // ⚠️ Aqui o formato de hora é HH:mm, não HH:mm:ss
                dateStr = $"{match.Groups["date"].Value} {match.Groups["time"].Value}";
                if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    timestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                }
            }
        }
        else
        {
            // Exemplo de mensagem: "as 12:34:56 de 05/11/2025 ..."
            regex = new Regex(@"as\s+(\d{2}:\d{2}:\d{2})\s+de\s+(\d{2}/\d{2}/\d{4})");
            var match = regex.Match(message);
            if (match.Success)
            {
                dateStr = $"{match.Groups[2].Value} {match.Groups[1].Value}";
                if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    timestamp = new DateTimeOffset(date).ToUnixTimeSeconds();
                }
            }
        }

        return timestamp;
    }



    public static string GetSid(string message)
    {
        bool isPayment = message.Contains("Movitel,SA", StringComparison.OrdinalIgnoreCase);
        string sid = "";
        if(!isPayment)
        {
            var sidMatch = Regex.Match(message, @"ID da transacao[: ]+([A-Za-z0-9.\-]+)");
            sid = sidMatch.Success ? sidMatch.Groups[1].Value : "";            
        }else{
            var sidMatch = Regex.Match(message, @"ID da Transacao:\s*(?<id>\S+)[\.\s]+");
            sid = sidMatch.Success ? sidMatch.Groups[1].Value : "";    
        }

        return sid;
    }



    public static double GetAmount(string message)
    {
        bool isPayment = message.Contains("Movitel,SA", StringComparison.OrdinalIgnoreCase);
        double amount;
        if(!isPayment)
        {
            // 3️⃣ Capturar valores e campos principais
            var amountMatch = Regex.Match(message, @"([\d.,]+)MT");
            amount = amountMatch.Success ? ParseNumber(amountMatch.Groups[1].Value) : 0;          
        }else{
            // 3️⃣ Capturar valores e campos principais
            var amountMatch = Regex.Match(message, @"Efectuou\s+um\s+pagamento\s+de\s*(?<amount>[\d\.,]+)\s*MT\s*para\s*(?<payee>.+?)\.\s");
            amount = amountMatch.Success ? ParseNumber(amountMatch.Groups[1].Value) : 0;    
        }

        return amount;
    }

    public static List<TransactionDto> Parse(List<TransactionParserDto> transactionDtos)
    {
        List<TransactionDto> transactions = new List<TransactionDto>();

        foreach (var transactionDto in transactionDtos)
        {
            if (string.IsNullOrWhiteSpace(transactionDto.Message))
                return null;

            var message = transactionDto.Message.Replace("\n", " ").Trim();

            var sid = GetSid(message);
            // 2️⃣ Detectar tipo de mensagem
            bool isReceived = message.Contains("Recebeste", StringComparison.OrdinalIgnoreCase);

            double tax = GetTaxValue(message);

            string name = GetName(message);
            string account = GetAccount(message);
            long timestamp = GetTimestamp(message);
            double amount = GetAmount(message);

            if(name == "" || account == "" || timestamp == 0 || amount == 0)
            {

            }else{
                transactions.Add(new TransactionDto
                {
                    Sid = sid,
                    Name = name,
                    Account = account,
                    Amount = amount,
                    Tax = tax,
                    IsReceived = isReceived,
                    Date = timestamp
                });                
            }

        }

        return transactions;
    }

    private static double ParseNumber(string value)
    {
        return double.TryParse(value.Replace(",", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0;
    }
}

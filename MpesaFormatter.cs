using System;
using System.Globalization;
using System.Text.RegularExpressions;

public static class MpesaFormatter
{
    public static long GetTimestamp(string message)
    {
        // Extrai data e hora
        var match = Regex.Match(
            message,
            @"(\d{1,2}/\d{1,2}/\d{2,4}).*?\b(a?s|às)\b\s*(\d{1,2}:\d{2}(?:\s?(AM|PM))?)",
            RegexOptions.IgnoreCase
        );

        if (!match.Success)
            return 0;

        string datePart = match.Groups[1].Value; // Ex: 8/11/25
        string timePart = match.Groups[3].Value.ToUpper(); // Ex: 5:01 PM

        // Todos os formatos possíveis
        string[] formats =
        {
            "d/M/yy",
            "dd/M/yy",
            "d/MM/yy",
            "dd/MM/yy",
            "d/M/yyyy",
            "dd/M/yyyy",
            "d/MM/yyyy",
            "dd/MM/yyyy"
        };

        DateTime dateParsed;

        // Tenta todos os formatos
        if (!DateTime.TryParseExact(
                datePart,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out dateParsed))
        {
            return 0;
        }

        // Junta data + hora em formato fixo
        string final = dateParsed.ToString("dd/MM/yyyy") + " " + timePart;

        DateTime finalDate;

        // Tenta AM/PM
        if (!DateTime.TryParseExact(
                final,
                new[] { "dd/MM/yyyy h:mm tt", "dd/MM/yyyy hh:mm tt" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out finalDate))
        {
            // Tenta 24h
            if (!DateTime.TryParseExact(
                    final,
                    "dd/MM/yyyy HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out finalDate))
            {
                return 0;
            }
        }

        return new DateTimeOffset(finalDate).ToUnixTimeSeconds();
    }



    public static double GetTaxValue(string message)
    {
        var match = Regex.Match(message, @"taxa.*?(\d+[.,]\d{1,2})\s*MT", RegexOptions.IgnoreCase);

        if (match.Success && double.TryParse(match.Groups[1].Value.Replace(",", "."), 
                NumberStyles.Any, CultureInfo.InvariantCulture, out var tax))
            return tax;

        return 0;
    }


    public static double GetAmount(string message)
    {
        if(message.Contains("Depositaste", StringComparison.OrdinalIgnoreCase))
        {
            var match1 = Regex.Match(
            message,
            @"Depositaste\s+o\s+valor\s+de\s+([\d.,]+)MT",
            RegexOptions.IgnoreCase
            );

            if (!match1.Success)
                return 0;

            double.TryParse(
                match1.Groups[1].Value.Replace(",", "").Replace(".", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var amount1
            );

            return amount1;
        }else if(message.Contains("Compraste", StringComparison.OrdinalIgnoreCase) || message.Contains("atraves do M-Pesa", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(message, @"no valor de\s+([\d.,]+)MT", RegexOptions.IgnoreCase);

            if (!match.Success) return 0;

            double.TryParse(match.Groups[1].Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount);
            return amount;
        } else {
            // Cash-out (levantaste)
            var match = Regex.Match(message,
                @"levantaste\s+(\d+[.,]\d{1,3})\s*MT",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                // Transferência P2P
                match = Regex.Match(message,
                    @"(?:Transferiste|Recebeste)\s+(\d+[.,]\d{1,3})\s*MT",
                    RegexOptions.IgnoreCase);
            }

            if (!match.Success)
            {
                // Depósito via agente
                match = Regex.Match(message,
                    @"Depositaste\s+o\s+valor\s+de\s+(\d+[.,]\d{1,3})\s*MT",
                    RegexOptions.IgnoreCase);
            }

            if (match.Success &&
                double.TryParse(match.Groups[1].Value.Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var amount))
                return amount;

            return 0;            
        }

    }




    public static string GetSid(string message)
    {
        if(message.Contains("Compraste", StringComparison.OrdinalIgnoreCase) && message.Contains("atraves do M-Pesa", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(message, @"(?<=\b)[A-Z0-9]{10,12}(?=\s+confirmado)", RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Trim() : "";
        } else {
            var match = Regex.Match(message, @"(?<=Confirmado\s)[A-Z0-9]{6,20}", RegexOptions.IgnoreCase);
            return match.Success ? match.Value : "";
        }
    }


    public static string GetName(string message)
    {
        if(message.Contains("Depositaste", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match(
                message,
                @"no agente\s+(.*?)\s+aos",
                RegexOptions.IgnoreCase
            );

            return match.Success ? match.Groups[1].Value.Trim() : "";
        }else if(message.Contains("Compraste", StringComparison.OrdinalIgnoreCase) || message.Contains("atraves do M-Pesa", StringComparison.OrdinalIgnoreCase))
        {
            return "Vodacom";
        } else {
            // Cash-Out via agente (levantaste)
            var agentMatch = Regex.Match(
                message,
                @"no agente\s+\d+\s*-\s*(.*?)\.",
                RegexOptions.IgnoreCase
            );

            if (agentMatch.Success)
                return agentMatch.Groups[1].Value.Trim();

            // Outros tipos de transação
            var match = Regex.Match(message, @"-\s*(.*?)\s+(?=aos\b)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "";            
        }

    }


    
    public static string GetAccount(string message)
    {
        if (message.Contains("Recebeste", StringComparison.OrdinalIgnoreCase) || message.Contains("Transferiste", StringComparison.OrdinalIgnoreCase))
        {
            // 2️⃣ P2P com formato internacional (258xxxxxxxx)
            var intlMatch = Regex.Match(message, @"\b258(\d{9})\b");
            return intlMatch.Groups[1].Value; // remove o 258
        }else if(message.Contains("Depositaste", StringComparison.OrdinalIgnoreCase))
        {
            return  " ";
        } else if(message.Contains("Compraste", StringComparison.OrdinalIgnoreCase) || message.Contains("atraves do M-Pesa", StringComparison.OrdinalIgnoreCase))
        {
            return "_";
        }else{
            // Agente
            var agentCode = Regex.Match(message, @"no agente\s+(\d+)", RegexOptions.IgnoreCase);
            if (agentCode.Success)
                return agentCode.Groups[1].Value;

            // P2P
            return Regex.Match(message, @"\b8\d{8}\b").Value;            
        }

    }


    public static List<TransactionDto> Parse(List<TransactionParserDto> transactionDtos)
    {
        List<TransactionDto> transactions = new List<TransactionDto>();

        foreach (var t in transactionDtos)
        {
            if (string.IsNullOrWhiteSpace(t.Message))
                continue;

            var message = t.Message.Replace("\n", " ").Trim();

            var sid = GetSid(message);
            bool isReceived = message.Contains("Recebeste", StringComparison.OrdinalIgnoreCase) ||
                  message.Contains("Depositaste", StringComparison.OrdinalIgnoreCase);
            var taxa = GetTaxValue(message);
            var nome = GetName(message);
            var numero = GetAccount(message);
            var data = GetTimestamp(message);
            var valor = GetAmount(message);

            if (nome == "" || numero == "" || data == 0 || valor == 0)
                continue;

            transactions.Add(new TransactionDto
            {
                Sid = sid,
                Name = nome,
                Account = numero,
                Amount = valor,
                Tax = taxa,
                IsReceived = isReceived,
                Date = data
            });
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

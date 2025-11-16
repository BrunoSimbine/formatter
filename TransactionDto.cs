using System.Text.Json;
using System.Text.Json.Serialization;

public class TransactionDto
{
	public string Sid { get; set; }
	public string Name { get; set; } 
	public string Account { get; set; } 
	public double Amount { get; set; }
	public double Tax { get; set; }
	public bool IsReceived { get; set; } 
	public long Date { get; set; }
}

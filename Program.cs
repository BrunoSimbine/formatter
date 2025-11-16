var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001);
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

app.MapPost("/api/v1/transaction/mpesa", 
    (List<TransactionParserDto> body) =>
{
    return MpesaFormatter.Parse(body);
})
.WithName("MpesaTransactionParser")
.WithOpenApi();

app.MapPost("/api/v1/transaction/emola", 
    (List<TransactionParserDto> body) =>
{
    return EmolaFormatter.Parse(body);
})
.WithName("EmolaTransactionParser")
.WithOpenApi();

app.Run();

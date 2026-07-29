using COM_STT_API.Data;
using COM_STT_API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DB AGMES: dùng cho toàn bộ nghiệp vụ, kể cả đăng nhập (join sang AGERP qua DB link DL_AGERP).
builder.Services.AddScoped<AgmesOracleService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<InspectionBatchService>();
builder.Services.AddScoped<ProdPlanService>();
builder.Services.AddScoped<InspectionHeadService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/check-db", async (AgmesOracleService agmes) =>
{
    try
    {
        await agmes.ExecuteQueryAsync("SELECT 1 FROM DUAL", r => 1);
        
        await agmes.ExecuteQueryAsync("SELECT 1 FROM DUAL", r => 1);
        return Results.Ok(new { success = true, message = "Kết nối AGMES THÀNH CÔNG!" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { success = false, message = "LỖI KẾT NỐI AGMES: " + ex.Message });
    }
});

app.MapControllers();

app.Run();

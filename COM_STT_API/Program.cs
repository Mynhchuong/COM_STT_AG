using COM_STT_API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// DB AGERP: chỉ dùng để xác thực đăng nhập.
builder.Services.AddScoped<AgerpOracleService>();
// DB AGMES: dùng cho toàn bộ nghiệp vụ còn lại.
builder.Services.AddScoped<AgmesOracleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/check-db", async (AgerpOracleService agerp, AgmesOracleService agmes) =>
{
    var result = new Dictionary<string, object>();

    try
    {
        await agerp.ExecuteQueryAsync("SELECT 1 FROM DUAL", r => 1);
        result["agerp"] = new { success = true, message = "Kết nối AGERP THÀNH CÔNG!" };
    }
    catch (Exception ex)
    {
        result["agerp"] = new { success = false, message = "LỖI KẾT NỐI AGERP: " + ex.Message };
    }

    try
    {
        await agmes.ExecuteQueryAsync("SELECT 1 FROM DUAL", r => 1);
        result["agmes"] = new { success = true, message = "Kết nối AGMES THÀNH CÔNG!" };
    }
    catch (Exception ex)
    {
        result["agmes"] = new { success = false, message = "LỖI KẾT NỐI AGMES: " + ex.Message };
    }

    return Results.Ok(result);
});

app.MapControllers();

app.Run();

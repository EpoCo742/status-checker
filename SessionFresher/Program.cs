using SessionFresher.Config;
using SessionFresher.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SessionServiceOptions>(builder.Configuration.GetSection("SessionService"));
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddHostedService(provider =>
    (SessionService)provider.GetRequiredService<ISessionService>());
builder.Services.AddSingleton<IStatusService, StatusService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

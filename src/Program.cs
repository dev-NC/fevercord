using FeverCordSvc;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddFeverCord(builder.Configuration);
builder.Services.AddJobScheduler(builder.Configuration);

var host = builder.Build();
host.Run();

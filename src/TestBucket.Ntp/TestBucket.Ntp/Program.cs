using MudBlazor.Services;
using Scalar.AspNetCore;
using TestBucket.Ntp.Components;
using TestBucket.Ntp.Core;
using TestBucket.Ntp.Core.Server;
using TestBucket.Ntp.Core.Testing;
using TestBucket.Ntp.Services;
using TestBucket.Ntp.Services.Upstream;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ITimeProvider, TestTimeProvider>();
builder.Services.AddSingleton<NtpServer>();
builder.Services.AddSingleton<IClientRuleRepository, FileSystemClientRuleRepository>();
builder.Services.AddHostedService<NtpServerLifetimeBackgroundService>();

// Upstream server time
builder.Services.AddSingleton<UpstreamTimeProvider>();
builder.Services.AddSingleton<TimeProvider>(serviceProvider => serviceProvider.GetRequiredService<UpstreamTimeProvider>());
builder.Services.AddHostedService<UpstreamServerTimeUpdater>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();

// If someone tries to access swagger, redirect them to the scalar API reference instead, since that's where the API docs are now. This is to avoid confusion and broken links for anyone who might have bookmarked the old swagger URL.
app.MapGet("/swagger", () => Results.Redirect("/scalar/v1", permanent: true)).ExcludeFromDescription();
app.MapGet("/swagger/{**rest}", () => Results.Redirect("/scalar/v1", permanent: true)).ExcludeFromDescription();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

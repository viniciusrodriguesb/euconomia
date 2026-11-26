using Api.Configuracao;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AdicionarPersistenciaFinanceira(builder.Configuration);
builder.Services.AdicionarCasosDeUso();
builder.Services.AdicionarSeguranca(builder.Environment, builder.Configuration);
builder.Services.AdicionarProxyReverso(builder.Configuration);
builder.Services.AdicionarLimitesAutenticacao();
builder.Services.AdicionarTratamentoDeErros();
builder.Services.AdicionarInterfaceHttp();

var app = builder.Build();

app.ConfigurarPipeline();
app.MapearRotas();

app.Run();

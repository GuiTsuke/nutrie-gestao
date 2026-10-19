# Nutriê Gestão

Solução para gestão de marmitas congeladas com duas interfaces independentes:

- `NutrieGestao`: aplicativo Desktop em WPF.
- `NutrieGestao.Web`: aplicativo Web responsivo em ASP.NET Core e Blazor.

## Estrutura

- `NutrieGestao.Core`: domínio compartilhado. Contém models, enums, repositórios JSON, `DataService`, `ManagementService` e cálculos de dashboard.
- `NutrieGestao`: interface WPF e MVVM, que referencia `NutrieGestao.Core`.
- `NutrieGestao.Web`: interface Blazor, design system CSS e Dashboard, que também referencia `NutrieGestao.Core`.

O WPF mantém o fallback local em `%LocalAppData%\NutrieGestao\Data`. A versão Web usa os caminhos definidos em `NutrieGestao.Web/appsettings.json`; na produção, os JSONs ficam em `E:\inetpub\Nutrie\NutrieGestao\Data`. Isso mantém o uso offline e permite separar os dados de desenvolvimento dos dados da publicação.

## Executar

Na raiz da solução:

```powershell
# Compilar as três partes da solução
dotnet build .\NutrieGestao.slnx

# Aplicativo desktop WPF
dotnet run --project .\NutrieGestao\NutrieGestao.csproj

# Aplicativo Web Blazor
dotnet run --project .\NutrieGestao.Web\NutrieGestao.Web.csproj
```

O endereço da Web é informado pelo terminal; em desenvolvimento, normalmente é `https://localhost:7085` e `http://localhost:5160`.

## Dados de demonstração

No primeiro cadastro pelo Desktop, são criados uma cliente, uma marmita e um pedido de exemplo. Para desativá-los antes da versão final, altere `CriarDadosDeExemploNoPrimeiroAcesso` para `false` em `NutrieGestao/ViewModels/AppViewModel.cs`. Para limpar dados já criados, remova apenas os JSONs de `%LocalAppData%\NutrieGestao\Data`.

## Estado atual da Web

A versão Web possui Dashboard, Clientes, Marmitas, Pedidos e Configurações. Os módulos incluem busca, cadastro, edição e exclusão com confirmação; marmitas também podem ser ativadas ou inativadas. O editor de pedidos permite adicionar produtos, ajustar quantidades, escolher pagamento, registrar pagamento parcial, editar status e preservar o preço histórico de cada item.

## Armazenamento da versão Web

O caminho da produção é configurado em `NutrieGestao.Web/appsettings.json`, na seção `NutrieGestao:Paths`. Com a configuração padrão, a aplicação usa `E:\inetpub\Nutrie\NutrieGestao\Data` para os JSONs, `E:\inetpub\Nutrie\NutrieGestao\Logs` para os logs diários e `E:\inetpub\Nutrie\NutrieGestao\Backups` para os backups.

O `appsettings.Development.json` aponta os testes para `%LocalAppData%\NutrieGestao\Development`, evitando gravações acidentais na produção. As pastas são criadas na inicialização. Os logs têm o formato `nutrie-AAAAMMDD.log` e registram inicialização, gravação dos JSONs, falhas de leitura/escrita e erros não tratados da Web.

## Preparação local para futura publicação

Requisitos confirmados: SDK .NET 10; Windows para o aplicativo WPF. A Web usa ASP.NET Core/Blazor; o núcleo compartilhado mantém persistência JSON. Esta preparação não executou nem validou as funcionalidades descritas acima.

Os appsettings não acompanham esta cópia. As referências anteriores a E:\inetpub descrevem o ambiente operacional; não use esses caminhos para testes. Antes de executar os comandos já documentados, confirme configuração isolada de desenvolvimento e o fluxo de primeiro acesso.

Na Web, foram confirmadas as chaves NutrieGestao:Paths:RootPath, NutrieGestao:Paths:Data, NutrieGestao:Paths:Logs e NutrieGestao:Paths:Backups, além de NutrieGestao:Security:BootstrapCode. Forneça os valores por configuração local fora do Git ou variáveis de ambiente, substituindo dois-pontos por dois sublinhados. Não coloque código de ativação real na documentação.

Não publicar JSONs reais de clientes, pedidos e usuários, logs, backups ou chaves de proteção de dados. Configuração e primeiro acesso permanecem pendentes de validação. O .gitignore não retira arquivos já rastreados. Nenhuma publicação foi feita.

## Verificacao final de preparacao

A solucao NutrieGestao.slnx (Core, WPF e Web) compilou com SDK .NET 10.0.400,
sem erros nem avisos. O projeto inicial Web e NutrieGestao.Web; a alternativa
Desktop e NutrieGestao/NutrieGestao.csproj e requer Windows. Nao ha
PackageReference externo nos tres projetos; usam os frameworks do .NET.

A Web foi iniciada com armazenamento isolado e respondeu HTTP 200 em
/primeiro-acesso, exibindo a protecao esperada enquanto BootstrapCode esta
vazio. Nenhum usuario foi criado; o processo de verificacao foi encerrado.
Nao foram acessados dados reais.

Copie NutrieGestao.Web/appsettings.Development.example.json para
NutrieGestao.Web/appsettings.Development.json. O exemplo usa uma pasta
Development em LocalAppData, separada dos dados operacionais. Nao publique
o arquivo local nem os JSONs gerados.

Antes de criar a primeira conta manualmente, defina
NutrieGestao__Security__BootstrapCode no ambiente do processo com um valor
privado escolhido pelo proprietario. O exemplo deixa o valor vazio por
seguranca. Execute a Web pelo perfil HTTPS do launchSettings e acesse
/primeiro-acesso. Use o certificado HTTPS local de desenvolvimento;
nao reutilize credenciais/dados de producao. Cadastro e login completos
nao foram exercitados para evitar criar usuarios.

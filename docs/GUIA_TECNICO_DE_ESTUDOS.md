# Guia técnico de estudos — Nutriê Gestão

Este é um guia para estudar desenvolvimento de software usando o Nutriê Gestão como projeto real. A ideia não é decorar arquivos: é aprender como as peças de C#, .NET e uma aplicação web se relacionam.

O sistema é pequeno o suficiente para ser compreendido por inteiro e completo o suficiente para mostrar práticas reais: interface, regras de negócio, autenticação, persistência, logs e publicação.

## 1. A visão que deve guiar o estudo

Antes de abrir qualquer arquivo, guarde este mapa mental:

```text
Pessoa usa o navegador
        ↓
Página Blazor recebe evento e mostra estado
        ↓
Service aplica regra de negócio e validação
        ↓
Repository lê ou grava JSON
        ↓
Arquivos na pasta de dados
```

Em paralelo existe um caminho de segurança:

```text
Login → valida credenciais → cria cookie → identifica usuário em cada acesso
```

O objetivo de arquitetura é que cada camada responda por uma pergunta diferente:

| Camada | Pergunta que ela responde |
| --- | --- |
| UI / Razor | “Como a pessoa vê e interage?” |
| Models | “Quais dados existem?” |
| Services | “Quais regras e operações são permitidas?” |
| Repositories | “Como os dados são guardados?” |
| Infraestrutura | “Como a aplicação inicia, autentica, registra e trata HTTP?” |

Se uma tela começa a salvar JSON sozinha, ou se um model começa a conhecer botão e CSS, a separação se perde. Esse é o principal problema que a estrutura do projeto evita.

## 2. Tecnologias usadas e por que existem

| Tecnologia | Uso no projeto | O que estudar |
| --- | --- | --- |
| C# | Linguagem de toda a regra e interface | classes, records, enums, LINQ, exceções, async/await, tipos anuláveis. |
| .NET 10 | Plataforma de compilação e execução | SDK, projetos `.csproj`, NuGet, build e publish. |
| ASP.NET Core | Servidor HTTP | pipeline, DI, middleware, endpoints, configuração. |
| Blazor Server | Interface web | componentes Razor, binding, eventos, rotas, estado e SignalR. |
| HTML/CSS/JS | Estrutura, estilo e pequenos comportamentos do navegador | formulários, responsividade, eventos e acessibilidade. |
| System.Text.Json | Persistência local | serialização, desserialização e compatibilidade de dados. |
| Criptografia .NET | Senhas, cookie e Data Protection | PBKDF2, salt, hash, cookies e antiforgery. |
| WPF | Interface desktop alternativa | XAML, binding e MVVM; não é a interface web atual. |

Não há banco de dados, API REST, Entity Framework ou dependências de front-end adicionais nesta versão. Isso é intencional: o foco é manter o MVP simples e offline.

## 3. Como o .NET organiza uma solução

Um arquivo `.slnx` reúne projetos. Neste repositório:

```text
NutrieGestao.slnx
├── NutrieGestao.Core        biblioteca com regras reutilizáveis
├── NutrieGestao.Web         aplicação web Blazor/ASP.NET Core
└── NutrieGestao             aplicação WPF Windows
```

### 3.1 O que é um projeto

Um projeto é definido pelo arquivo `.csproj`. Ele informa ao SDK:

- qual tipo de aplicação é construída;
- qual versão do .NET será usada (`net10.0` ou `net10.0-windows`);
- quais referências o projeto precisa;
- quais recursos especiais estão habilitados.

Exemplos reais:

- `NutrieGestao.Core.csproj` é uma biblioteca comum, sem interface.
- `NutrieGestao.Web.csproj` usa `Microsoft.NET.Sdk.Web` e referencia o Core.
- `NutrieGestao.csproj` habilita WPF com `<UseWPF>true</UseWPF>`.

Uma referência de projeto significa “este projeto pode usar classes do outro”. Por isso, o Web pode instanciar `ManagementService`, mas o Core não conhece páginas Razor. A dependência aponta para dentro, onde há menos detalhes de infraestrutura.

### 3.2 Build, restore, run e publish

| Comando | O que faz |
| --- | --- |
| `dotnet restore` | Baixa dependências declaradas pelo projeto. |
| `dotnet build` | Compila e encontra erros de C#. |
| `dotnet run` | Compila se necessário e inicia a aplicação. |
| `dotnet test` | Executa testes automatizados, quando houver projeto de testes. |
| `dotnet publish` | Monta os arquivos para implantação. |

No terminal, a rotina básica é:

```powershell
dotnet build .\NutrieGestao.slnx --nologo
dotnet run --project .\NutrieGestao.Web\NutrieGestao.Web.csproj --launch-profile http
```

`bin/` contém arquivos gerados para execução e `obj/` contém arquivos intermediários da compilação. Não estude nem edite esses diretórios; o código fonte está fora deles.

## 4. C# essencial visto no projeto

### 4.1 Classe, propriedade e objeto

`Pedido`, `Cliente`, `Marmita` e `Usuario` são classes. Elas descrevem o formato de um objeto.

```csharp
public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = string.Empty;
}
```

- `public`: outras classes podem acessar.
- `Guid`: identificador único; reduz chance de colisão entre registros.
- `{ get; set; }`: propriedade que pode ser lida e alterada.
- `= Guid.NewGuid()`: valor inicial quando um objeto nasce.
- `string.Empty`: string vazia, evitando `null` onde não é necessário.

### 4.2 Tipos anuláveis

O projeto habilita `<Nullable>enable</Nullable>`. Isso faz o compilador alertar quando um valor que pode ser `null` é usado como se nunca fosse nulo.

```csharp
public Empresa? LoadEmpresa()
```

O `?` informa que pode não haver empresa cadastrada. Quem chama precisa considerar esse caso:

```csharp
var empresa = Data.LoadEmpresa() ?? new Empresa();
```

O operador `??` significa “use o valor à esquerda; se for nulo, use o da direita”.

### 4.3 Enum: escolhas fechadas

Enums representam opções limitadas e evitam strings soltas:

```csharp
public enum StatusPedido { Novo, EmPreparacao, Pronto, Entregue, Cancelado }
```

O código interno usa `EmPreparacao`; a interface traduz para “Em preparação”. Esse detalhe separa linguagem técnica de linguagem de negócio.

### 4.4 Record: dados pequenos e imutáveis por intenção

`OperationResult`, `PasswordHash`, `ResultadoAutenticacao` e `UsuarioResumo` usam `record`.

```csharp
public sealed record PasswordHash(string Hash, string Salt, int Iterations);
```

Um record é útil para transportar um conjunto pequeno de dados, com igualdade por conteúdo e sintaxe curta. `sealed` impede herança, o que simplifica seu comportamento.

### 4.5 LINQ

LINQ é a sintaxe de consulta de coleções. Aparece em pedidos, dashboard e usuários.

```csharp
var ativos = usuarios
    .Where(usuario => usuario.Ativo)
    .OrderBy(usuario => usuario.Nome)
    .ToList();
```

Leia de cima para baixo: pega usuários, filtra ativos, ordena por nome e materializa uma lista.

Operações recorrentes no projeto:

| Método | Efeito |
| --- | --- |
| `Where` | Filtra. |
| `Select` | Transforma cada item. |
| `Any` | Verifica se existe ao menos um. |
| `FirstOrDefault` | Retorna o primeiro ou `null`/valor padrão. |
| `OrderBy` | Ordena crescente. |
| `Sum` | Soma valores. |
| `Aggregate` | Acumula com lógica personalizada. |
| `ToList` | Executa a consulta e cria lista concreta. |

### 4.6 Exceções

Exceções representam erros inesperados, como arquivo bloqueado ou JSON inválido. O projeto não deve mostrá-las cruas para o usuário.

```csharp
try
{
    return JsonSerializer.Deserialize<List<T>>(conteudo) ?? [];
}
catch (Exception exception)
{
    _logger.Error("Não foi possível ler o arquivo JSON.", exception);
    return [];
}
```

Observe a decisão: registrar detalhes técnicos para diagnóstico e devolver um estado seguro para a aplicação continuar estável. Não use `catch { }` silencioso, porque ele esconde a causa.

## 5. Models: o vocabulário do domínio

O domínio é a parte do código que representa o negócio de marmitas. Os models vivem em `NutrieGestao.Core/Models`.

### 5.1 Pedido e histórico

`Pedido` possui uma lista de `ItemPedido`. Cada item guarda nome, preço e ingredientes da marmita no momento em que o pedido é feito.

Esse padrão é uma **fotografia histórica**. Ele resolve esta pergunta: “se o preço da marmita mudar amanhã, o pedido de ontem deve mudar?” A resposta é não.

```text
Marmita atual: preço pode mudar
Pedido antigo: preço precisa continuar igual ao da venda
```

É por isso que não basta o pedido guardar apenas `MarmitaId`.

### 5.2 Dinheiro e `decimal`

Valores financeiros usam `decimal`:

```csharp
public decimal Preco { get; set; }
```

`double` e `float` trabalham em base binária e podem gerar aproximações inesperadas para decimal humano. `decimal` foi desenhado para esse tipo de cálculo.

Os cálculos do pedido são propriedades derivadas:

```text
ValorBruto     = soma dos subtotais
ValorDesconto  = desconto em R$ ou percentual
ValorTotal     = bruto − desconto
ValorPendente  = total − pago, nunca abaixo de zero
```

O modelo também protege multiplicações e somas contra `OverflowException`. Ainda assim, a defesa principal é validar valores antes de salvar.

### 5.3 Autoria

Clientes, marmitas e pedidos possuem campos de autoria:

```text
CriadoPorUsuarioId
CriadoPorNome
```

Eles são preenchidos apenas no primeiro salvamento. Uma edição posterior preserva o autor original. Isso é uma forma simples de auditoria, sem transformar o sistema em um ERP.

## 6. Services: regras de negócio e casos de uso

Services ficam em `NutrieGestao.Core/Services`. Eles impedem que a página vire uma mistura de tela, regra e arquivo.

### 6.1 `ManagementService`

É o serviço de operações de negócio para empresa, cliente, marmita e pedido. O padrão é:

```text
recebe entidade
  → valida
  → normaliza dados
  → carrega coleção atual
  → preserva regra/histórico/autoria
  → atualiza ou adiciona
  → salva coleção
  → devolve OperationResult
```

`OperationResult` evita usar exceção para uma situação comum de negócio, como “cliente sem nome” ou “marmita com histórico não pode ser excluída”. A página recebe `Success` e `Message` e mostra uma mensagem amigável.

### 6.2 `ValidacaoCadastro`

Este é um ótimo arquivo para estudar porque ele concentra regras simples, reutilizáveis e fáceis de testar:

- tamanho máximo de textos;
- obrigatoriedade de campos;
- telefone brasileiro com DDD;
- normalização/validação de e-mail;
- preço e desconto não negativos;
- desconto percentual até 100;
- pedido com cliente e itens;
- limites numéricos contra valores acidentais gigantes.

Uma validação central é importante porque o HTML pode ser manipulado pelo navegador. A regra do servidor é a decisão final.

### 6.3 Calculadoras

`DashboardCalculator`, `ConsultaCalculator` e `PedidoAnalysisCalculator` isolam fórmulas de relatório. Isso é melhor do que repetir `Sum` e filtros em cada página porque:

1. há uma fonte de verdade para a fórmula;
2. o cálculo pode receber teste unitário;
3. a interface fica focada em apresentação.

Exemplo de regra: pedidos cancelados são removidos da base de faturamento antes de somar valores recebidos.

## 7. Persistência: Repository e JSON

### 7.1 A abstração

`IRepository<T>` descreve a capacidade de carregar e salvar uma lista de um tipo. `JsonRepository<T>` é a implementação atual.

O `T` é um **tipo genérico**. Assim a mesma classe funciona para `Cliente`, `Marmita`, `Pedido` e `Usuario` sem duplicar código.

```csharp
new JsonRepository<Cliente>("clientes.json", options.DataPath, logger)
```

### 7.2 `DataService`

`DataService` sabe quais arquivos existem, mas não deixa páginas conhecerem a implementação de JSON:

```text
DataService.LoadClientes()
DataService.SaveClientes(...)
```

Essa é uma fronteira arquitetural útil. Caso o sistema migre para SQLite, as páginas e `ManagementService` precisam mudar pouco ou nada.

### 7.3 Escrita atômica simples

Ao salvar JSON, o repositório grava primeiro um temporário e depois substitui o arquivo final:

```text
clientes.json.tmp → clientes.json
```

Isso reduz o risco de um arquivo parcialmente escrito se o processo parar durante a gravação. Não substitui backup, mas é uma proteção prática de baixo custo.

### 7.4 Compatibilidade de dados

JSON antigo pode não conter campos criados depois. Em C#, propriedades com valores padrão mantêm a leitura compatível na maior parte dos casos.

Ao mudar formato de um arquivo existente:

1. faça backup;
2. preserve campos antigos quando possível;
3. aceite valores ausentes com defaults seguros;
4. nunca use “apagar todos os dados” como correção automática.

## 8. Blazor Server: interface web em C#

### 8.1 Componentes Razor

Uma página `.razor` mistura HTML e C# de forma controlada:

```razor
@page "/clientes"
@attribute [Authorize]
@inject ManagementService Management

<button @onclick="NovoCliente">Novo cliente</button>

@code {
    private void NovoCliente() { ... }
}
```

| Trecho | Significado |
| --- | --- |
| `@page` | Define a URL. |
| `[Authorize]` | Exige autenticação. |
| `@inject` | Recebe uma dependência pronta. |
| `@onclick` | Conecta evento do navegador a método C#. |
| `@code` | Estado e comportamento do componente. |

### 8.2 Estado e renderização

Em Blazor Server, o código C# da página executa no servidor. Quando a pessoa digita, clica ou muda uma seleção, um evento via SignalR chega ao servidor, o estado é atualizado e o Blazor envia apenas as diferenças necessárias de HTML de volta ao navegador.

Consequências práticas:

- Variáveis como `Editando`, `Feedback` e `Itens` representam o estado da tela.
- Um erro não tratado em evento pode interromper o circuito daquela página; por isso eventos precisam receber o tipo correto, como `ChangeEventArgs`.
- O navegador precisa estar conectado ao servidor para a interatividade funcionar.
- A tela possui componente de reconexão (`ReconnectModal`) para comunicar quedas de conexão.

### 8.3 Binding e eventos

`@bind` sincroniza campo e propriedade. Exemplo:

```razor
<InputText @bind-Value="Editando.Nome" />
```

Para máscara e comportamento imediato, o projeto usa campos HTML com `@oninput` e JavaScript pequeno em `wwwroot/js/formata-cadastro.js`. A divisão é intencional:

- JavaScript: garante resposta imediata no navegador, como limitar telefone a 11 dígitos.
- C#: mantém o valor final correto e valida antes de persistir.

Nunca confie somente na máscara JavaScript, pois ela pode não executar ou ser alterada pelo cliente.

### 8.4 Rotas e autorização

`Routes.razor` contém `AuthorizeRouteView`. Se uma página protegida é acessada sem identidade válida, `RedirectToLogin` leva a pessoa à entrada.

O `MainLayout.razor` desenha menu, cabeçalho móvel e a região onde cada página aparece (`@Body`). Esse é um exemplo claro da diferença entre layout compartilhado e página específica.

## 9. ASP.NET Core: inicialização, DI e pipeline

### 9.1 `Program.cs`

`NutrieGestao.Web/Program.cs` é a composição da aplicação. Ele não é regra de pedido; ele diz ao .NET quais peças usar e em qual ordem atender requisições.

É útil lê-lo em três blocos:

1. **Configuração e serviços:** `builder.Services.Add...`.
2. **Pipeline HTTP:** `app.Use...`.
3. **Endpoints e componentes:** `app.Map...`.

### 9.2 Dependency Injection (DI)

DI é o mecanismo que constrói serviços e os entrega para quem precisa deles. Em vez de uma página criar `new DataService()` por conta própria, o projeto registra:

```csharp
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<ManagementService>();
```

E a página pede:

```razor
@inject ManagementService Management
```

Vantagens:

- menos acoplamento;
- configuração centralizada;
- dependências explícitas;
- troca mais simples em testes ou futura evolução.

`Singleton` significa uma instância por processo. Aqui faz sentido porque os serviços são leves e trabalham com arquivos compartilhados. Em sistemas web maiores, o tempo de vida (`Singleton`, `Scoped`, `Transient`) precisa ser escolhido com atenção.

### 9.3 Middleware

Middleware é uma etapa que envolve cada requisição. O pipeline deste projeto inclui, em linhas gerais:

```text
log e captura de exceção
→ tratamento de erro
→ HTTPS
→ rate limit
→ autenticação
→ autorização
→ antiforgery
→ arquivos estáticos/endpoints/componentes
```

Ordem é comportamento. Por exemplo, não adianta autorizar antes de autenticar: primeiro o sistema precisa identificar quem fez a requisição.

## 10. Autenticação, autorização e criptografia

### 10.1 Não confunda os termos

| Termo | Pergunta |
| --- | --- |
| Autenticação | “Quem é esta pessoa?” |
| Autorização | “Essa pessoa pode fazer esta ação?” |
| Criptografia/proteção | “Como impedimos leitura ou alteração indevida?” |

### 10.2 Login real do projeto

O endpoint `POST /auth/login` segue este fluxo:

```text
formulário HTML
→ valida token antiforgery
→ aplica rate limit por IP
→ normaliza usuário/e-mail
→ localiza usuário ativo
→ verifica hash da senha
→ cria claims
→ emite cookie
→ redireciona
```

O nome de usuário é o identificador preferencial. O e-mail permanece como alternativa de compatibilidade para contas antigas.

### 10.3 Hash de senha não é criptografia reversível

O sistema não “descriptografa senha”. Ao criar ou trocar senha:

1. gera um salt aleatório;
2. aplica PBKDF2 com SHA-256 e 600.000 iterações;
3. guarda hash, salt e número de iterações;
4. na entrada, recalcula o hash com a senha informada e compara em tempo constante.

Isso está em `PasswordHasher.cs` e usa APIs oficiais do .NET:

```csharp
Rfc2898DeriveBytes.Pbkdf2(...)
CryptographicOperations.FixedTimeEquals(...)
```

Nunca crie senha manualmente em `usuarios.json`, nem tente reverter hash. Use fluxo de redefinição de senha ou gere corretamente via serviço controlado.

### 10.4 Claims, cookie e sessão

Após login, o código cria um `ClaimsPrincipal` com ID, nome de usuário, nome, e-mail, perfil e carimbo de segurança. Essas claims seguem no cookie de autenticação protegido pelo ASP.NET Core.

Configurações relevantes:

- cookie `HttpOnly`: JavaScript do navegador não o lê;
- `SameSite=Strict`: reduz ataques de requisições cruzadas;
- `Secure`: exige HTTPS;
- expiração de 8 horas;
- renovação deslizante enquanto há uso;
- sem persistência de “lembrar de mim”.

### 10.5 Carimbo de segurança e invalidação

O `CarimboSeguranca` é armazenado no usuário e também no cookie. Ao validar a sessão, o serviço compara os dois valores. Se a senha, perfil, dados relevantes ou ativação da conta mudam, o carimbo é renovado. O cookie anterior deixa de valer.

Esse padrão responde a uma situação importante: “como deslogar uma pessoa em outro computador depois de redefinir a senha?”

### 10.6 Antiforgery, rate limit e Data Protection

| Proteção | Problema que evita |
| --- | --- |
| Antiforgery | Site externo forjar envio de formulário de login/acesso inicial. |
| Rate limit | Muitas tentativas de senha a partir do mesmo IP. |
| Data Protection | Proteção de cookies, tokens e chaves de framework. |
| DPAPI no Windows | Protege as chaves persistidas usando a proteção do próprio Windows. |

O rate limit atual permite cinco tentativas em 15 minutos por IP para endpoints de login e primeiro acesso. Isso não transforma o sistema em segurança corporativa completa, mas é uma defesa adequada e explícita para este MVP.

## 11. Configuração e ambientes

Configuração é lida de `appsettings.json`, `appsettings.Development.json`, variáveis de ambiente e outras fontes que o ASP.NET Core combina.

As chaves usadas pelo projeto incluem:

```text
NutrieGestao:Paths:RootPath
NutrieGestao:Paths:Data
NutrieGestao:Paths:Logs
NutrieGestao:Paths:Backups
NutrieGestao:Diagnostics:File:...
NutrieGestao:Security:BootstrapCode
```

O código de ativação inicial é segredo. Em produção, ele deve vir de uma fonte segura, não de um repositório público. Também não se deve expor arquivos de dados, logs ou a pasta `Data/Keys` pela web.

Uma diferença clássica entre desenvolvimento e produção é o caminho dos dados. Sempre confirme nos logs de inicialização os valores de `DataPath`, `LogsPath` e `BackupsPath` antes de diagnosticar “dados que sumiram”. Muitas vezes a aplicação está apenas lendo outra pasta.

## 12. Observabilidade: logs e diagnóstico

`FileLogger` grava eventos em JSON Lines (`.jsonl`), ou seja, um objeto JSON por linha. Isso facilita pesquisa por ferramenta e evita depender apenas do console.

O projeto registra, entre outros:

- início e fim da aplicação;
- salvamento/leitura de JSON;
- exceções inesperadas;
- falhas de antiforgery;
- requisições HTTP relevantes;
- eventos de segurança sem incluir senha/token.

Ao depurar, use a seguinte estratégia:

1. reproduza a falha uma vez;
2. anote horário aproximado e módulo;
3. procure o arquivo de log do dia;
4. correlacione com `RequestId` quando houver;
5. corrija a causa, não apenas a mensagem visual.

## 13. Padrões de projeto presentes (sem exagero)

O projeto usa padrões simples, não uma “Clean Architecture” pesada:

| Padrão | Onde aparece | Benefício |
| --- | --- | --- |
| Layering | Web → Core → dados | Responsabilidades claras. |
| Repository | `IRepository<T>` / `JsonRepository<T>` | Isola persistência. |
| Service | `ManagementService`, `AuthenticationService` | Centraliza casos de uso. |
| Dependency Injection | `Program.cs` e `@inject` | Evita criação espalhada de dependências. |
| MVVM | Projeto WPF | Separa estado da interface desktop. |
| DTO/Resumo | `UsuarioResumo`, `DashboardSummary` | Evita expor dados desnecessários na tela. |

O objetivo não é aplicar todo padrão conhecido. É introduzir abstração apenas quando ela facilita manutenção ou uma troca futura real, como JSON para SQLite.

## 14. Exercícios práticos progressivos

Faça cada exercício em uma branch ou cópia dos dados de teste. Sempre compile ao final.

### Nível 1 — C# e interface

1. Adicione um contador de caracteres para observações do cliente.
2. Mostre uma mensagem quando uma lista estiver vazia, seguindo o padrão `empty-state`.
3. Crie uma função que transforma um `StatusPedido` no texto amigável e use-a em outra página.
4. Inclua um novo campo visual que não seja salvo, por exemplo um filtro por cidade.

### Nível 2 — Model, validação e persistência

1. Adicione `CEP` a `Cliente`.
2. Adicione limite de tamanho e validação em `ValidacaoCadastro`.
3. Preserve o campo ao copiar cliente para edição.
4. Exiba-o na tabela apenas se não prejudicar o mobile.
5. Crie cliente, reinicie a aplicação e confirme que o valor continua no JSON.

### Nível 3 — Regra de negócio

1. Crie um campo `DataInativacao` para marmitas inativas.
2. Faça o serviço preencher a data só quando uma marmita passa de ativa para inativa.
3. Garanta que reativar limpe a data.
4. Mostre a informação na interface.

### Nível 4 — Segurança

1. Localize onde a senha é validada.
2. Explique por que a confirmação de senha não é armazenada.
3. Trace o caminho de uma troca de senha até a invalidação de sessão.
4. Verifique que nenhum log registra a senha fornecida.

### Nível 5 — Arquitetura

1. Liste quais métodos de `DataService` seriam necessários em uma implementação SQLite.
2. Desenhe uma interface para uma fonte de dados sem reescrever páginas.
3. Discuta quais operações precisariam de transação se JSON fosse substituído por banco.

## 15. Roteiro de estudo em quatro semanas

### Semana 1 — Linguagem e projeto

- C# básico: classes, propriedades, métodos, `if`, coleções.
- Leia Models e Enums.
- Aprenda `decimal`, `Guid`, `DateTime`, enum e record.
- Faça os exercícios de nível 1.

### Semana 2 — Regras e dados

- LINQ: `Where`, `Select`, `Any`, `Sum`, `FirstOrDefault`.
- Leia `ValidacaoCadastro`, `ManagementService`, `DataService` e `JsonRepository`.
- Faça exercícios de níveis 2 e 3.
- Estude JSON e compatibilidade de dados.

### Semana 3 — Web e interface

- HTML semântico, formulários e CSS Grid/Flexbox.
- Leia `MainLayout`, `Routes`, Clientes, Marmitas e Pedidos.
- Entenda `@bind`, `@onclick`, `@inject` e `@code`.
- Teste em celular e notebook com DevTools.

### Semana 4 — Backend e segurança

- Leia `Program.cs` por blocos.
- Estude DI, middleware, endpoint, cookie, claim e autorização.
- Leia `AuthenticationService` e `PasswordHasher`.
- Faça exercício de nível 4 e desenhe a arquitetura do nível 5.

## 16. Como analisar uma funcionalidade existente

Use este método toda vez que quiser entender algo novo:

1. Comece pela tela: por exemplo, `Pedidos.razor`.
2. Localize o botão/evento: `@onclick="Salvar"`.
3. Leia o método `Salvar` e veja qual serviço chama.
4. Siga para `ManagementService.SavePedido`.
5. Veja qual validação é executada.
6. Siga para `DataService` e `JsonRepository`.
7. Verifique o model para entender os campos e cálculos.
8. Se existir regra de segurança, veja `Program.cs` e `AuthenticationService`.

Esse caminho “da borda para o centro” é mais didático do que ler arquivos em ordem alfabética.

## 17. Boas práticas que este projeto deve preservar

- Dinheiro sempre com `decimal`.
- Regra crítica validada no Core antes de salvar.
- Tela não acessa arquivo JSON diretamente.
- Pedido mantém histórico de preço/nome/ingredientes do item vendido.
- Mensagem ao usuário é amigável; detalhe técnico vai ao log.
- Senha nunca aparece em log, tela de listagem ou JSON em texto puro.
- Mudança de formato de dados começa com backup e compatibilidade.
- Toda mudança relevante termina com `dotnet build` sem erros introduzidos.
- Interfaces devem ser testadas em desktop, notebook e celular.

## 18. Referência de arquivos para leitura

| Objetivo de estudo | Arquivo |
| --- | --- |
| Model e cálculo financeiro | `NutrieGestao.Core/Models/Pedido.cs` |
| Validação | `NutrieGestao.Core/Services/ValidacaoCadastro.cs` |
| Casos de uso | `NutrieGestao.Core/Services/ManagementService.cs` |
| Persistência JSON | `NutrieGestao.Core/Repositories/JsonRepository.cs` |
| Catálogo de arquivos de dados | `NutrieGestao.Core/Services/DataService.cs` |
| Senhas | `NutrieGestao.Core/Services/PasswordHasher.cs` |
| Login, perfis e sessão | `NutrieGestao.Core/Services/AuthenticationService.cs` |
| Configuração do servidor | `NutrieGestao.Web/Program.cs` |
| Rotas protegidas | `NutrieGestao.Web/Components/Routes.razor` |
| Navegação | `NutrieGestao.Web/Components/Layout/MainLayout.razor` |
| Página complexa e completa | `NutrieGestao.Web/Components/Pages/Pedidos.razor` |
| Responsividade | `NutrieGestao.Web/wwwroot/css/components.css` e `mobile.css` |

---

O resultado esperado deste estudo não é apenas saber onde ficam os arquivos. É conseguir olhar um requisito — por exemplo “adicionar CEP ao cliente” ou “criar desconto por cupom” — e decidir com segurança: qual model muda, qual regra valida, qual service coordena, qual página apresenta e como o dado será mantido no histórico.

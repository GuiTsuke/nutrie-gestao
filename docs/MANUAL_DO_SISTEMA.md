# Manual do Nutriê Gestão

Este documento é a referência funcional e técnica do sistema. Para estudar C#, .NET, Blazor, arquitetura e segurança a partir deste projeto, comece pelo [Guia técnico de estudos](GUIA_TECNICO_DE_ESTUDOS.md).

> Público: pessoas que operam o sistema e pessoas que querem entender ou evoluir o código. Não é necessário saber programação para as partes 1 a 3.

## 1. Visão geral

O Nutriê Gestão é um sistema para administrar uma pequena operação de marmitas congeladas. A versão utilizada no navegador é uma aplicação **ASP.NET Core + Blazor Server**, escrita em **C#** e executada sobre **.NET 10**.

Ela foi pensada para funcionar com poucos recursos e sem banco de dados: os dados são persistidos em arquivos JSON. O projeto também contém uma aplicação WPF desktop mais antiga/alternativa, que reutiliza a mesma camada de regras; o produto que recebe as evoluções de interface atuais é `NutrieGestao.Web`.

### O que cada módulo resolve

| Módulo | Uso principal |
| --- | --- |
| Dashboard | Visão resumida de vendas, clientes e pedidos. |
| Pedidos | Montagem, desconto, status, pagamento e detalhamento de pedidos. |
| Clientes | Cadastro e consulta de contatos/endereço. |
| Marmitas | Catálogo, preço, tamanho, ingredientes e ativação de produtos. |
| Rentabilidade | Resultado por marmita, com base no vendido e no custo cadastrado. |
| Consultas | Filtros e consolidações de pedidos/ingredientes. |
| Minha conta | Troca da própria senha. |
| Usuários | Administração de contas, perfis, ativação e redefinição de senha. |
| Configurações | Dados da empresa exibidos pelo sistema. |

## 2. Manual de operação

### 2.1 Entrada, saída e sessão

1. Acesse a tela **Entrar**.
2. Informe o nome de usuário e a senha. O e-mail também é aceito para contas antigas, mas o nome de usuário é o identificador principal.
3. Ao entrar, o sistema abre o Dashboard.
4. Use **Sair** no rodapé do menu para encerrar a sessão no navegador.

A sessão dura até 8 horas e tem renovação deslizante: enquanto a pessoa continua usando o sistema, o prazo é renovado periodicamente. Fechar apenas uma aba não necessariamente encerra a sessão; usar **Sair** é a forma correta de terminar o acesso.

No primeiro uso, a tela **Primeiro acesso** cria o administrador inicial. Ela exige o código de ativação configurado pelo responsável técnico. Depois que existe o arquivo de usuários, essa tela não permite criar outro administrador inicial.

### 2.2 Perfis de usuário

Há dois perfis:

| Perfil | Pode fazer |
| --- | --- |
| Administrador | Usar os módulos e gerenciar usuários: criar, editar, ativar/desativar e redefinir senhas. |
| Operador | Usar os módulos operacionais, mas não administrar contas. |

Proteções importantes:

- Não é permitido deixar o sistema sem ao menos um administrador ativo.
- Uma pessoa não pode desativar a própria conta.
- Um administrador não pode alterar o próprio perfil por engano.
- Alterar dados de uma conta, redefinir sua senha ou desativá-la invalida sua sessão por segurança.

### 2.3 Ordem recomendada de cadastro

Para começar sem retrabalho, siga esta sequência:

1. Complete **Configurações** com os dados da empresa.
2. Cadastre as **Marmitas**, com preço e ingredientes.
3. Cadastre os **Clientes**.
4. Crie os **Pedidos**.
5. Consulte o Dashboard, Rentabilidade e Consultas.

### 2.4 Clientes

Em **Clientes**, use **Novo cliente** para abrir o formulário. Somente o nome é obrigatório, mas telefone e WhatsApp, quando preenchidos, precisam ter DDD válido.

- A máscara limita telefone/WhatsApp ao formato brasileiro `(00) 00000-0000`.
- **Cancelar** e o botão `×` fecham o formulário e descartam os campos que ainda não foram salvos.
- Ao editar, o sistema trabalha sobre uma cópia; portanto, cancelar não altera o cadastro original.
- A tabela mostra também quem cadastrou o cliente quando essa informação existe.
- Um cliente com pedidos no histórico não pode ser excluído, para evitar pedidos sem referência. Ele pode continuar no sistema mesmo que não seja mais usado em novos pedidos.

### 2.5 Marmitas e ingredientes

Uma marmita tem nome, descrição, categoria, tamanho, preço, status ativo/inativo e ingredientes.

- O preço usa `decimal`, nunca `float` ou `double`, para não gerar erro de arredondamento financeiro.
- O preço não pode ser negativo nem ultrapassar o limite de segurança definido pelo sistema.
- Tamanho deve ser um dos tamanhos previstos pela aplicação (350g ou 500g).
- Ingredientes registram nome, quantidade por marmita, unidade e custo da porção.
- Quando uma marmita já aparece em pedidos, o sistema prioriza inativá-la em vez de apagá-la. Isso preserva o histórico.

No notebook, o editor de ingredientes reorganiza os campos em duas linhas quando o painel fica estreito. No celular, a estrutura também se adapta para evitar sobreposição.

### 2.6 Pedidos

Um pedido exige um cliente e pelo menos uma marmita.

Fluxo:

1. Clique em **Novo pedido**.
2. Escolha o cliente e a data de entrega.
3. Busque marmitas e use **Adicionar**.
4. Ajuste a quantidade pelos botões `+` e `−` ou remova itens.
5. Escolha status e forma de pagamento.
6. Se necessário, aplique desconto por valor em reais ou por porcentagem.
7. Salve o pedido.

Regras de cálculo:

```text
subtotal do item = quantidade × valor unitário guardado no pedido
valor bruto      = soma dos subtotais
desconto         = valor informado ou porcentagem sobre o valor bruto
valor total      = valor bruto − desconto
valor pendente   = valor total − valor pago
```

O preço e os ingredientes são copiados para o item no instante da venda. Por isso, alterar a marmita depois não modifica os pedidos antigos.

#### Descontos

- **Valor (R$):** não pode superar o valor das marmitas.
- **Porcentagem:** é limitada entre 0 e 100; a tela corta imediatamente qualquer valor acima de 100.
- Um pedido novo registra automaticamente como pago o total já descontado. Ao editar um pedido antigo, pagamentos parciais existentes são preservados, exceto se excederem o novo total.

#### Status

| Valor interno | Texto exibido |
| --- | --- |
| `Novo` | Novo |
| `EmPreparacao` | Em preparação |
| `Pronto` | Pronto |
| `Entregue` | Entregue |
| `Cancelado` | Cancelado |

Pedidos cancelados não entram no faturamento. O número apresentado (`000001`, por exemplo) é diferente do identificador interno e é gerado sequencialmente.

### 2.7 Dashboard, consultas e rentabilidade

O Dashboard reúne indicadores calculados a partir dos pedidos:

- faturamento efetivamente recebido;
- faturamento do mês;
- pedidos em andamento e concluídos;
- clientes cadastrados;
- pedidos recentes e distribuição por status.

**Rentabilidade** calcula o resultado por marmita: valor arrecadado menos custo das porções dos ingredientes. **Consultas** permite filtrar e consolidar pedidos e ingredientes necessários. Ambos usam cálculos da camada Core, e não fórmulas duplicadas na interface.

### 2.8 Interface, responsividade e acessibilidade

A navegação lateral é o layout padrão. Em telas menores, ela é substituída por um menu móvel. Tabelas grandes de Clientes e Pedidos rolam horizontalmente **dentro do card**, em vez de ultrapassarem a borda da tela.

Os arquivos visuais ficam em `NutrieGestao.Web/wwwroot`:

- `css/variables.css`: cores, sombras, fontes e espaçamentos reutilizáveis;
- `css/base.css`: normalização e estilos gerais;
- `css/components.css`: cards, tabelas, formulários, pedidos e ingredientes;
- `css/mobile.css`: ajustes para telas pequenas;
- `css/auth.css`: telas de entrada e primeiro acesso;
- `icons/`: ícones SVG;
- `js/formata-cadastro.js`: máscaras de telefone e limite imediato de desconto percentual.

## 3. Dados e backup

### 3.1 Onde os arquivos ficam

`StorageOptions` resolve a pasta raiz. Sem configuração específica, o padrão é:

```text
%LocalAppData%\NutrieGestao\
├── Data\
├── Logs\
└── Backups\
```

No ambiente de desenvolvimento deste projeto, `appsettings*.json` aponta a raiz para `E:\NutrieGestao\Testes\`. Em produção, confirme a configuração efetivamente publicada antes de mexer em qualquer arquivo.

Os arquivos principais são:

| Arquivo | Conteúdo |
| --- | --- |
| `empresa.json` | Um cadastro de empresa, armazenado dentro de uma lista JSON. |
| `clientes.json` | Lista de clientes. |
| `marmitas.json` | Lista de marmitas e ingredientes. |
| `pedidos.json` | Lista de pedidos e seus itens congelados no momento da venda. |
| `usuarios.json` | Lista de contas; contém hashes de senha, nunca senha em texto. |
| `Data/Keys/` | Chaves de proteção de dados usadas pelo ASP.NET Core. |

### 3.2 Como o salvamento é protegido

`JsonRepository<T>` centraliza leitura e escrita. Ao salvar, ele:

1. serializa os registros formatados;
2. grava primeiro em `arquivo.json.tmp`;
3. substitui o arquivo definitivo;
4. registra a operação no log.

Se o arquivo não existe ou está vazio, a leitura devolve uma lista vazia. Se o JSON estiver inválido, o erro vai para o log e a aplicação evita encerrar abruptamente.

### 3.3 Backup seguro

Para fazer backup manual:

1. Peça para todos saírem do sistema ou pare a aplicação.
2. Copie a pasta `Data` inteira para uma pasta com data, por exemplo `Backup-2026-09-07`.
3. Guarde a cópia em local diferente do computador/servidor principal.
4. Inclua `Data/Keys`; sem essas chaves, cookies e tokens existentes deixam de ser decifráveis.

Não use o JSON como tela de edição comum. Em especial:

- mantenha `usuarios.json` como uma **lista** (`[ ... ]`), mesmo se houver apenas uma pessoa;
- não tente calcular ou trocar `SenhaHash`, `SenhaSalt` ou `CarimboSeguranca` manualmente;
- antes de uma alteração manual necessária, faça backup e valide o JSON;
- não apague todos os arquivos para “resolver” falhas.

## 4. Mapa da arquitetura

```text
Navegador
   │ HTTPS + SignalR (interatividade Blazor Server)
   ▼
NutrieGestao.Web
   ├── Pages / Layout / CSS / JavaScript
   ├── autenticação por cookie e endpoints de login
   └── injeta serviços
           │
           ▼
NutrieGestao.Core
   ├── Models: entidades e enums
   ├── Services: regras, cálculos, validação, autenticação
   └── Repositories: JSON
           │
           ▼
Arquivos JSON + Logs + chaves de proteção
```

### 4.1 Projetos da solução

| Projeto | Papel |
| --- | --- |
| `NutrieGestao.Core` | Biblioteca sem interface com modelos, regras e persistência. É o núcleo reutilizável. |
| `NutrieGestao.Web` | Aplicação ASP.NET Core/Blazor Server usada pelo navegador. |
| `NutrieGestao` | Aplicação WPF Windows que também referencia o Core. |

Essa separação permite trocar a interface ou, futuramente, trocar JSON por SQLite sem reescrever regras de pedidos, validações e cálculos.

### 4.2 Responsabilidade de cada pasta

```text
NutrieGestao.Core/
  Models/          Entidades: Cliente, Marmita, Pedido, Usuario etc.
  Services/        Casos de uso, regras, validações, cálculos e logging.
  Repositories/    Contrato IRepository e implementação JsonRepository.

NutrieGestao.Web/
  Components/Pages/    Uma página Razor por módulo/tela.
  Components/Layout/   Navegação, casca visual e reconexão.
  Components/Shared/   Pequenos componentes reutilizáveis.
  Infrastructure/      Provider de log e diagnóstico antiforgery.
  wwwroot/             CSS, JavaScript e imagens estáticas.
  Program.cs           Composição da aplicação e pipeline HTTP.

NutrieGestao/
  ViewModels/ e Views WPF/   Interface desktop alternativa.
```

### 4.3 Modelos principais

| Modelo | Ideia central |
| --- | --- |
| `Cliente` | Dados de contato/endereço, data e autoria do cadastro. |
| `Marmita` | Produto, preço `decimal`, ingredientes e situação ativa. |
| `IngredienteMarmita` | Ingrediente, quantidade, unidade e custo da porção. |
| `Pedido` | Cliente, datas, status, pagamento, desconto, itens e autoria. |
| `ItemPedido` | Fotografia da marmita no momento da venda: nome, quantidade, preço e ingredientes. |
| `Usuario` | Identidade, perfil, senha protegida, status e carimbo de segurança. |

Todos usam `Guid` como identificador interno. O pedido possui ainda um número legível, independente do `Guid`.

## 5. Caminho de uma ação no código

Exemplo: salvar cliente pela página web.

```text
Clientes.razor
  → chama ManagementService.SaveCliente(...)
  → ValidacaoCadastro.ValidarCliente(...)
  → formata telefone/WhatsApp
  → DataService.LoadClientes / SaveClientes
  → JsonRepository<Cliente>.Save(...)
  → clientes.json
```

As páginas não leem nem escrevem JSON diretamente. Isso evita que uma tela passe a ter regra diferente de outra e é a base para uma futura migração a banco de dados.

### Serviços importantes

| Serviço | Função |
| --- | --- |
| `DataService` | Ponto central de leitura/gravação de entidades. |
| `ManagementService` | Salva/exclui empresa, clientes, marmitas e pedidos; preserva autoria e histórico. |
| `ValidacaoCadastro` | Validação central de telefone, e-mail, tamanhos, valores e limites. |
| `FormatacaoCadastro` | Formatação/normalização de telefone e e-mail. |
| `AuthenticationService` | Contas, login, perfis, senha e validade de sessão. |
| `PasswordHasher` | PBKDF2 com SHA-256, salt aleatório e comparação em tempo constante. |
| `DashboardCalculator` | Indicadores do Dashboard. |
| `PedidoAnalysisCalculator` | Ingredientes necessários e rentabilidade. |
| `FileLogger` | Logs estruturados em arquivo e rotação por tamanho/dias. |

### Validação: duas camadas

A boa experiência visual não substitui a regra de negócio:

1. **Interface:** atributos HTML, máscaras, `min`, `max`, `maxlength` e mensagens próximas aos campos evitam erros cedo.
2. **Core:** `ValidacaoCadastro` é a autoridade antes da persistência. Mesmo que alguém altere o navegador, a gravação inválida é recusada.

Limites atuais incluem preço/custo até R$ 1.000.000, quantidade de ingrediente até 100.000 e até 1.000 unidades do mesmo item por pedido. Esses limites previnem erros de cálculo e entradas acidentais gigantes.

## 6. Autenticação e segurança do backend

### 6.1 Fluxo de login

```text
POST /auth/login
  → valida token antiforgery
  → aplica limite de tentativas por IP
  → AuthenticationService.Autenticar
  → PasswordHasher.Verificar
  → cria ClaimsPrincipal
  → grava cookie de autenticação
  → redireciona para a página solicitada ou Dashboard
```

As páginas de autenticação (`/entrar` e `/primeiro-acesso`) são renderizadas no servidor sem interatividade Blazor. As páginas internas usam `InteractiveServer`, que mantém a interatividade por SignalR entre navegador e servidor.

### 6.2 Senhas

Senhas nunca são salvas em texto puro. O `PasswordHasher` usa:

- PBKDF2 (`Rfc2898DeriveBytes.Pbkdf2`);
- SHA-256;
- salt aleatório de 16 bytes por senha;
- 600.000 iterações;
- hash de 32 bytes;
- comparação em tempo constante (`CryptographicOperations.FixedTimeEquals`).

Regra atual de senha: de 9 a 128 caracteres, contendo ao menos uma letra e um número. A regra fica em `AuthenticationService.ValidarSenha`.

### 6.3 Cookie e sessão

O cookie de autenticação é configurado com:

| Propriedade | Efeito |
| --- | --- |
| `HttpOnly` | JavaScript não lê o cookie. |
| `SameSite=Strict` | Reduz envio indevido em navegação de outros sites. |
| `SecurePolicy=Always` | Cookie é usado apenas por HTTPS. |
| 8 horas | Duração da sessão. |
| `SlidingExpiration=true` | Uso contínuo renova o prazo. |
| `IsPersistent=false` | Não cria login persistente além da sessão do navegador. |

O nome do cookie recebe um sufixo derivado da pasta de dados. Isso evita colisão entre instalações locais que compartilham o mesmo host.

### 6.4 Carimbo de segurança

Cada usuário possui `CarimboSeguranca`, um valor aleatório armazenado no JSON e também colocado nas claims do cookie. Em toda validação de principal, o sistema compara o valor do cookie com o valor atual do usuário.

O carimbo muda ao alterar dados sensíveis, senha ou ativação. Consequência: cookies antigos deixam de ser aceitos e a pessoa precisa entrar novamente. É por isso que redefinir senha ou desativar alguém encerra sua sessão.

### 6.5 Proteções de requisição

- **Antiforgery:** os `POST`s de login e primeiro acesso exigem token válido. Requisições sem token são rejeitadas e registradas.
- **Rate limit:** no máximo 5 tentativas de login/primeiro acesso por IP a cada 15 minutos; a sexta recebe `429` e é redirecionada com mensagem amigável.
- **Redirecionamento local:** a URL de retorno é aceita apenas se começa por `/` e não começa com `//`, evitando redirecionamento aberto.
- **Data Protection:** chaves persistidas em `Data/Keys`; no Windows são protegidas com DPAPI.
- **Logs:** não devem registrar senha, hash, salt, token ou conteúdo de formulários.

### 6.6 Configurações sensíveis

`NutrieGestao:Security:BootstrapCode` é o código do primeiro acesso. Ele deve ser definido fora do código-fonte público — por variável de ambiente, configuração segura do servidor ou secrets locais de desenvolvimento. Nunca publique esse valor em documentação, imagem ou repositório.

## 7. Inicialização e pipeline HTTP

`NutrieGestao.Web/Program.cs` é o ponto de entrada. Ele, em ordem, faz:

1. Cria a aplicação e lê configuração.
2. Resolve diretórios de dados, logs e backups.
3. Registra logger, persistência, gestão, autenticação e estado de autenticação.
4. Configura cookie, antiforgery, Data Protection e rate limit.
5. Monta o pipeline HTTP: tratamento de erro, HTTPS, rate limit, autenticação, autorização, antiforgery e arquivos estáticos.
6. Mapeia endpoints de login, primeiro acesso e saída.
7. Mapeia componentes Razor/Blazor Server.

A ordem do pipeline importa: autenticação deve ocorrer antes de autorização, e antiforgery deve ocorrer antes dos endpoints que dependem dela.

## 8. Como estudar o código na prática

Uma sequência eficiente para se familiarizar:

1. Leia este manual e navegue pela aplicação aberta.
2. Abra `NutrieGestao.Web/Components/Layout/MainLayout.razor` para entender a navegação.
3. Leia `Clientes.razor`, depois `Marmitas.razor` e `Pedidos.razor`.
4. Siga as chamadas para `ManagementService`.
5. Leia `ValidacaoCadastro`, `DataService` e `JsonRepository<T>`.
6. Estude `Pedido.cs` e os calculators para ver as fórmulas.
7. Por último, leia `Program.cs` e `AuthenticationService.cs` para a infraestrutura e segurança.

Ao ler uma página Razor, procure por:

- `@page`: rota da tela;
- `[Authorize]`: exige usuário autenticado;
- `@inject`: serviço utilizado pela tela;
- `@bind`: ligação entre campo visual e estado C#;
- `@onclick`: ação de botão;
- bloco `@code`: estado e métodos da página.

## 9. Ferramentas e comandos

Pré-requisito: SDK do .NET 10. No terminal aberto em `E:\NutrieGestao`:

```powershell
# Compilar todos os projetos
dotnet build .\NutrieGestao.slnx --nologo

# Executar a aplicação web em desenvolvimento
dotnet run --project .\NutrieGestao.Web\NutrieGestao.Web.csproj --launch-profile http

# Publicar uma versão de produção em uma pasta de saída
dotnet publish .\NutrieGestao.Web\NutrieGestao.Web.csproj -c Release -o .\publish
```

No perfil HTTP de desenvolvimento, a aplicação usa `http://localhost:5160`.

Para alterações visuais, normalmente basta modificar Razor/CSS/JS, compilar e recarregar com `Ctrl + F5`. Para alterações de regra, sempre teste o fluxo completo: criar, editar, cancelar, salvar, recarregar a página e confirmar o JSON.

### Diagnóstico

| Sintoma | Primeiro lugar para olhar |
| --- | --- |
| Tela não atualiza/interações não funcionam | Console/logs e mensagem de reconexão do Blazor. Recarregue a página. |
| Login falha | `usuarios.json`, logs, status ativo, nome de usuário e configuração de chaves. |
| Dados não aparecem | Pasta `Data` configurada no ambiente e validade do JSON. |
| Erro ao salvar | Mensagem de validação na tela; depois log em `Logs`. |
| Layout quebrado | `components.css`, `mobile.css` e largura da tela. |

Os logs são arquivos `.jsonl`, uma entrada JSON por linha. Eles são mais seguros para diagnóstico do que mostrar exceções técnicas para a usuária.

## 10. Como evoluir sem quebrar o sistema

### Para adicionar um campo a uma entidade

Exemplo: adicionar `CEP` a `Cliente`.

1. Inclua a propriedade em `Models/Cliente.cs`, com valor padrão seguro.
2. Inclua campo e limite na página `Clientes.razor`.
3. Inclua regra em `ValidacaoCadastro.ValidarCliente`.
4. Preserve o campo no método de cópia usado pela edição.
5. Se o campo deve aparecer na tabela, adicione coluna com cuidado para o mobile.
6. Compile e teste com um JSON antigo sem o campo. Propriedades novas precisam aceitar o valor padrão para manter compatibilidade.

### Para adicionar uma página

1. Crie `Components/Pages/NovaPagina.razor` com `@page` e `[Authorize]` se ela for interna.
2. Use `@inject` apenas para os serviços necessários.
3. Adicione link em `MainLayout.razor` e no menu móvel.
4. Mantenha regra no Core, não no componente Razor.
5. Reaproveite classes de CSS existentes antes de criar novas.

### Para trocar JSON por SQLite no futuro

O ponto de troca é a persistência. `DataService` já isola as telas do formato de arquivo; a evolução natural é criar repositórios SQLite com as mesmas operações e adaptar `DataService`, preservando `ManagementService`, validações e modelos o máximo possível.

### Checklist antes de publicar

- Compile sem erros: `dotnet build`.
- Teste a funcionalidade adicionada e as telas relacionadas.
- Verifique o mobile e pelo menos uma largura de notebook.
- Não publique segredos em `appsettings.json` ou no repositório.
- Faça backup da pasta de dados antes de mudar formato de JSON.
- Verifique logs depois da publicação.
- Em produção, use HTTPS e mantenha permissões de pasta restritas ao processo da aplicação.

## 11. Glossário rápido

| Termo | Significado neste sistema |
| --- | --- |
| Blazor Server | Interface Razor renderizada no servidor, com interatividade enviada ao navegador por SignalR. |
| Razor | Arquivo `.razor` que mistura marcação HTML e C#. |
| Model | Classe que representa dados, como `Pedido`. |
| Service | Classe que concentra uma regra ou caso de uso. |
| Repository | Classe responsável por guardar/carregar dados. |
| DI / injeção | Forma de receber serviços prontos com `@inject` ou `AddSingleton`. |
| `decimal` | Tipo numérico apropriado para moeda. |
| Hash | Resultado não reversível usado para conferir uma senha sem guardá-la. |
| Salt | Valor aleatório combinado com a senha antes do hash. |
| Cookie | Pequeno dado de sessão guardado pelo navegador. |
| Claim | Informação de identidade no cookie, como ID, nome e perfil. |
| Antiforgery | Proteção contra envio de formulário forjado por outro site. |
| Rate limit | Limite de requisições em um intervalo de tempo. |
| JSON | Formato de texto usado para os dados persistidos. |

## 12. Arquivos de referência

| Assunto | Arquivo principal |
| --- | --- |
| Inicialização e segurança HTTP | `NutrieGestao.Web/Program.cs` |
| Layout e menu | `NutrieGestao.Web/Components/Layout/MainLayout.razor` |
| Rotas protegidas | `NutrieGestao.Web/Components/Routes.razor` |
| Pedidos | `NutrieGestao.Web/Components/Pages/Pedidos.razor` |
| Clientes | `NutrieGestao.Web/Components/Pages/Clientes.razor` |
| Marmitas | `NutrieGestao.Web/Components/Pages/Marmitas.razor` |
| Regras de cadastro | `NutrieGestao.Core/Services/ValidacaoCadastro.cs` |
| Gestão de dados | `NutrieGestao.Core/Services/ManagementService.cs` |
| Arquivos JSON | `NutrieGestao.Core/Services/DataService.cs` e `Repositories/JsonRepository.cs` |
| Autenticação | `NutrieGestao.Core/Services/AuthenticationService.cs` |
| Senhas | `NutrieGestao.Core/Services/PasswordHasher.cs` |
| Dados de pedido | `NutrieGestao.Core/Models/Pedido.cs` |

---

Se surgir dúvida durante o estudo, use o caminho inverso: comece na tela que você está vendo, localize o arquivo `.razor` com o mesmo nome, veja qual serviço ela chama e só então siga para a regra no Core. Esse caminho torna o código muito mais fácil de entender do que tentar ler a solução inteira de uma vez.

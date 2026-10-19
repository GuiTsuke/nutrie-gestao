using System.Security.Cryptography;
using NutrieGestao.Models;

namespace NutrieGestao.Services;

public sealed class AuthenticationService
{
    private static readonly object UsersLock = new();
    private readonly DataService _data;
    private readonly SecurityOptions _security;
    private readonly FileLogger _logger;
    private readonly string _usuariosPath;

    public AuthenticationService(DataService data, StorageOptions storage, SecurityOptions security, FileLogger logger)
    {
        _data = data;
        _security = security;
        _logger = logger;
        _usuariosPath = Path.Combine(storage.DataPath, "usuarios.json");
    }

    public bool ConfiguracaoInicialDisponivel() => !File.Exists(_usuariosPath);
    public bool TemCodigoDeAtivacao() => _security.TemCodigoDeAtivacao;

    public Usuario? Autenticar(string? nomeUsuario, string? senha)
    {
        var nomeUsuarioNormalizado = NormalizarNomeUsuario(nomeUsuario);
        var emailLegado = NormalizarEmail(nomeUsuario);
        if ((nomeUsuarioNormalizado is null && emailLegado is null) || string.IsNullOrEmpty(senha))
        {
            _logger.Information("Tentativa de login recusada por credenciais incompletas.");
            return null;
        }

        lock (UsersLock)
        {
            var usuarios = _data.LoadUsuarios();
            var usuario = nomeUsuarioNormalizado is null ? null : usuarios.FirstOrDefault(item => item.Ativo && item.NomeUsuario == nomeUsuarioNormalizado);
            // O nome de usuário é o identificador principal. O e-mail continua
            // aceito como alias para contas migradas e para evitar perda de acesso
            // durante a transição do formato antigo.
            usuario ??= emailLegado is null ? null : usuarios.FirstOrDefault(item => item.Ativo && item.Email.Equals(emailLegado, StringComparison.OrdinalIgnoreCase));
            if (usuario is null || !PasswordHasher.Verificar(senha, usuario.SenhaHash, usuario.SenhaSalt, usuario.IteracoesSenha))
            {
                _logger.Information("Tentativa de login recusada por credenciais inválidas.");
                return null;
            }

            if (NormalizarNomeUsuario(usuario.NomeUsuario) is null)
            {
                usuarios = CarregarUsuariosAtualizados();
                usuario = usuarios.First(item => item.Id == usuario.Id);
                _logger.Information("Conta antiga atualizada para usar nome de usuário.");
            }

            _logger.Information($"Login realizado para o usuário '{usuario.Id}'.");
            return usuario;
        }
    }

    public ResultadoAutenticacao CriarAdministradorInicial(string? nome, string? nomeUsuario, string? email, string? senha, string? confirmacaoSenha, string? codigoDeAtivacao)
    {
        lock (UsersLock)
        {
            if (!ConfiguracaoInicialDisponivel()) return ResultadoAutenticacao.Falha("O acesso inicial já foi concluído.");
            if (!_security.ValidarCodigoDeAtivacao(codigoDeAtivacao))
            {
                _logger.Information("Tentativa recusada de criação do acesso inicial.");
                return ResultadoAutenticacao.Falha("Não foi possível concluir o acesso inicial.");
            }

            var nomeNormalizado = nome?.Trim();
            var usuarioNormalizado = NormalizarNomeUsuario(nomeUsuario);
            var emailNormalizado = NormalizarEmail(email);
            var validacao = ValidarNovoUsuario(nomeNormalizado, usuarioNormalizado, emailNormalizado, senha, confirmacaoSenha);
            if (validacao is not null) return ResultadoAutenticacao.Falha(validacao);

            var administrador = CriarUsuarioInterno(nomeNormalizado!, usuarioNormalizado!, emailNormalizado!, senha!, PerfilUsuario.Administrador);
            _data.SaveUsuarios([administrador]);
            _logger.Information($"Administrador inicial criado com o usuário '{administrador.Id}'.");
            return ResultadoAutenticacao.Ok(administrador);
        }
    }

    public IReadOnlyList<UsuarioResumo> ListarUsuarios(Guid administradorId)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            return EhAdministrador(usuarios, administradorId)
                ? usuarios.OrderBy(item => item.Nome).Select(UsuarioResumo.Criar).ToList()
                : [];
        }
    }

    public OperationResult CriarUsuario(Guid administradorId, string? nome, string? nomeUsuario, string? email, string? senha, string? confirmacaoSenha, PerfilUsuario perfil)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            if (!EhAdministrador(usuarios, administradorId)) return SemPermissao();

            var nomeNormalizado = nome?.Trim();
            var usuarioNormalizado = NormalizarNomeUsuario(nomeUsuario);
            var emailNormalizado = NormalizarEmail(email);
            var validacao = ValidarNovoUsuario(nomeNormalizado, usuarioNormalizado, emailNormalizado, senha, confirmacaoSenha);
            if (validacao is not null) return OperationResult.Fail(validacao);
            if (usuarios.Any(item => item.NomeUsuario == usuarioNormalizado)) return OperationResult.Fail("Este nome de usuário já está em uso.");
            if (usuarios.Any(item => item.Email.Equals(emailNormalizado, StringComparison.OrdinalIgnoreCase))) return OperationResult.Fail("Este e-mail já está cadastrado.");
            if (!Enum.IsDefined(perfil)) return OperationResult.Fail("Selecione um perfil válido.");

            usuarios.Add(CriarUsuarioInterno(nomeNormalizado!, usuarioNormalizado!, emailNormalizado!, senha!, perfil));
            _data.SaveUsuarios(usuarios);
            _logger.Information("Nova conta de usuário criada.");
            return OperationResult.Ok("Usuário cadastrado com sucesso.");
        }
    }

    public OperationResult AtualizarUsuario(Guid administradorId, Guid usuarioId, string? nome, string? nomeUsuario, string? email, PerfilUsuario perfil)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            if (!EhAdministrador(usuarios, administradorId)) return SemPermissao();
            var usuario = usuarios.FirstOrDefault(item => item.Id == usuarioId);
            if (usuario is null) return OperationResult.Fail("Usuário não encontrado.");

            var nomeNormalizado = nome?.Trim();
            var usuarioNormalizado = NormalizarNomeUsuario(nomeUsuario);
            var emailNormalizado = NormalizarEmail(email);
            var validacao = ValidarDadosUsuario(nomeNormalizado, usuarioNormalizado, emailNormalizado);
            if (validacao is not null) return OperationResult.Fail(validacao);
            if (!Enum.IsDefined(perfil)) return OperationResult.Fail("Selecione um perfil válido.");
            if (usuarioId == administradorId && perfil != usuario.Perfil) return OperationResult.Fail("Não é possível alterar seu próprio perfil.");
            if (usuarios.Any(item => item.Id != usuarioId && item.NomeUsuario == usuarioNormalizado)) return OperationResult.Fail("Este nome de usuário já está em uso.");
            if (usuarios.Any(item => item.Id != usuarioId && item.Email.Equals(emailNormalizado, StringComparison.OrdinalIgnoreCase))) return OperationResult.Fail("Este e-mail já está cadastrado.");
            if (usuario.Perfil == PerfilUsuario.Administrador && perfil != PerfilUsuario.Administrador && AdministradoresAtivos(usuarios) == 1)
                return OperationResult.Fail("Mantenha pelo menos uma conta administradora ativa.");

            usuario.Nome = nomeNormalizado!;
            usuario.NomeUsuario = usuarioNormalizado!;
            usuario.Email = emailNormalizado!;
            if (usuarioId != administradorId) usuario.Perfil = perfil;
            RenovarCarimboSeguranca(usuario);
            _data.SaveUsuarios(usuarios);
            _logger.Information("Dados de usuário atualizados.");
            return OperationResult.Ok("Usuário atualizado. A sessão dessa conta foi encerrada por segurança.");
        }
    }

    public OperationResult DefinirAtivo(Guid administradorId, Guid usuarioId, bool ativo)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            if (!EhAdministrador(usuarios, administradorId)) return SemPermissao();
            if (usuarioId == administradorId) return OperationResult.Fail("Não é possível alterar o status da sua própria conta.");

            var usuario = usuarios.FirstOrDefault(item => item.Id == usuarioId);
            if (usuario is null) return OperationResult.Fail("Usuário não encontrado.");
            if (usuario.Ativo == ativo) return OperationResult.Ok(ativo ? "Usuário já está ativo." : "Usuário já está desativado.");
            if (!ativo && usuario.Perfil == PerfilUsuario.Administrador && AdministradoresAtivos(usuarios) == 1)
                return OperationResult.Fail("Mantenha pelo menos uma conta administradora ativa.");

            usuario.Ativo = ativo;
            RenovarCarimboSeguranca(usuario);
            _data.SaveUsuarios(usuarios);
            _logger.Information(ativo ? "Conta de usuário ativada." : "Conta de usuário desativada.");
            return OperationResult.Ok(ativo ? "Usuário ativado." : "Usuário desativado e sessão encerrada.");
        }
    }

    public OperationResult RedefinirSenha(Guid administradorId, Guid usuarioId, string? senha, string? confirmacaoSenha)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            if (!EhAdministrador(usuarios, administradorId)) return SemPermissao();
            var usuario = usuarios.FirstOrDefault(item => item.Id == usuarioId);
            if (usuario is null) return OperationResult.Fail("Usuário não encontrado.");

            var validacao = ValidarSenha(senha, confirmacaoSenha);
            if (validacao is not null) return OperationResult.Fail(validacao);
            AplicarSenha(usuario, senha!);
            _data.SaveUsuarios(usuarios);
            _logger.Information("Senha de usuário redefinida por administrador.");
            return OperationResult.Ok("Senha redefinida. A sessão dessa conta foi encerrada por segurança.");
        }
    }

    public OperationResult AlterarPropriaSenha(Guid usuarioId, string? senhaAtual, string? novaSenha, string? confirmacaoSenha)
    {
        lock (UsersLock)
        {
            var usuarios = CarregarUsuariosAtualizados();
            var usuario = usuarios.FirstOrDefault(item => item.Id == usuarioId && item.Ativo);
            if (usuario is null) return OperationResult.Fail("Sua conta não está disponível.");
            if (string.IsNullOrWhiteSpace(senhaAtual) || !PasswordHasher.Verificar(senhaAtual, usuario.SenhaHash, usuario.SenhaSalt, usuario.IteracoesSenha))
                return OperationResult.Fail("A senha atual não confere.");

            var validacao = ValidarSenha(novaSenha, confirmacaoSenha);
            if (validacao is not null) return OperationResult.Fail(validacao);
            if (PasswordHasher.Verificar(novaSenha!, usuario.SenhaHash, usuario.SenhaSalt, usuario.IteracoesSenha))
                return OperationResult.Fail("Escolha uma senha diferente da atual.");

            AplicarSenha(usuario, novaSenha!);
            _data.SaveUsuarios(usuarios);
            _logger.Information("Usuário alterou a própria senha.");
            return OperationResult.Ok("Senha alterada. Entre novamente para continuar.");
        }
    }

    public bool SessaoEhValida(Guid usuarioId, string? carimboSeguranca)
    {
        if (usuarioId == Guid.Empty || string.IsNullOrWhiteSpace(carimboSeguranca)) return false;
        lock (UsersLock)
        {
            var usuario = _data.LoadUsuarios().FirstOrDefault(item => item.Id == usuarioId && item.Ativo);
            if (usuario is null) return false;
            var esperado = System.Text.Encoding.UTF8.GetBytes(usuario.CarimboSeguranca);
            var informado = System.Text.Encoding.UTF8.GetBytes(carimboSeguranca);
            return esperado.Length == informado.Length && CryptographicOperations.FixedTimeEquals(esperado, informado);
        }
    }

    private List<Usuario> CarregarUsuariosAtualizados()
    {
        var usuarios = _data.LoadUsuarios();
        var nomesEmUso = new HashSet<string>(StringComparer.Ordinal);
        var alterado = false;
        foreach (var usuario in usuarios)
        {
            var nomeUsuario = NormalizarNomeUsuario(usuario.NomeUsuario);
            if (nomeUsuario is null || !nomesEmUso.Add(nomeUsuario))
            {
                nomeUsuario = GerarNomeUsuarioLegado(usuario, nomesEmUso);
                alterado = true;
            }
            else if (!string.Equals(usuario.NomeUsuario, nomeUsuario, StringComparison.Ordinal))
            {
                alterado = true;
            }
            usuario.NomeUsuario = nomeUsuario;
        }
        if (alterado && usuarios.Count > 0)
        {
            _data.SaveUsuarios(usuarios);
            _logger.Information("Contas existentes receberam nomes de usuário compatíveis.");
        }
        return usuarios;
    }

    private static Usuario CriarUsuarioInterno(string nome, string nomeUsuario, string email, string senha, PerfilUsuario perfil)
    {
        var senhaProtegida = PasswordHasher.Criar(senha);
        return new Usuario
        {
            Nome = nome, NomeUsuario = nomeUsuario, Email = email, Perfil = perfil,
            SenhaHash = senhaProtegida.Hash, SenhaSalt = senhaProtegida.Salt, IteracoesSenha = senhaProtegida.Iterations,
            CarimboSeguranca = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        };
    }

    private static void AplicarSenha(Usuario usuario, string senha)
    {
        var senhaProtegida = PasswordHasher.Criar(senha);
        usuario.SenhaHash = senhaProtegida.Hash;
        usuario.SenhaSalt = senhaProtegida.Salt;
        usuario.IteracoesSenha = senhaProtegida.Iterations;
        RenovarCarimboSeguranca(usuario);
    }

    private static void RenovarCarimboSeguranca(Usuario usuario) => usuario.CarimboSeguranca = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private static bool EhAdministrador(IEnumerable<Usuario> usuarios, Guid usuarioId) => usuarios.Any(item => item.Id == usuarioId && item.Ativo && item.Perfil == PerfilUsuario.Administrador);
    private static int AdministradoresAtivos(IEnumerable<Usuario> usuarios) => usuarios.Count(item => item.Ativo && item.Perfil == PerfilUsuario.Administrador);
    private static OperationResult SemPermissao() => OperationResult.Fail("Você não tem permissão para gerenciar usuários.");

    private static string? ValidarNovoUsuario(string? nome, string? nomeUsuario, string? email, string? senha, string? confirmacaoSenha)
        => ValidarDadosUsuario(nome, nomeUsuario, email) ?? ValidarSenha(senha, confirmacaoSenha);

    private static string? ValidarDadosUsuario(string? nome, string? nomeUsuario, string? email)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "Informe o nome da pessoa responsável.";
        if (nome.Length > 120) return "O nome deve ter no máximo 120 caracteres.";
        if (nomeUsuario is null) return "Use um nome de usuário entre 3 e 40 caracteres.";
        if (email is null) return "Informe um e-mail válido.";
        return null;
    }

    private static string? ValidarSenha(string? senha, string? confirmacaoSenha)
    {
        if (senha is null || senha.Length < 9) return "A senha deve ter pelo menos 9 caracteres.";
        if (senha.Length > 128) return "A senha deve ter no máximo 128 caracteres.";
        if (!senha.Any(char.IsLetter) || !senha.Any(char.IsDigit)) return "A senha deve ter letras e números.";
        if (!string.Equals(senha, confirmacaoSenha, StringComparison.Ordinal)) return "A confirmação de senha não confere.";
        return null;
    }

    private static string? NormalizarNomeUsuario(string? nomeUsuario)
    {
        if (string.IsNullOrWhiteSpace(nomeUsuario)) return null;
        var normalizado = nomeUsuario.Trim().ToLowerInvariant();
        if (normalizado.Length is < 3 or > 40) return null;
        return normalizado.All(caractere => char.IsLetterOrDigit(caractere) || caractere is '.' or '-' or '_') ? normalizado : null;
    }

    private static string GerarNomeUsuarioLegado(Usuario usuario, ISet<string> nomesEmUso)
    {
        var origem = usuario.Email.Split('@')[0];
        var baseNormalizada = new string(origem.Select(caractere => char.IsLetterOrDigit(caractere) || caractere is '.' or '-' or '_' ? char.ToLowerInvariant(caractere) : '-').ToArray()).Trim('.', '-', '_');
        if (baseNormalizada.Length < 3) baseNormalizada = "usuario";
        if (baseNormalizada.Length > 32) baseNormalizada = baseNormalizada[..32];
        var candidato = baseNormalizada;
        var sufixo = 2;
        while (!nomesEmUso.Add(candidato))
        {
            var textoSufixo = $"-{sufixo++}";
            candidato = baseNormalizada[..Math.Min(baseNormalizada.Length, 40 - textoSufixo.Length)] + textoSufixo;
        }
        return candidato;
    }

    private static string? NormalizarEmail(string? email) => ValidacaoCadastro.NormalizarEmail(email);
}

public sealed record ResultadoAutenticacao(bool Sucesso, string? Mensagem, Usuario? Usuario)
{
    public static ResultadoAutenticacao Ok(Usuario usuario) => new(true, null, usuario);
    public static ResultadoAutenticacao Falha(string mensagem) => new(false, mensagem, null);
}

public sealed record UsuarioResumo(Guid Id, string Nome, string NomeUsuario, string Email, PerfilUsuario Perfil, bool Ativo, DateTime DataCadastro)
{
    public static UsuarioResumo Criar(Usuario usuario) => new(usuario.Id, usuario.Nome, usuario.NomeUsuario, usuario.Email, usuario.Perfil, usuario.Ativo, usuario.DataCadastro);
}

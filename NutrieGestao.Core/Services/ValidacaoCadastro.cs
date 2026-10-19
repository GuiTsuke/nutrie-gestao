using System.Net.Mail;
using NutrieGestao.Models;

namespace NutrieGestao.Services;

public static class ValidacaoCadastro
{
    public const decimal ValorMonetarioMaximo = 1_000_000m;
    public const decimal QuantidadeIngredienteMaxima = 100_000m;
    public const int QuantidadeItemPedidoMaxima = 1_000;

    public static string? ValidarEmpresa(Empresa empresa)
    {
        var erro = ValidarObrigatorio(empresa.Nome, "o nome da empresa", 120)
            ?? ValidarObrigatorio(empresa.Responsavel, "a pessoa responsável", 120)
            ?? ValidarTelefone(empresa.Telefone, "o telefone")
            ?? ValidarTelefone(empresa.WhatsApp, "o WhatsApp")
            ?? ValidarTamanho(empresa.Instagram, "o Instagram", 100)
            ?? ValidarTamanho(empresa.Endereco, "o endereço", 180)
            ?? ValidarTamanho(empresa.Cidade, "a cidade", 100)
            ?? ValidarTamanho(empresa.Estado, "o estado", 30);

        return erro;
    }

    public static string? ValidarCliente(Cliente cliente)
    {
        var erro = ValidarObrigatorio(cliente.Nome, "o nome do cliente", 120)
            ?? ValidarTelefone(cliente.Telefone, "o telefone")
            ?? ValidarTelefone(cliente.WhatsApp, "o WhatsApp")
            ?? ValidarTamanho(cliente.Endereco, "o endereço", 180)
            ?? ValidarTamanho(cliente.Numero, "o número", 20)
            ?? ValidarTamanho(cliente.Complemento, "o complemento", 120)
            ?? ValidarTamanho(cliente.Bairro, "o bairro", 100)
            ?? ValidarTamanho(cliente.Cidade, "a cidade", 100)
            ?? ValidarTamanho(cliente.Observacoes, "as observações", 1_000);

        return erro;
    }

    public static string? ValidarMarmita(Marmita marmita)
    {
        var erro = ValidarObrigatorio(marmita.Nome, "o nome da marmita", 120)
            ?? ValidarTamanho(marmita.Descricao, "a descrição", 500)
            ?? ValidarTamanho(marmita.Categoria, "a categoria", 100);

        if (erro is not null) return erro;
        if (!TamanhosMarmita.EhValido(marmita.Tamanho)) return "Selecione o tamanho 350g ou 500g.";
        if (marmita.Preco < 0 || marmita.Preco > ValorMonetarioMaximo) return $"O preço deve estar entre R$ 0,00 e R$ {ValorMonetarioMaximo:N2}.";
        if (marmita.Ingredientes.Any(ingrediente =>
                string.IsNullOrWhiteSpace(ingrediente.Nome) ||
                ingrediente.Nome.Trim().Length > 100 ||
                ingrediente.QuantidadePorMarmita < 0 ||
                ingrediente.QuantidadePorMarmita > QuantidadeIngredienteMaxima ||
                ingrediente.CustoPorcao < 0 ||
                ingrediente.CustoPorcao > ValorMonetarioMaximo))
        {
            return "Revise os ingredientes, quantidades e custos informados.";
        }

        return null;
    }

    public static string? ValidarPedido(Pedido pedido)
    {
        if (pedido.ClienteId == Guid.Empty || string.IsNullOrWhiteSpace(pedido.NomeCliente)) return "Selecione um cliente.";
        if (pedido.NomeCliente.Trim().Length > 120) return "O nome do cliente do pedido deve ter no máximo 120 caracteres.";
        if (pedido.Itens.Count == 0) return "Adicione pelo menos um item ao pedido.";
        if (pedido.Itens.Any(item => item.Quantidade is < 1 or > QuantidadeItemPedidoMaxima || item.ValorUnitario < 0 || item.ValorUnitario > ValorMonetarioMaximo || string.IsNullOrWhiteSpace(item.NomeMarmita) || item.NomeMarmita.Trim().Length > 120))
            return "Revise as quantidades, produtos e valores dos itens.";
        if (!Enum.IsDefined(pedido.TipoDesconto)) return "Selecione um tipo de desconto válido.";
        if (pedido.Desconto < 0) return "O desconto não pode ser negativo.";
        if (pedido.TipoDesconto == TipoDescontoPedido.Percentual && pedido.Desconto > 100) return "O desconto percentual não pode ser maior que 100%.";
        if (pedido.TipoDesconto == TipoDescontoPedido.Valor && (pedido.Desconto > ValorMonetarioMaximo || pedido.Desconto > pedido.ValorBruto)) return "O desconto não pode ser maior que o valor das marmitas.";
        if (pedido.ValorPago < 0 || pedido.ValorPago > pedido.ValorTotal) return "Informe um valor pago válido.";
        return ValidarTamanho(pedido.Observacoes, "as observações", 1_000);
    }

    public static string? NormalizarEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 180 || email.Any(char.IsWhiteSpace)) return null;

        try
        {
            var endereco = new MailAddress(email.Trim());
            return string.Equals(endereco.Address, email.Trim(), StringComparison.OrdinalIgnoreCase)
                ? endereco.Address.ToLowerInvariant()
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? ValidarTelefone(string? telefone, string nomeCampo)
    {
        if (string.IsNullOrWhiteSpace(telefone)) return null;
        if (telefone.Length > 20) return $"{PrimeiraLetraMaiuscula(nomeCampo)} deve ter no máximo 20 caracteres.";
        if (telefone.Any(caractere => !char.IsDigit(caractere) && caractere is not (' ' or '(' or ')' or '-' or '+' or '.')))
            return $"Informe {nomeCampo} usando apenas números e formatação.";
        if (telefone.Count(caractere => caractere == '+') > 1 || (telefone.Contains('+') && !telefone.TrimStart().StartsWith('+')))
            return $"Informe {nomeCampo} com o código do país apenas no início.";

        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length is 12 or 13 && digitos.StartsWith("55", StringComparison.Ordinal)) digitos = digitos[2..];
        return digitos.Length is 10 or 11 ? null : $"Informe {nomeCampo} com DDD.";
    }

    private static string? ValidarObrigatorio(string? valor, string nomeCampo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return $"Informe {nomeCampo}.";
        return valor.Trim().Length > tamanhoMaximo ? $"{PrimeiraLetraMaiuscula(nomeCampo)} deve ter no máximo {tamanhoMaximo} caracteres." : null;
    }

    private static string? ValidarTamanho(string? valor, string nomeCampo, int tamanhoMaximo)
    {
        return !string.IsNullOrWhiteSpace(valor) && valor.Trim().Length > tamanhoMaximo
            ? $"{PrimeiraLetraMaiuscula(nomeCampo)} deve ter no máximo {tamanhoMaximo} caracteres."
            : null;
    }

    private static string PrimeiraLetraMaiuscula(string texto) => char.ToUpperInvariant(texto[0]) + texto[1..];
}

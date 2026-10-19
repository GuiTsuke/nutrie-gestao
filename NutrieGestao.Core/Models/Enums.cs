namespace NutrieGestao.Models;

public enum StatusPedido { Novo, EmPreparacao, Pronto, Entregue, Cancelado }
public enum FormaPagamento { Pix, Dinheiro, Cartao, Outro }
public enum TipoDescontoPedido { Valor, Percentual }
public enum Pagina { Dashboard, Pedidos, Clientes, Marmitas, Configuracoes }
public enum PerfilUsuario { Administrador, Operador }

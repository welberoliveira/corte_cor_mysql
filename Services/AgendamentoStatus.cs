namespace CorteCor.Services
{
    public static class AgendamentoStatus
    {
        public const string Agendado = "Agendado";
        public const string Pendente = "Pendente";
        public const string Pago = "Pago";
        public const string Cancelado = "Cancelado";
        public const string Confirmado = "Confirmado";

        public static string Normalizar(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Agendado;
            }

            var comparacao = status.Trim();

            if (comparacao.Equals(Confirmado, System.StringComparison.OrdinalIgnoreCase))
            {
                return Pago;
            }

            if (comparacao.Equals(Agendado, System.StringComparison.OrdinalIgnoreCase))
            {
                return Agendado;
            }

            if (comparacao.Equals(Pendente, System.StringComparison.OrdinalIgnoreCase))
            {
                return Pendente;
            }

            if (comparacao.Equals(Pago, System.StringComparison.OrdinalIgnoreCase))
            {
                return Pago;
            }

            if (comparacao.Equals(Cancelado, System.StringComparison.OrdinalIgnoreCase))
            {
                return Cancelado;
            }

            return comparacao;
        }

        public static bool PodeAlterar(string? status) =>
            Normalizar(status) is Agendado or Pendente;

        public static bool PodeExcluir(string? status) =>
            Normalizar(status) is not Pago and not Cancelado;

        public static bool PodePagar(string? status) =>
            Normalizar(status) is Agendado or Pendente;

        public static bool PodeEmitirNota(string? status) =>
            Normalizar(status) == Pago;

        public static string ObterCor(string? status) =>
            Normalizar(status) switch
            {
                Agendado => "#3788d8",
                Pendente => "#ffc107",
                Pago => "#28a745",
                Cancelado => "#dc3545",
                _ => "#3788d8"
            };

        public static string ObterClasseBadgeBootstrap(string? status) =>
            Normalizar(status) switch
            {
                Agendado => "badge-success",
                Pendente => "badge-warning text-dark",
                Pago => "badge-primary",
                Cancelado => "badge-danger",
                _ => "badge-secondary"
            };
    }
}
